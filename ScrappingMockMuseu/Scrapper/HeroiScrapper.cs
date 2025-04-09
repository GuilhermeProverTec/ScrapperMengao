using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
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
            _driver.Navigate().GoToUrl(url);
            var heroi = new Heroi();

            try
            {
                heroi.Apelido = _driver.FindElement(By.CssSelector("div.heroBox > h1")).Text;
                var paragrafos = _driver.FindElements(By.CssSelector("div.heroBox > div.heroContent > p"));

                if (paragrafos.Count > 0) heroi.NomeCompleto = paragrafos[0].Text.Split('\n').LastOrDefault();
                if (paragrafos.Count > 1) heroi.DataNascimento = paragrafos[1].Text.Split('\n').LastOrDefault();
                if (paragrafos.Count > 2) heroi.DataFalecimento = paragrafos[2].Text.Split('\n').LastOrDefault();
                if (paragrafos.Count > 3) heroi.LocalNascimento = paragrafos[3].Text.Split('\n').LastOrDefault();
                if (paragrafos.Count > 4) heroi.AreaAtuacao = paragrafos[4].Text.Split('\n').LastOrDefault();

                // Textos
                var textos = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt h1, div.infoBox.fullWidth.stdCnt p"));
                foreach (var texto in textos)
                    heroi.Textos.Add(texto.Text.Trim());

                // Instagram
                var instagrams = _driver.FindElements(By.CssSelector("iframe.instagram-media"));
                heroi.InstagramIframes = instagrams.Select(x => x.GetAttribute("src")).ToList();

                // YouTube
                var iframesYoutube = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));
                heroi.YoutubeIframes = iframesYoutube.Select(x => x.GetAttribute("src")).ToList();

                // Carrossel de imagens
                // Carrossel de imagens
                var imagens = _driver.FindElements(By.CssSelector("#gallery-1 div div dl"));

                foreach (var dl in imagens)
                {
                    try
                    {
                        // Captura o HREF da imagem (link completo)
                        var src = dl.FindElement(By.CssSelector("dt a")).GetAttribute("href");

                        // Captura o crédito, se existir
                        var credito = dl.FindElements(By.CssSelector("dd.wp-caption-text")).FirstOrDefault()?.Text ?? "";

                        heroi.Imagens.Add((src, credito));
                    }
                    catch { continue; }
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
