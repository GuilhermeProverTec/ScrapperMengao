using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockAcervo.Models;
using System.Text.Json;

namespace ScrappingMockAcervo.Scrapper
{
    public class AcervoScrapper
    {
        private readonly IWebDriver _driver;

        public AcervoScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }
        public Acervo ObterAcervo()
        {
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com.br/acervo");

            var acervo = ObterDadosAcervo();

            _driver.Quit();
            return acervo;
        }

        public void SalvarArquibancadaComoJson(Acervo acervo, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(acervo, options);
            File.WriteAllText(caminho, json);
        }

        private Acervo ObterDadosAcervo()
        {
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);
            Acervo acervo = new Acervo();

            acervo.Titulo = _driver.FindElement(By.CssSelector("div > h1")).Text;
            acervo.Texto = _driver.FindElement(By.CssSelector("div > p")).Text.Trim();

            var icones = _driver.FindElements(By.CssSelector("div.categorias > div > a"));
            foreach (var icone in icones)
            {
                var style = icone.GetAttribute("style");
                string imageUrl = "https://museuflamengo.com.br" + System.Text.RegularExpressions.Regex.Match(style, @"url\(['""]?(.*?)['""]?\)").Groups[1].Value;
                string href = icone.GetAttribute("href");
                string nomeIcone = icone.FindElement(By.CssSelector("span")).Text;
                acervo.Icones.Add(new Icone() { 
                    Nome = nomeIcone,
                    URL = href,
                    Imagem = imageUrl
                });
            }
            return acervo;
        }
    }
}
