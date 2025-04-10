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
using System.Xml;

namespace ScrappingMockMuseu.Scrapper
{
    public class EsportesScrapper
    {
        private readonly IWebDriver _driver;

        public EsportesScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Esportes> ObterHerois()
        {
            var herois = new List<Esportes>();
            _driver.Navigate().GoToUrl("https://museuflamengo.com/mais-esportes");

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

        public void SalvarHeroisComoJson(List<Esportes> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Esportes ObterDadosHeroi(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var heroi = new Esportes();

            try
            {
                heroi.Nome = _driver.FindElement(By.CssSelector("body > div.container > div > div.lado_lado.heroBox > div.texto.titulo-sublinhado.titulo-sublinhado-vermelho > div > h1")).Text;
                try
                {
                    var anoElement = _driver.FindElement(By.CssSelector("body > div.container > div > div.lado_lado.heroBox > div.texto.titulo-sublinhado.titulo-sublinhado-vermelho > div > p"));
                    heroi.Ano = anoElement.Text;
                }
                catch (NoSuchElementException)
                {
                    heroi.Ano = null; // Set a default value if <p> is not found
                }

                var nomes = _driver.FindElements(By.CssSelector("div.heroBox > div.slick-list.draggable > div.slick-track > p"));
                foreach (var nome in nomes)
                    heroi.NomesAtletas.Add(nome.Text.Trim());

                // Textos
                var textos = _driver.FindElements(By.CssSelector("div.fullWidth.min-padding.sobre.stdCnt > p"));
                foreach (var texto in textos)
                    heroi.Textos.Add(texto.Text.Trim());

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
