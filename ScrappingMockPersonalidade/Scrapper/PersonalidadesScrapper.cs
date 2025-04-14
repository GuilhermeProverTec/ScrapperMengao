using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrappingMockPersonalidades.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScrappingMockPersonalidades.Scrapper
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

        public List<Personalidade> ObterPersonalidades()
        {
            var personalidades = new List<Personalidade>();
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com.br/cultura-rubro-negra/personalidades/");

            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements.Select(link => link.GetAttribute("href")).Where(href => !string.IsNullOrEmpty(href)).ToList();


            foreach (var href in hrefs)
            {
                var personalidade = ObterDadosPersonalidade(href);
                if (personalidade != null)
                    personalidades.Add(personalidade);
            }

            _driver.Quit();
            return personalidades;
        }

        public void SalvarPersonalidadesComoJson(List<Personalidade> personalidades, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(personalidades, options);
            File.WriteAllText(caminho, json);
        }

        private Personalidade ObterDadosPersonalidade(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var personalidade = new Personalidade();

            try
            {
                _driver.Navigate().GoToUrl(url);

                personalidade.Apelido = _driver.FindElement(By.CssSelector("div.heroBox h1")).Text;

                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent > p"));
                var fullText = string.Join("\n", paragrafos.Select(p => p.Text));

                int countNomeCompleto = Regex.Matches(fullText, @"(?i)nome completo").Count;

                bool containsMultiple = paragrafos.Any(p => Regex.IsMatch(p.Text, @"\([^)]+\)")); // crude check for aliases in parentheses

                bool isTeam = paragrafos.Count > 2 && paragrafos[0].Text.Trim().StartsWith("O time", StringComparison.OrdinalIgnoreCase);
                if (isTeam)
                {
                    for (int i = 1; i < paragrafos.Count; i++) // skip the title line
                    {
                        string nome = paragrafos[i].Text.Trim();
                        if (!string.IsNullOrWhiteSpace(nome))
                        {
                            personalidade.DadosPessoais.Add(new DadosPessoais
                            {
                                NomeCompleto = nome
                            });
                        }
                    }
                }


                else if (countNomeCompleto > 1)
                {
                    foreach (var p in paragrafos)
                    {
                        var lines = p.Text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                                          .Select(line => line.Trim())
                                          .Where(line => !string.IsNullOrWhiteSpace(line))
                                          .ToList();

                        var dadosTemp = new DadosPessoais();
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("Nome completo", StringComparison.OrdinalIgnoreCase))
                            {
                                if (!string.IsNullOrWhiteSpace(dadosTemp.NomeCompleto))
                                {
                                    // Already filled a person, save and start new
                                    personalidade.DadosPessoais.Add(dadosTemp);
                                    dadosTemp = new DadosPessoais();
                                }

                                dadosTemp.NomeCompleto = GetField(line, "Nome completo");
                            }
                            else if (line.StartsWith("Data de nascimento", StringComparison.OrdinalIgnoreCase))
                            {
                                dadosTemp.DataNascimento = GetField(line, "Data de nascimento");
                            }
                            else if (line.StartsWith("Data de falecimento", StringComparison.OrdinalIgnoreCase))
                            {
                                dadosTemp.DataFalecimento = GetField(line, "Data de falecimento");
                            }
                            else if (line.StartsWith("Local de nascimento", StringComparison.OrdinalIgnoreCase))
                            {
                                dadosTemp.LocalNascimento = GetField(line, "Local de nascimento");
                            }
                            else if (line.StartsWith("Área de atuação", StringComparison.OrdinalIgnoreCase))
                            {
                                personalidade.AreaAtuacao = GetField(line, "Área de atuação");
                                // Save the last one
                                if (!string.IsNullOrWhiteSpace(dadosTemp.NomeCompleto))
                                    personalidade.DadosPessoais.Add(dadosTemp);
                            }
                        }

                        // Catch any leftovers
                        if (!string.IsNullOrWhiteSpace(dadosTemp.NomeCompleto) &&
                            !personalidade.DadosPessoais.Any(d => d.NomeCompleto == dadosTemp.NomeCompleto))
                        {
                            personalidade.DadosPessoais.Add(dadosTemp);
                        }
                    }
                }

                else if (paragrafos.Count <= 2)
                {
                    var dadosPessoais = new DadosPessoais();

                    foreach (var p in paragrafos)
                    {
                        var html = p.GetAttribute("innerHTML");
                        if (string.IsNullOrWhiteSpace(html)) continue;

                        // Normalize <br> tags
                        var parts = Regex.Split(html, @"<br\s*/?>", RegexOptions.IgnoreCase);

                        foreach (var part in parts)
                        {
                            if (string.IsNullOrWhiteSpace(part)) continue;

                            // Extract label and value
                            var labelMatch = Regex.Match(part, @"<strong>(.*?)<\/strong>", RegexOptions.IgnoreCase);
                            if (!labelMatch.Success) continue;

                            var rawLabel = labelMatch.Groups[1].Value;
                            var label = Regex.Replace(rawLabel, "<.*?>", "").Trim().Trim(':').ToLower();

                            // Remove the <strong> part to get the value
                            var value = Regex.Replace(part, @"<strong>.*?<\/strong>", "", RegexOptions.IgnoreCase).Trim();
                            value = Regex.Replace(value, "<.*?>", "").Trim();

                            Console.WriteLine($"[INLINE] Found: {label} => {value}");

                            switch (label)
                            {
                                case "nome":
                                case "nome completo":
                                    dadosPessoais.NomeCompleto = value;
                                    break;
                                case "data de nascimento":
                                    dadosPessoais.DataNascimento = value;
                                    break;
                                case "data de falecimento":
                                    dadosPessoais.DataFalecimento = value;
                                    break;
                                case "local de nascimento":
                                    dadosPessoais.LocalNascimento = value;
                                    break;
                                case "área de atuação":
                                    personalidade.AreaAtuacao = value;
                                    break;
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(dadosPessoais.NomeCompleto))
                    {
                        personalidade.DadosPessoais.Add(dadosPessoais);
                    }
                }




                else
                {
                    var dadosPessoais = new DadosPessoais();
                    string? pendingLabel = null;
                    foreach (var p in paragrafos)
                    {
                        string rawHtml = p.GetAttribute("innerHTML");
                        string label = "";
                        string value = "";

                        // Extract content from <strong>...</strong> as label
                        var labelMatch = Regex.Match(rawHtml, @"<strong>(.*?)<\/strong>", RegexOptions.Singleline | RegexOptions.IgnoreCase);
                        if (labelMatch.Success)
                        {
                            label = Regex.Replace(labelMatch.Groups[1].Value, "<.*?>", "").Trim(':', ' ', '\n', '\r').ToLower();

                            // Remove the <strong> part from innerHTML to get value
                            value = Regex.Replace(rawHtml, @"<strong>.*?<\/strong>", "", RegexOptions.Singleline).Trim();
                            value = Regex.Replace(value, "<.*?>", "").Trim();
                        }

                        if (string.IsNullOrEmpty(label) || string.IsNullOrEmpty(value)) continue;

                        switch (label)
                        {
                            case "nome":
                            case "nome completo":
                                dadosPessoais.NomeCompleto = value;
                                break;
                            case "data de nascimento":
                                dadosPessoais.DataNascimento = value;
                                break;
                            case "data de falecimento":
                                dadosPessoais.DataFalecimento = value;
                                break;
                            case "local de nascimento":
                                dadosPessoais.LocalNascimento = value;
                                break;
                            case "área de atuação":
                                personalidade.AreaAtuacao = value;
                                break;
                        }
                    }


                    if (!string.IsNullOrWhiteSpace(dadosPessoais.NomeCompleto))
                    {
                        personalidade.DadosPessoais.Add(dadosPessoais);
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
                        personalidade.ImagemPersonalidade.Add(src);
                    }
                }
            }
            catch (Exception e)
            {
                personalidade.ImagemPersonalidade = null;
            }

            try
            {
                personalidade.TituloTexto = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt > h1")).Text;
            }
            catch (Exception e)
            {
                personalidade.TituloTexto = null;
            }

            try
            {
                var textos = _driver.FindElements(By.CssSelector("div.fullWidth.sobre.stdCnt > p"));
                foreach (var texto in textos)
                    personalidade.Textos.Add(texto.Text.Trim());
            }
            catch (Exception e)
            {
                personalidade.Textos = null;
            }

            try
            {
                var saibaMais = _driver.FindElements(By.CssSelector("div.fullWidth.saiba_mais p a"));
                foreach (var saibaMaisElement in saibaMais)
                {
                    personalidade.SaibaMais.Add(new Imagem
                    {
                        Url = saibaMaisElement.GetAttribute("href"),
                        Legenda = saibaMaisElement.Text.Trim()
                    });
                }
            }
            catch (Exception e)
            {
                personalidade.SaibaMais = null;
            }

            try
            {
                var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                foreach (var imagem in imagens)
                {
                    var imageElement = imagem.FindElement(By.CssSelector("img"));
                    personalidade.Imagens.Add(new Imagem
                    {
                        Url = imageElement.GetAttribute("src"),
                        Legenda = imagem.FindElement(By.CssSelector("dd.gallery-caption")).Text.Trim()
                    });
                }
            }
            catch (Exception e)
            {
                personalidade.Imagens = null;
            }

            try
            {
                var imagem = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt")); ;
                var imageElement = imagem.FindElement(By.CssSelector("img.wp-image-3375"));
                personalidade.ImagemTexto.Add(new Imagem
                {
                    Url = imageElement.GetAttribute("src"),
                    Legenda = imagem.FindElement(By.CssSelector(".wp-caption-text")).Text.Trim()
                });

            }
            catch (Exception e)
            {
                personalidade.ImagemTexto = null;
            }

            try
            {
                var saibaMaisSection = _driver.FindElement(By.CssSelector("div.fullWidth.saiba_mais"));
                var iframeElements = saibaMaisSection.FindElements(By.TagName("iframe"));

                foreach (var iframe in iframeElements)
                {
                    var src = iframe.GetAttribute("src");
                    if (src.Contains("youtube"))
                        personalidade.YoutubeIframes.Add(src);
                }
            }
            catch (NoSuchElementException)
            {
                personalidade.YoutubeIframes = null;
            }

            return personalidade;
        }
        private string GetField(string text, string label)
        {
            int index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = index + label.Length;
            int end = text.Length;

            var knownLabels = new[] {
                "Nome completo:", "Nome:", "Data de nascimento:", "Data de falecimento:",
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