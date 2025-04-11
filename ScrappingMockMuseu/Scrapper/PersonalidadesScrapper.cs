using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using OpenQA.Selenium;
using ScrappingMockMuseu.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Text.RegularExpressions;

namespace ScrappingMockMuseu.Scrapper
{
    public class PersonalidadesScrapper
    {
        private readonly IWebDriver _driver;

        public PersonalidadesScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Personalidade> ObterHerois()
        {
            var herois = new List<Personalidade>();
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com.br/cultura-rubro-negra/personalidades/");

            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements.Select(link => link.GetAttribute("href")).Where(href => !string.IsNullOrEmpty(href)).ToList();


            foreach (var href in hrefs)
            {
                var heroi = ObterDadosHeroi(href);
                if (heroi != null)
                    herois.Add(heroi);
            }

            _driver.Quit();
            return herois;
        }

        public void SalvarHeroisComoJson(List<Personalidade> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Personalidade ObterDadosHeroi(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var heroi = new Personalidade();

            try
            {
                heroi.Apelido = _driver.FindElement(By.CssSelector("body > div.container.personalidades > div > div.heroBox.fullWidth.personalidades > h1")).Text;
                //var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent.ficha_tecnica > p"));
                //string lastLabel = null;

                //foreach (var p in paragrafos)
                //{
                //    string rawText = p.Text;

                //    var knownLabels = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                //    {
                //        { "nome completo:", val => heroi.NomeCompleto = val },
                //        { "nome:", val => heroi.NomeCompleto = val },
                //        { "data de nascimento:", val => heroi.DataNascimento = val },
                //        { "local de nascimento:", val => heroi.LocalNascimento = val },
                //        { "área de atuação:", val => heroi.AreaAtuacao = val },
                //        { "data de falecimento:", val => heroi.DataFalecimento = val }
                //    };

                //    foreach (var kvp in knownLabels)
                //    {
                //        var label = kvp.Key;
                //        var assign = kvp.Value;

                //        int index = rawText.IndexOf(label, StringComparison.OrdinalIgnoreCase);
                //        if (index >= 0)
                //        {
                //            int start = index + label.Length;
                //            int end = rawText.Length;

                //            // Try to find the next label
                //            foreach (var otherLabel in knownLabels.Keys)
                //            {
                //                if (otherLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
                //                    continue;

                //                int otherIndex = rawText.IndexOf(otherLabel, start, StringComparison.OrdinalIgnoreCase);
                //                if (otherIndex != -1 && otherIndex < end)
                //                    end = otherIndex;
                //            }

                //            var value = rawText.Substring(start, end - start).Trim(':', '-', ' ', '\n', '\r');
                //            assign(value);
                //        }
                //    }
                //}
                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent.ficha_tecnica > p"));
                var fullText = string.Join("\n", paragrafos.Select(p => p.Text));

                int countNomeCompleto = Regex.Matches(fullText, @"(?i)nome completo:").Count;

                if (countNomeCompleto == 2)
                {
                    // Handle Claudinho e Buchecha (special case: 2 people, merged)
                    string nomeCompleto1 = null, nomeCompleto2 = null;
                    string dataNascimento1 = null, dataNascimento2 = null;
                    string dataFalecimento = null;
                    string localNascimento1 = null, localNascimento2 = null;
                    string areaAtuacao1 = null, areaAtuacao2 = null;

                    foreach (var p in paragrafos)
                    {
                        var text = p.Text;

                        if (text.Contains("Nome completo:", StringComparison.OrdinalIgnoreCase))
                        {
                            var nome = GetField(text, "Nome completo:");
                            var nascimento = GetField(text, "Data de nascimento:");
                            var falecimento = GetField(text, "Data de falecimento:");
                            var local = GetField(text, "Local de nascimento:");
                            var area = GetField(text, "Área de atuação:");

                            if (string.IsNullOrEmpty(nomeCompleto1))
                            {
                                nomeCompleto1 = nome;
                                dataNascimento1 = nascimento;
                                dataFalecimento = falecimento;
                                localNascimento1 = local;
                                areaAtuacao1 = area;
                            }
                            else
                            {
                                nomeCompleto2 = nome;
                                dataNascimento2 = nascimento;
                                localNascimento2 = local;
                                areaAtuacao2 = area;
                            }
                        }
                    }

                    heroi.NomeCompleto = $"{nomeCompleto1} / {nomeCompleto2}";
                    heroi.DataNascimento = $"{dataNascimento1} / {dataNascimento2}";
                    heroi.LocalNascimento = $"{localNascimento1} / {localNascimento2}";
                    heroi.DataFalecimento = dataFalecimento;
                    heroi.AreaAtuacao = $"{areaAtuacao1}";
                }
                else
                {
                    // Standard parsing for single person (default flow)
                    foreach (var p in paragrafos)
                    {
                        string rawText = p.Text;

                        var knownLabels = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "nome completo:", val => heroi.NomeCompleto = val },
                             { "nome:", val => heroi.NomeCompleto = val },
                            { "data de nascimento:", val => heroi.DataNascimento = val },
                            { "data de falecimento:", val => heroi.DataFalecimento = val },
                            { "local de nascimento:", val => heroi.LocalNascimento = val },
                            { "área de atuação:", val => heroi.AreaAtuacao = val }
                        };

                        foreach (var kvp in knownLabels)
                        {
                            var label = kvp.Key;
                            var assign = kvp.Value;

                            int index = rawText.IndexOf(label, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                int start = index + label.Length;
                                int end = rawText.Length;

                                // Try to find the next label
                                foreach (var otherLabel in knownLabels.Keys)
                                {
                                    if (otherLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
                                        continue;

                                    int otherIndex = rawText.IndexOf(otherLabel, start, StringComparison.OrdinalIgnoreCase);
                                    if (otherIndex != -1 && otherIndex < end)
                                        end = otherIndex;
                                }

                                var value = rawText.Substring(start, end - start).Trim(':', '-', ' ', '\n', '\r');
                                assign(value);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10)); // Ajuste o tempo conforme necessário
            try
            {
                wait.Until(driver => driver.FindElements(By.CssSelector(".slick-track img")).Count > 0);

                // Get all <img> elements inside the slick-track
                var imgElements = _driver.FindElements(By.CssSelector(".slick-track img"));

                // Use HashSet to avoid duplicates from cloned slides
                var imageUrls = new HashSet<string>();

                foreach (var img in imgElements)
                {
                    var src = img.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src))
                    {
                        heroi.ImagemPersonalidade.Add(src);
                    }
                }
            }
            catch (Exception e)
            {
                heroi.ImagemPersonalidade = null;
            }

            try
            {
                heroi.TituloTexto = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt > h1")).Text;
            }
            catch (Exception e)
            {
                heroi.TituloTexto = null;
            }

            try
            {
                var textos = _driver.FindElements(By.CssSelector("div.fullWidth.sobre.stdCnt > p"));
                foreach (var texto in textos)
                    heroi.Textos.Add(texto.Text.Trim());
            }
            catch (Exception e)
            {
                heroi.Textos = null;
            }

            try
            {
                var saibaMais = _driver.FindElements(By.CssSelector("div.fullWidth.saiba_mais p a"));
                foreach (var saibaMaisElement in saibaMais)
                {
                    heroi.SaibaMais.Add(new Imagem
                    {
                        Url = saibaMaisElement.GetAttribute("href"),
                        Legenda = saibaMaisElement.Text.Trim()
                    });
                }
            }
            catch (Exception e)
            {
                heroi.SaibaMais = null;
            }

            try
            {
                var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                foreach (var imagem in imagens)
                {
                    var imageElement = imagem.FindElement(By.CssSelector("img"));
                    heroi.Imagens.Add(new Imagem
                    {
                        Url = imageElement.GetAttribute("src"),
                        Legenda = imagem.FindElement(By.CssSelector("dd.gallery-caption")).Text.Trim()
                    });
                }
            }
            catch (Exception e)
            {
                heroi.Imagens = null;
            }

            try
            {
                var imagem = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt")); ;
                var imageElement = imagem.FindElement(By.CssSelector("img.wp-image-3375"));
                heroi.ImagemTexto.Add(new Imagem
                {
                    Url = imageElement.GetAttribute("src"),
                    Legenda = imagem.FindElement(By.CssSelector(".wp-caption-text")).Text.Trim()
                });

            }
            catch (Exception e)
            {
                heroi.ImagemTexto = null;
            }

            try
            {
                var saibaMaisSection = _driver.FindElement(By.CssSelector("div.fullWidth.saiba_mais"));
                var iframeElements = saibaMaisSection.FindElements(By.TagName("iframe"));

                foreach (var iframe in iframeElements)
                {
                    var src = iframe.GetAttribute("src");
                    if(src.Contains("youtube"))
                        heroi.YoutubeIframes.Add(src);
                }
            }
            catch (NoSuchElementException)
            {
                heroi.YoutubeIframes = null;
            }

            return heroi;
        }
        private string GetField(string text, string label)
        {
            int index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = index + label.Length;
            int end = text.Length;

            var knownLabels = new[] {
                "Nome completo:", "Data de nascimento:", "Data de falecimento:",
                "Local de nascimento:", "Área de atuação:"
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