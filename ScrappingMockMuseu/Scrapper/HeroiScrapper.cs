﻿using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockHeroi.Models;
using System.Text.Json;
using System.Text.RegularExpressions;


namespace ScrappingMockHeroi.Scrapper
{
    public class HeroiScrapper
    {
        private readonly IWebDriver _driver;

        public HeroiScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Heroi> ObterHerois()
        {
            var herois = new List<Heroi>();
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com/herois");


            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrEmpty(href))
                .ToList();

            foreach (var href in hrefs)
            {
                var heroi = ObterDadosHeroi(href);
                if (heroi != null)
                    herois.Add(heroi);
            }

            _driver.Quit();
            return herois;
        }

        public void SalvarHeroisComoJson(List<Heroi> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Heroi ObterDadosHeroi(string url)
        {
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            var heroi = new Heroi();
            try
            {
                _driver.Navigate().GoToUrl(url);

                heroi.Apelido = _driver.FindElement(By.CssSelector("div.heroBox h1")).Text;

                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent > p"));
                var fullText = string.Join("\n", paragrafos.Select(p => p.Text));

                int countNomeCompleto = Regex.Matches(fullText, @"(?i)nome completo").Count;

                bool containsMultiple = paragrafos.Any(p => Regex.IsMatch(p.Text, @"\([^)]+\)")); 

                bool isTeam = paragrafos.Count > 2 && (paragrafos[0].Text.Trim().StartsWith("O time", StringComparison.OrdinalIgnoreCase));
                if (isTeam)
                {
                    for (int i = 1; i < paragrafos.Count; i++) 
                    {
                        string nome = paragrafos[i].Text.Trim();
                        if (!string.IsNullOrWhiteSpace(nome))
                        {
                            heroi.DadosPessoais.Add(new DadosPessoais
                            {
                                NomeCompleto = nome
                            });
                        }
                    }                
                }

                else if (Regex.IsMatch(fullText, @"Quem são[:\s]*", RegexOptions.IgnoreCase))
                {
                    DadosPessoais current = null;
                    bool lastLineWasAreaAtuacao = false;

                    foreach (var p in paragrafos)
                    {
                        var lines = p.Text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

                        foreach (var lineRaw in lines)
                        {
                            var raw = lineRaw.Trim().Trim('"').Trim();

                            if (string.IsNullOrWhiteSpace(raw))
                                continue;
                            if (raw.StartsWith("Área de atuação", StringComparison.OrdinalIgnoreCase))
                            {
                                lastLineWasAreaAtuacao = true;
                                continue;
                            }

                            if (lastLineWasAreaAtuacao)
                            {
                                heroi.AreaAtuacao = raw;
                                lastLineWasAreaAtuacao = false;
                                continue;
                            }

                            if (!raw.Contains(":"))
                            {
                                if (current != null)
                                    heroi.DadosPessoais.Add(current);

                                current = new DadosPessoais
                                {
                                    NomeCompleto = raw
                                };
                            }
                            else if (raw.StartsWith("Nascimento", StringComparison.OrdinalIgnoreCase))
                            {
                                var nascimento = Regex.Match(raw, @"Nascimento[:\s]+([\d/]+)");
                                if (nascimento.Success && current != null)
                                    current.DataNascimento = nascimento.Groups[1].Value.Trim();
                            }
                            else if (raw.StartsWith("Local de Nascimento", StringComparison.OrdinalIgnoreCase))
                            {
                                var local = Regex.Match(raw, @"Local de Nascimento[:\s]+(.+)");
                                if (local.Success && current != null)
                                    current.LocalNascimento = local.Groups[1].Value.Trim();
                            }
                            else if (raw.StartsWith("Falecimento", StringComparison.OrdinalIgnoreCase))
                            {
                                var falecimento = Regex.Match(raw, @"Falecimento[:\s]+(.+)");
                                if (falecimento.Success && current != null)
                                    current.DataFalecimento = falecimento.Groups[1].Value.Trim();
                            }
                        }
                    }

                    if (current != null)
                        heroi.DadosPessoais.Add(current);
                }




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


                            heroi.DadosPessoais.Add(dadosPessoais);
                        }
                        else if (rawText.Contains("Área de atuação", StringComparison.OrdinalIgnoreCase))
                        {
                            heroi.AreaAtuacao = GetField(rawText, "Área de atuação");
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
                            { "área de atuação", val => heroi.AreaAtuacao = val }
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
                        heroi.DadosPessoais.Add(dadosPessoais);

                }


                // Imagem do herói
                try
                {
                    var img = _driver.FindElement(By.CssSelector("div.heroBox > img"));
                    heroi.ImagemPersonalidade = img?.GetAttribute("src");
                }
                catch
                {
                    heroi.ImagemPersonalidade = null;
                }

                try
                {
                    heroi.TituloTexto = _driver.FindElement(By.CssSelector("div.infoBox.fullWidth.stdCnt > h1")).Text;
                }
                catch
                {
                    heroi.TituloTexto = null;
                }

                try
                {
                    var textos = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt > p"));
                    foreach (var texto in textos)
                        if (texto.Text.Trim() != "")
                            heroi.Textos.Add(texto.Text.Trim());
                }
                catch
                {
                    heroi.Textos = null;
                }

                try
                {
                    var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide:not(.slick-cloned)"));
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

                        if (!heroi.Imagens.Contains(novaImagem))
                        {
                            heroi.Imagens.Add(novaImagem);
                        }
                    }
                }
                catch
                {
                    heroi.Imagens = null;
                }

                try
                {
                    var iframes = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));
                    foreach (var iframe in iframes)
                    {
                        var src = iframe.GetAttribute("src");
                        if (src.Contains("instagram", StringComparison.OrdinalIgnoreCase))
                            heroi.InstagramIframes.Add(src);
                        else if (src.Contains("youtube", StringComparison.OrdinalIgnoreCase) || src.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                            heroi.YoutubeIframes.Add(src);
                    }
                }
                catch
                {
                    heroi.InstagramIframes = null;
                    heroi.YoutubeIframes = null;
                }
                try
                {
                    var maisHerois = _driver.FindElements(By.CssSelector(".cardHolder:not(.slick-cloned)"));
                    foreach(var maisHeroi in maisHerois)
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

                        heroi.MaisHerois.Add(novoMaisHeroi);
                    }
                }
                catch
                {
                    heroi.MaisHerois = null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
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