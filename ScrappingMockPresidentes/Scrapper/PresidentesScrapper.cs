using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrappingMockPresidentes.Models;
using System.Text.Json;

namespace ScrappingMockPresidentes.Scrapper
{
    public class PresidentesScrapper
    {
        private readonly IWebDriver _driver;

        public PresidentesScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Presidente> ObterPresidentes()
        {
            var presidentes = new List<Presidente>();
            _driver.Navigate().GoToUrl("https://museuflamengo.com/presidentes");

            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements.Select(link => link.GetAttribute("href")).Where(href => !string.IsNullOrEmpty(href)).ToList();


            foreach (var href in hrefs)
            {
                var presidente = ObterDadosPresidente(href);
                if (presidente != null)
                    presidentes.Add(presidente);
            }

            _driver.Quit();
            return presidentes;
        }

        public void SalvarPresidentesComoJson(List<Presidente> presidentes, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(presidentes, options);
            File.WriteAllText(caminho, json);
        }

        private Presidente ObterDadosPresidente(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var presidente = new Presidente();

            try
            {
                presidente.Nome = _driver.FindElement(By.CssSelector("body > div.container > div > div.heroBox.lado_lado > div.texto.titulo.titulo-sub-vermelho > div.heroContent.ficha_tecnica > h1")).Text;
                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent.ficha_tecnica > p"));
                string lastLabel = null;

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

                    // Match and assign
                    if (label.Contains("data de nascimento"))
                        presidente.DataNascimento = value;
                    else if (label.Contains("local de nascimento"))
                        presidente.LocalNascimento = value;
                    else if (label.Contains("data de falecimento"))
                        presidente.DataFalecimento = value;
                    else if (label.Contains("profissão"))
                        presidente.Profissao = value;
                    else if (label.Contains("mandato"))
                        presidente.Mandato += (string.IsNullOrEmpty(presidente.Mandato) ? "" : "\n") + value;
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10)); // Ajuste o tempo conforme necessário
            try { 
                IWebElement imgElement = wait.Until(driver => driver.FindElement(By.CssSelector("div.imagem img")));
                var src = imgElement.GetAttribute("src");
                presidente.Imagem = src;
            }
            catch(Exception e)
            {
                presidente.Imagem = null;
            }
            try
            {
                presidente.Observacao = _driver.FindElement(By.CssSelector("body > div.container > div > div.fullWidth.saiba_mais > p")).Text;
            }
            catch (Exception e)
            {
                presidente.Observacao = null;
            }
            return presidente;
        }
    }
}
