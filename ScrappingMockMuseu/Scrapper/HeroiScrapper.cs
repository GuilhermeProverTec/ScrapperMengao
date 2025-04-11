using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrappingMockMuseu.Models;
using System.Text.Json;

namespace ScrappingMockMuseu.Scrapper
{
    public class HeroiScraper
    {
        private readonly IWebDriver _driver;

        public HeroiScraper()
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

        public void SalvarHeroisComoJson(List<Heroi> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Heroi ObterDadosHeroi(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var heroi = new Heroi();

            try
            {
                heroi.Apelido = _driver.FindElement(By.CssSelector("div.heroBox h1")).Text;
                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent > p"));
                string lastLabel = null;

                if (paragrafos.Count > 3)
                {
                    foreach (var p in paragrafos)
                    {
                        string label = null;
                        string value = null;

                        try
                        {
                            var labelElements = p.FindElements(By.CssSelector("strong, b"));

                            // Pick the first label element that has text
                            var labelElement = labelElements.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Text));

                            if (labelElement != null)
                            {
                                label = labelElement.Text.Trim().ToLower();
                                value = p.Text.Replace(labelElement.Text, "").Trim();
                                lastLabel = label;
                            }
                            else
                            {
                                // No valid label, treat as continuation
                                value = p.Text.Trim();
                                label = lastLabel;
                            }
                        }
                        catch (Exception ex)
                        {
                            // Something weird? Skip.
                            Console.WriteLine($"Erro ao processar parágrafo: {ex.Message}");
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(label))
                            continue;

                        // Match and assign based on different label types
                        if (label.Contains("nome completo"))
                            heroi.NomeCompleto = value;
                        if (label.Contains("nome"))
                            heroi.NomeCompleto = value;
                        else if (label.Contains("data de nascimento"))
                            heroi.DataNascimento = value;
                        else if (label.Contains("local de nascimento"))
                            heroi.LocalNascimento = value;
                        else if (label.Contains("data de falecimento"))
                            heroi.DataFalecimento = value;
                        else if (label.Contains("área de atuação"))
                            heroi.AreaAtuacao = value;
                    }
                }
                else
                {
                    // Handle case where all data is inside a single <p> tag
                    string fullText = paragrafos.FirstOrDefault()?.Text;
                    var label = fullText.ToLower();

                    if (!string.IsNullOrEmpty(fullText))
                    {
                        // Split the full text based on known labels
                        if (label.Contains("nome completo"))
                            heroi.NomeCompleto = ExtractValue(fullText, "nome completo");
                        else if (label.Contains("nome"))
                            heroi.NomeCompleto = ExtractValue(fullText, "nome");
                        else if (label.Contains("data de nascimento"))
                            heroi.DataNascimento = ExtractValue(fullText, "data de nascimento");
                        else if (label.Contains("local de nascimento"))
                            heroi.LocalNascimento = ExtractValue(fullText, "local de nascimento");
                        else if (label.Contains("data de falecimento"))
                            heroi.DataFalecimento = ExtractValue(fullText, "data de falecimento");
                        else if (label.Contains("área de atuação"))
                            heroi.AreaAtuacao = ExtractValue(fullText, "área de atuação");
                    }
                }

                WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10)); // Ajuste o tempo conforme necessário
                try
                {
                    wait.Until(driver => driver.FindElement(By.CssSelector("div.heroBox > img")));

                    // Get all <img> elements inside the slick-track
                    var imgElements = _driver.FindElement(By.CssSelector("div.heroBox > img"));

                    // Use HashSet to avoid duplicates from cloned slides
                    var imageUrls = new HashSet<string>();

                    var src = imgElements.GetAttribute("src");
                    heroi.ImagemPersonalidade = src;

                }
                catch (Exception e)
                {
                    heroi.ImagemPersonalidade = null;
                }

                try
                {
                    heroi.TituloTexto = _driver.FindElement(By.CssSelector("div.infoBox.fullWidth.stdCnt > h1")).Text;
                }
                catch (Exception e)
                {
                    heroi.TituloTexto = null;
                }

                try
                {
                    var textos = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt > p"));
                    foreach (var texto in textos)
                        heroi.Textos.Add(texto.Text.Trim());
                }
                catch (Exception e)
                {
                    heroi.Textos = null;
                }

                try
                {
                    var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                    foreach (var imagem in imagens)
                    {
                        var imageElement = imagem.FindElement(By.CssSelector("img"));
                        heroi.Imagens.Add(new Image
                        {
                            Url = imageElement.GetAttribute("src"),
                            Descricao = imagem.FindElement(By.CssSelector("dd.gallery-caption")).Text.Trim()
                        });
                    }
                }
                catch (Exception e)
                {
                    heroi.Imagens = null;
                }

                try
                {
                    var saibaMaisSection = _driver.FindElement(By.CssSelector("div.infoBox.fullWidth.stdCnt"));
                    var iframeElements = saibaMaisSection.FindElements(By.TagName("iframe.instagram-media"));

                    foreach (var iframe in iframeElements)
                    {
                        heroi.InstagramIframes.Add(iframe.GetAttribute("src"));
                    }
                }
                catch (NoSuchElementException)
                {
                    heroi.InstagramIframes = null;
                }

                try
                {
                    var iframeElements = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));

                    foreach (var iframe in iframeElements)
                    {
                        heroi.YoutubeIframes.Add(iframe.GetAttribute("src"));
                    }
                }
                catch (NoSuchElementException)
                {
                    heroi.YoutubeIframes = null;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            return heroi;
        }

        private string ExtractValue(string fullText, string label)
        {
            var temp = fullText.ToLower();
            var index = temp.IndexOf(label);

            if (index != -1)
            {
                return fullText.Substring(index + label.Length).Split("\r").FirstOrDefault().Split(":").LastOrDefault().Trim();
            }
            return string.Empty;
        }
    }
}
