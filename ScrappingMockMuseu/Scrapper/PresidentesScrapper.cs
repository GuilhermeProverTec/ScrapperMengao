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

namespace ScrappingMockMuseu.Scrapper
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

        public List<Presidente> ObterHerois()
        {
            var herois = new List<Presidente>();
            _driver.Navigate().GoToUrl("https://museuflamengo.com/presidentes");

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

        public void SalvarHeroisComoJson(List<Presidente> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Presidente ObterDadosHeroi(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var heroi = new Presidente();

            try
            {
                heroi.Nome = _driver.FindElement(By.CssSelector("body > div.container > div > div.heroBox.lado_lado > div.texto.titulo.titulo-sub-vermelho > div.heroContent.ficha_tecnica > h1")).Text;
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
                        heroi.DataNascimento = value;
                    else if (label.Contains("local de nascimento"))
                        heroi.LocalNascimento = value;
                    else if (label.Contains("data de falecimento"))
                        heroi.DataFalecimento = value;
                    else if (label.Contains("profissão"))
                        heroi.Profissao = value;
                    else if (label.Contains("mandato"))
                        heroi.Mandato += (string.IsNullOrEmpty(heroi.Mandato) ? "" : "\n") + value;
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
                heroi.Imagem = src;
            }
            catch(Exception e)
            {
                heroi.Imagem = null;
            }
            try
            {
                heroi.Observacao = _driver.FindElement(By.CssSelector("body > div.container > div > div.fullWidth.saiba_mais > p")).Text;
            }
            catch (Exception e)
            {
                heroi.Observacao = null;
            }
            return heroi;
        }
    }
}
