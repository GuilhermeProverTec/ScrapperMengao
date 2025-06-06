﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockIdolos.Models;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace ScrappingMockIdolos.Scrapper
{
    public class IdolosScrapper
    {
        private readonly IWebDriver _driver;

        public IdolosScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Idolo> ObterIdolos()
        {
            var idolos = new List<Idolo>();
            _driver.Navigate().GoToUrl("https://museuflamengo.com/personagens/idolos/basquete/");

            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrEmpty(href))
                .ToList();

            foreach (var href in hrefs)
            {
                var idolo = ObterDadosIdolo(href);
                if (idolo != null)
                    idolos.Add(idolo);
            }

            _driver.Quit();
            return idolos;
        }

        public void SalvarIdolosComoJson(List<Idolo> idolos, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(idolos, options);
            File.WriteAllText(caminho, json);
        }

        private Idolo ObterDadosIdolo(string url)
        {
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            var idolo = new Idolo();
            try
            {
                _driver.Navigate().GoToUrl(url);

                idolo.Apelido = _driver.FindElement(By.CssSelector("div.heroBox h1")).Text;

                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent > p"));
                var fullText = string.Join("\n", paragrafos.Select(p => p.Text));

                int countNomeCompleto = Regex.Matches(fullText, @"(?i)nome completo").Count;

                bool containsMultiple = paragrafos.Any(p => Regex.IsMatch(p.Text, @"\([^)]+\)"));

                bool isTeam = paragrafos.Count > 2 && paragrafos[0].Text.Trim().StartsWith("O time", StringComparison.OrdinalIgnoreCase);
                if (isTeam)
                {
                    for (int i = 1; i < paragrafos.Count; i++)
                    {
                        string nome = paragrafos[i].Text.Trim();
                        if (!string.IsNullOrWhiteSpace(nome))
                        {
                            idolo.DadosPessoais.Add(new DadosPessoais
                            {
                                NomeCompleto = nome
                            });
                        }
                    }                }


                else if (containsMultiple)
                {
                    foreach (var p in paragrafos)
                    {
                        var rawText = p.Text;

                        var match = Regex.Match(rawText,
                        @"^(?<nome>.+?)\s*\((?<apelido>[^)]+)\)\s*(?:<br>)?\s*Nascimento[:>]\s*(?<nascimento>[\d/]+)\s*[-–]\s*(?<local>.+?)(?:\s*<br>?\s*Falecimento[:>]\s*(?<falecimento>[\d/]+))?$",
                        RegexOptions.IgnoreCase);


                        if (match.Success)
                        {
                            var dadosPessoais = new DadosPessoais
                            {
                                NomeCompleto = match.Groups["nome"].Value.Trim(),
                                Apelido = match.Groups["apelido"].Value.Trim(),
                                DataNascimento = match.Groups["nascimento"].Value.Trim(),
                                LocalNascimento = match.Groups["local"].Value.Trim()
                            };

                            if (match.Groups["falecimento"].Success)
                                dadosPessoais.DataFalecimento = match.Groups["falecimento"].Value.Trim();


                            idolo.DadosPessoais.Add(dadosPessoais);
                        }
                        else if (rawText.Contains("Área de atuação", StringComparison.OrdinalIgnoreCase))
                        {
                            idolo.AreaAtuacao = GetField(rawText, "Área de atuação");
                        }
                    }
                }
                else
                {

                    var dadosPessoais = new DadosPessoais();

                    foreach (var p in paragrafos)
                    {
                        string rawText = p.Text;

                        var knownLabels = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "nome completo", val => dadosPessoais.NomeCompleto = val },
                            { "data de nascimento", val => dadosPessoais.DataNascimento = val },
                            { "data de falecimento", val => dadosPessoais.DataFalecimento = val },
                            { "local de nascimento", val => dadosPessoais.LocalNascimento = val },
                            { "área de atuação", val => idolo.AreaAtuacao = val }
                        };

                        foreach (var kvp in knownLabels)
                        {
                            var label = kvp.Key;
                            var assign = kvp.Value;

                            int index = rawText.IndexOf(label, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                string value = GetField(rawText, label);
                                assign(value);
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(dadosPessoais.NomeCompleto))
                        idolo.DadosPessoais.Add(dadosPessoais);

                }

                try
                {
                    var img = _driver.FindElement(By.CssSelector("div.heroBox > img"));
                    idolo.ImagemPersonalidade = img?.GetAttribute("src");
                }
                catch
                {
                    idolo.ImagemPersonalidade = null;
                }

                try
                {
                    idolo.TituloTexto = _driver.FindElement(By.CssSelector("div.infoBox.fullWidth.stdCnt > h1")).Text;
                }
                catch
                {
                    idolo.TituloTexto = null;
                }

                try
                {
                    var textos = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt > p"));
                    foreach (var texto in textos)
                        if (texto.Text.Trim() != "")
                            idolo.Textos.Add(texto.Text.Trim());
                }
                catch
                {
                    idolo.Textos = null;
                }

                try
                {
                    var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                    foreach (var imagem in imagens)
                    {
                        var img = imagem.FindElement(By.CssSelector("img"));
                        var legendaElement = imagem.FindElement(By.CssSelector("dd.wp-caption-text.gallery-caption"));
                        var legenda = legendaElement.GetAttribute("textContent")?.Trim();

                        var novaImagem = new Imagem
                        {
                            Url = img.GetAttribute("src"),
                            Descricao = legenda
                        };

                        if (!idolo.Imagens.Contains(novaImagem))
                        {
                            idolo.Imagens.Add(novaImagem);
                        }
                    }
                }
                catch
                {
                    idolo.Imagens = null;
                }

                try
                {
                    var iframes = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));
                    foreach (var iframe in iframes)
                    {
                        var src = iframe.GetAttribute("src");
                        if (src.Contains("instagram", StringComparison.OrdinalIgnoreCase))
                            idolo.InstagramIframes.Add(src);
                        else if (src.Contains("youtube", StringComparison.OrdinalIgnoreCase) || src.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                            idolo.YoutubeIframes.Add(src);
                    }
                }
                catch
                {
                    idolo.InstagramIframes = null;
                    idolo.YoutubeIframes = null;
                }
                try
                {
                    var maisHerois = _driver.FindElements(By.CssSelector(".cardHolder:not(.slick-cloned)"));
                    foreach (var maisHeroi in maisHerois)
                    {
                        var card = maisHeroi.FindElement(By.CssSelector("a.card"));
                        var urlNovoHeroi = card.GetAttribute("href");
                        var imagemNovoHeroi = maisHeroi.FindElement(By.TagName("img")).GetAttribute("src");
                        var infoNovoHeroi = card.FindElement(By.CssSelector("span")).GetAttribute("innerHTML");

                        string nome = null;
                        string areaAtuacao = null;

                        // Simple regex to match the pattern: <b>Nome</b>Área
                        var match = Regex.Match(infoNovoHeroi, @"<b>(.*?)<\/b>(.*)");

                        if (match.Success)
                        {
                            nome = match.Groups[1].Value.Trim();
                            areaAtuacao = match.Groups[2].Value.Trim();
                        }

                        var novoMaisHeroi = new MaisHerois
                        {
                            Nome = nome,
                            AreaAtuacao = areaAtuacao,
                            Url = nome,
                            Imagem = imagemNovoHeroi,
                        };

                        idolo.MaisHerois.Add(novoMaisHeroi);
                    }
                }
                catch
                {
                    idolo.MaisHerois = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            return idolo;
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