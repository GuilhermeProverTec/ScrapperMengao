using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockArquibancada.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace ScrappingMockArquibancada.Scrapper
{
    public class ArquibancadaScrapper
    {
        private readonly IWebDriver _driver;

        public ArquibancadaScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public Arquibancada ObterArquibancada()
        {
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com.br/torcida/arquibancada-rubro-negra/");

            var arquibancada = ObterDadosArquibancada();

            _driver.Quit();
            return arquibancada;
        }

        public void SalvarArquibancadaComoJson(Arquibancada arquibancada, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(arquibancada, options);
            File.WriteAllText(caminho, json);
        }

        private Arquibancada ObterDadosArquibancada()
        {
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);
            var arquibancada = new Arquibancada();


                arquibancada.Titulo = _driver.FindElement(By.CssSelector("div > h1")).Text;

                var imagemElements = _driver.FindElements(By.CssSelector(".carrossel.principal img"));

                var imagens = new HashSet<Imagem>();

                foreach (var element in imagemElements)
                {
                    var url = element.GetAttribute("src");
                    var alt = element.GetAttribute("alt");

                    // Optional: Filter out any invalid or duplicate entries automatically via the HashSet
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        imagens.Add(new Imagem
                        {
                            Url = url,
                            Alt = alt
                        });
                    }
                }

                arquibancada.CarrosselImagens = imagens;

                arquibancada.Descricao = _driver.FindElement(By.CssSelector("div.fullWidth.txt > div > p")).Text;

                var torcidas = new List<Torcida>();
                var sections = _driver.FindElements(By.CssSelector("div.listaTorcidas > section"));

                foreach (var section in sections) { 
                    try
                    {
                        var torcida = new Torcida();

                        // Nome da torcida
                        torcida.NomeTorcida = section.FindElement(By.CssSelector("button")).Text.Trim();

                        // Descrição (logo, dados, etc.)
                        var descricao = section.FindElement(By.CssSelector(".descricao"));
                        var logo = descricao.FindElement(By.CssSelector("img"));

                        if (logo.GetAttribute("src") != null)
                        {
                            // Ensure the base URL is added only once
                            var logoUrl = logo.GetAttribute("src");
                            if (!logoUrl.StartsWith("https://"))
                            {
                                logoUrl = $"https://www.museuflamengo.com.br{logoUrl}";
                            }
                            torcida.LogoTorcida.Url = logoUrl;
                            torcida.LogoTorcida.Alt = logo.GetAttribute("alt");
                        }
                        else
                        {
                            torcida.LogoTorcida = null;
                        }

                        // Get the article
                        var article = descricao.FindElement(By.TagName("article"));

                        // Count the number of <p> tags within the article
                        var paragraphs = article.FindElements(By.TagName("p"));

                        if (paragraphs.Count == 1)
                        {
                            // If there's only one <p>, process the logic for Charanga style (single paragraph)
                            var paragraphHtml = paragraphs[0].GetAttribute("innerHTML");

                            // Data de fundação
                            var matchData = Regex.Match(paragraphHtml, @"Data de fundação.*?(\d{2}/\d{2}/\d{4})");
                            if (matchData.Success && DateOnly.TryParse(matchData.Groups[1].Value, out var data))
                            {
                                torcida.DataFundacao = data;
                            }

                            var torcedoresHistoricos = Regex.Match(paragraphHtml, @"Torcedores históricos:&nbsp;\s*(.*)");

                            if (torcedoresHistoricos.Success)
                            {
                                torcida.TorcedoresIlustres = torcedoresHistoricos.Groups[1].Value.Trim();
                            }

                            var torcedoresIlustres = Regex.Match(paragraphHtml, @"Torcedores ilustres:</b>\s*(.*)");

                            if (torcedoresIlustres.Success)
                            {
                                torcida.TorcedoresIlustres = torcedoresIlustres.Groups[1].Value.Trim();
                            }
                        // Fundadores
                        var fundadoresMatch = Regex.Match(paragraphHtml, @"Fundadores.*?:\s*(.*?)(?:<br>|</p>|$)", RegexOptions.Singleline);
                            List<LinkExterno> fundadores = new List<LinkExterno>();
                            if (fundadoresMatch.Success)
                            {
                                var fundadoresFragment = fundadoresMatch.Groups[1].Value;

                                // Extract anchor tags inside Fundadores (linked names)
                                var fundadoresLinks = Regex.Matches(fundadoresFragment, @"<a href=""(.*?)"".*?>(.*?)</a>");
                                foreach (Match link in fundadoresLinks)
                                {
                                    fundadores.Add(new LinkExterno
                                    {
                                        Url = $"https://www.museuflamengo.com.br{link.Groups[1].Value}",
                                        Texto = link.Groups[2].Value.Replace("&nbsp;", " ").Trim()
                                    });
                                }

                                // Handle non-linked fundadores (plain text names)
                                var plainText = Regex.Replace(fundadoresFragment, "<.*?>", "").Trim();
                                var splitNames = plainText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                          .Select(name => name.Trim())
                                                          .ToList();

                                var finalNames = new List<string>();
                                foreach (var name in splitNames)
                                {
                                    if (name.Contains(" e "))
                                    {
                                        var splitByAnd = name.Replace("&nbsp;", " ").Trim().Split(new[] { " e " }, StringSplitOptions.None)
                                                              .Select(n => n.Trim())
                                                              .ToList();
                                        finalNames.AddRange(splitByAnd);
                                    }
                                    else
                                    {
                                        finalNames.Add(name.Replace("&nbsp;", " ").Trim());
                                    }
                                }

                                foreach (var name in finalNames)
                                {
                                    if (!fundadores.Any(f => f.Texto == name))
                                    {
                                        fundadores.Add(new LinkExterno { Texto = name });
                                    }
                                }
                                torcida.FundadoresTorcedoresIlustres = fundadores;
                            }

                            // Lema
                        var lemaMatch = Regex.Match(paragraphHtml, @"Lema:\s*</strong>\s*(.*?)(<br>|$)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        if (lemaMatch.Success)
                            {
                                torcida.Lema = lemaMatch.Groups[1].Value.Trim();
                            }

                            // Livro Biografia
                            var livroMatch = Regex.Match(paragraphHtml, @"Livro biografia.*?<a href=""(.*?)"".*?>(.*?)</a>");
                            if (livroMatch.Success)
                            {
                                torcida.LivroBiografia = new LinkExterno
                                {
                                    Url = $"https://www.museuflamengo.com.br{livroMatch.Groups[1].Value}",
                                    Texto = livroMatch.Groups[2].Value
                                };
                            }
                        }
                        else
                        {
                            // If there are multiple <p> tags, process the logic for Torcida Jovem style (multiple paragraphs)
                            foreach (var paragraph in paragraphs)
                            {
                                var paragraphHtml = paragraph.GetAttribute("innerHTML");
                                try
                                {
                                    var labelMatch = Regex.Match(paragraphHtml, @"<strong>(.*?)</strong>\s*(.*)");
                                    // Find the <strong> tag within the <p> tag
                                    var strongTag = paragraph.FindElement(By.TagName("strong"));
 

                                    if (strongTag != null)
                                    {
                                        var strongText = strongTag.Text.Trim();

                                        if (labelMatch.Success)
                                        {
                                            var label = labelMatch.Groups[1].Value.Trim(); // This is the text inside <strong>
                                            var content = labelMatch.Groups[2].Value.Trim(); // This is the content after <strong>

                                            // Now we can process based on the label
                                            switch (label)
                                            {
                                                case "Data de fundação:":
                                                    if (DateOnly.TryParse(content, out var data))
                                                    {
                                                        torcida.DataFundacao = data;
                                                    }
                                                    break;

                                                case "Fundadores e torcedores ilustres:":
                                                case "Fundadores:" :
                                                    var fundadoresFragment = content;
                                                    List<LinkExterno> fundadores = new();

                                                    // Extract anchor tags inside Fundadores (linked names)
                                                    var fundadoresLinks = Regex.Matches(fundadoresFragment, @"<a href=""(.*?)"".*?>(.*?)</a>");
                                                    foreach (Match link in fundadoresLinks)
                                                    {
                                                        fundadores.Add(new LinkExterno
                                                        {
                                                            Url = $"https://www.museuflamengo.com.br{link.Groups[1].Value}",
                                                            Texto = link.Groups[2].Value.Replace("&nbsp;", " ").Trim()
                                                        });
                                                    }

                                                    // Handle non-linked fundadores (plain text names)
                                                    var plainText = Regex.Replace(fundadoresFragment, "<.*?>", "").Trim();
                                                    var splitNames = plainText.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                                                .Select(name => name.Trim())
                                                                                .ToList();

                                                    var finalNames = new List<string>();
                                                    foreach (var name in splitNames)
                                                    {
                                                        if (name.Contains(" e "))
                                                        {
                                                            var splitByAnd = name.Replace("&nbsp;", " ").Trim().Split(new[] { " e " }, StringSplitOptions.None)
                                                                                    .Select(n => n.Trim())
                                                                                    .ToList();
                                                            finalNames.AddRange(splitByAnd);
                                                        }
                                                        else
                                                        {
                                                            finalNames.Add(name);
                                                        }
                                                    }

                                                    foreach (var name in finalNames)
                                                    {
                                                        if (!fundadores.Any(f => f.Texto == name))
                                                        {
                                                            fundadores.Add(new LinkExterno { Texto = name.Replace("&nbsp;", " ").Trim() });
                                                        }
                                                    }
                                                    torcida.FundadoresTorcedoresIlustres = fundadores;
                                                    break;
                                                case "Lema:":
                                                    torcida.Lema = content;
                                                    break;

                                                case "Livro biografia:":
                                                    var livroMatch = Regex.Match(paragraphHtml, @"Livro biografia.*?<a href=""(.*?)"".*?>(.*?)</a>");
                                                    if (livroMatch.Success)
                                                    {
                                                        torcida.LivroBiografia = new LinkExterno
                                                        {
                                                            Url = $"https://www.museuflamengo.com.br{livroMatch.Groups[1].Value}",
                                                            Texto = livroMatch.Groups[2].Value
                                                        };
                                                    }
                                                    break;

                                                default:
                                                    break;
                                            }
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    
                                }
                            }
                        }
                        try
                        {
                            // Textos da história
                            var textos = new List<string>();
                            var historia = section.FindElement(By.CssSelector("div.historia.stdCnt"));
                        
                            var ps = section.FindElements(By.CssSelector("div.historia.stdCnt > p"));
                            foreach (var p in ps)
                            {
                                var rawHtml = p.GetAttribute("innerHTML");

                            // Remove HTML tags
                                var texto = Regex.Replace(rawHtml, "<.*?>", "").Trim();
                                if (!string.IsNullOrWhiteSpace(texto) && !texto.StartsWith("Livro biografia", StringComparison.OrdinalIgnoreCase))
                                {
                                    textos.Add(texto);
                                }
                            }
                            torcida.Textos = textos;

                        }
                        catch (Exception ex) { 
                            torcida.Textos = null;
                        }
                        try
                        {
                            var iframes = section.FindElements(By.CssSelector("div.historia.stdCnt > p > iframe"));
                            foreach (var iframe in iframes)
                            {
                                torcida.YoutubeIFrames.Add(iframe.GetAttribute("src"));
                            }
                        }
                        catch (Exception ex)
                        {
                            torcida.YoutubeIFrames = null;
                        }

                        // Imagens do carrossel dentro da torcida
                        var imagensTorcida = new HashSet<Imagem>();
                        var carrosselImgs = section.FindElements(By.CssSelector(".gallery img"));
                        foreach (var img in carrosselImgs)
                        {
                            var src = img.GetAttribute("src");
                            if (!string.IsNullOrEmpty(src))
                            {
                                imagensTorcida.Add(new Imagem
                                {
                                    Url = src,
                                    Alt = img.GetAttribute("alt")
                                });
                            }
                        }
                        torcida.CarroselImagemTorcida = imagensTorcida;

                        torcidas.Add(torcida);
                    }
                    catch(Exception ex) 
                    {
                        throw(ex);
                    }

                }

                arquibancada.Torcidas = torcidas;

            


            return arquibancada;
        }

        private string GetField(string text, string label)
        {
            int index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = index + label.Length;
            int end = text.Length;

            var knownLabels = new[] {
                "Nome completo", "Data de nascimento", "Data de falecimento",
                "Local de nascimento", "Área de atuação"
            };

            foreach (var nextLabel in knownLabels)
            {
                if (nextLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
                    continue;

                int labelIndex = text.IndexOf(nextLabel, start, StringComparison.OrdinalIgnoreCase);
                if (labelIndex >= 0 && labelIndex < end)
                    end = labelIndex;
            }

            return text.Substring(start, end - start).Trim(':', '-', ' ', '\n', '\r');
        }
    }
}
