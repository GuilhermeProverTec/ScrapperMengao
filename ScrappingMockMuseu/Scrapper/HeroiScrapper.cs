using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrappingMockMuseu.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScrappingMockMuseu.Scrapper
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

                // Detect if it's a "group-style" page
                bool isGroupLayout =
                    paragrafos.Count > 1 &&
                    paragrafos.First().Text.Trim().ToLower().Contains("o time do") ||
                    heroi.Apelido.Contains(",") || // like "Angelú, Engole Garfo e Bocca Larga"
                    paragrafos.Any(p => p.Text.Contains("Nascimento>") || p.Text.Contains("Nascimento:"));

                // If it's a group-style page, just store raw text paragraphs
                if (isGroupLayout)
                {
                    foreach (var p in paragrafos)
                    {
                        string text = p.Text.Trim();
                        if (!string.IsNullOrWhiteSpace(text))
                            heroi.Textos.Add(text);
                    }
                }
                else
                {
                    // Regular parsing logic for individual heroes
                    foreach (var p in paragrafos)
                    {
                        string label = null;
                        string value = null;

                        try
                        {
                            var labelElements = p.FindElements(By.CssSelector("strong, b"));
                            var labelElement = labelElements.FirstOrDefault(e => !string.IsNullOrWhiteSpace(e.Text));

                            if (labelElement != null)
                            {
                                label = labelElement.Text.Trim().ToLower();
                                value = p.Text.Replace(labelElement.Text, "").Trim();
                                lastLabel = label;
                            }
                            else
                            {
                                value = p.Text.Trim();
                                label = lastLabel;
                            }
                        }
                        catch
                        {
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(label))
                            continue;

                        // Match and assign based on labels
                        if (label.Contains("nome completo"))
                            heroi.NomeCompleto = value;
                        else if (label.Contains("nome"))
                            heroi.NomeCompleto = value;
                        else if (label.Contains("área de atuação"))
                            heroi.AreaAtuacao = value;
                        else if (label.Contains("data de nascimento"))
                            heroi.DataNascimento = value;
                        else if (label.Contains("local de nascimento"))
                            heroi.LocalNascimento = value;
                        else if (label.Contains("data de falecimento"))
                            heroi.DataFalecimento = value;
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
                    var iframeElements = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));

                    foreach (var iframe in iframeElements)
                    {
                        var src = iframe.GetAttribute("src");

                        if (string.IsNullOrEmpty(src))
                            continue;

                        if (src.Contains("instagram.com", StringComparison.OrdinalIgnoreCase))
                        {
                            heroi.InstagramIframes.Add(src);
                        }
                        else if (src.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) || src.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                        {
                            heroi.YoutubeIframes.Add(src);
                        }
                    }
                }
                catch (Exception)
                {
                    heroi.InstagramIframes = null;
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
    }
}
