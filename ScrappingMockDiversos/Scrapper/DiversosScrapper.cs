using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockDiversos.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ScrappingMockDiversos.Models.CatalogoDiversos;

namespace ScrappingMockDiversos.Scrapper
{
    public class DiversosScrapper
    {
        private readonly IWebDriver _driver;

        public DiversosScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }
        public CatalogoDiversos ObterDiversos()
        {
            _driver.Navigate().GoToUrl("https://museuflamengo.com/acervo/tipo/diversos");

            CatalogoDiversos iconografias = new CatalogoDiversos();

            iconografias.Info.Titulo = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > h1")).Text.Trim();

            var style = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > h1")).GetAttribute("style");
            string imageUrl = "https://museuflamengo.com.br" + System.Text.RegularExpressions.Regex.Match(style, @"url\(['""]?(.*?)['""]?\)").Groups[1].Value;
            iconografias.Info.Icone = imageUrl;
            iconografias.Info.Descricao = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > p")).Text.Trim();

            var hrefs = _driver.FindElements(By.CssSelector("div.itens > div > a"));

            List<string> images = new List<string>();
            List<string> names = new List<string>();
            List<string> urls = new List<string>();

            foreach (var href in hrefs)
            {
                style = href.GetAttribute("style");
                names.Add(href.FindElement(By.CssSelector("b")).Text.Trim());
                images.Add("https://museuflamengo.com.br" + System.Text.RegularExpressions.Regex.Match(style, @"url\(['""]?(.*?)['""]?\)").Groups[1].Value);
                urls.Add(href.GetAttribute("href"));
            }


            for (int i = 0; i < urls.Count; i++)
            {
                iconografias.Diversoss.Add(new Diversos
                {
                    Nome = names[i],
                    Imagem = images[i],
                    Url = urls[i]
                });
                iconografias.Diversoss[i].DetalhesDiversos = ObterDadosDiversos(urls[i]);
            }

            _driver.Quit();
            return iconografias;
        }

        public void SalvarDiversosComoJson(CatalogoDiversos iconografias, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(iconografias, options);
            File.WriteAllText(caminho, json);
        }

        private Diversos.Detalhes ObterDadosDiversos(string url)
        {
            _driver.Navigate().GoToUrl(url);
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);
            Diversos.Detalhes vestimenta = new Diversos.Detalhes();

            vestimenta.Titulo = _driver.FindElement(By.CssSelector("div.lado_lado.acervoBox.single > div.texto > div > h1")).Text.Trim();
            var paragraphs = _driver.FindElements(By.CssSelector("div.lado_lado.acervoBox.single > div.texto > div > p"));

            foreach (var paragraph in paragraphs)
            {
                try
                {
                    var strong = paragraph.FindElement(By.TagName("strong"));

                    string fullText = paragraph.Text;
                    if (strong.Text.ToLower().Contains("modalidade"))
                    {
                        vestimenta.Modalidade = fullText.Replace(strong.Text, "").Trim();
                    }

                    else if (strong.Text.ToLower().Contains("especifica"))
                    {
                        vestimenta.EspecificacoesTecnicas = fullText.Replace(strong.Text, "").Trim();
                    }
                }
                catch { continue; }
            }
            var imagens = _driver.FindElements(By.CssSelector("div.lado_lado.acervoBox.single .slick-track .slick-slide:not(.slick-cloned) img"));

            foreach (var imagem in imagens)
            {
                vestimenta.CarrosselImagens.Add(imagem.GetAttribute("src"));
            }
            vestimenta.Ano = _driver.FindElement(By.CssSelector("div.lado_lado.acervoBox.single > div.item > div > p")).Text.Trim();

            var maisItens = _driver.FindElements(By.CssSelector(".cardHolder.slick-slide:not(.slick-cloned)"));

            foreach (var item in maisItens)
            {

                var nameElement = item.FindElement(By.CssSelector("span > b"));
                string nome = nameElement.Text.Trim();

                if (string.IsNullOrWhiteSpace(nome))
                {
                    nome = ((IJavaScriptExecutor)_driver)
                        .ExecuteScript("return arguments[0].innerText;", nameElement)
                        .ToString().Trim();
                }

                vestimenta.MaisItens.Add(new Diversos.Detalhes.Cards()
                {
                    Nome = nome,
                    Url = item.FindElement(By.TagName("a")).GetAttribute("href"),
                    Imagem = item.FindElement(By.TagName("img")).GetAttribute("src")
                });
            }

            return vestimenta;
        }
    }
}
