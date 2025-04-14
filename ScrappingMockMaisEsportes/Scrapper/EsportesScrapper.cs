using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockMaisEsportes.Models;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScrappingMockMaisEsportes.Scrapper
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

        public List<Esporte> ObterEsportes()
        {
            var esportes = new List<Esporte>();
            _driver.Navigate().GoToUrl("https://museuflamengo.com/mais-esportes");

            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements.Select(link => link.GetAttribute("href")).Where(href => !string.IsNullOrEmpty(href)).ToList();

            foreach (var href in hrefs)
            {
                var esporte = ObterDadosEsporte(href);
                if (esporte != null)
                    esportes.Add(esporte);
            }

            _driver.Quit();
            return esportes;
        }

        public void SalvarEsportesComoJson(List<Esporte> esportes, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(esportes, options);
            File.WriteAllText(caminho, json);
        }

        private Esporte ObterDadosEsporte(string url)
        {
            _driver.Navigate().GoToUrl(url);
            var esporte = new Esporte();

            try
            {
                esporte.Nome = _driver.FindElement(By.CssSelector("body > div.container > div > div.lado_lado.heroBox > div.texto.titulo-sublinhado.titulo-sublinhado-vermelho > div > h1")).Text;

                try
                {
                    var anoElement = _driver.FindElement(By.CssSelector("body > div.container > div > div.lado_lado.heroBox > div.texto.titulo-sublinhado.titulo-sublinhado-vermelho > div > p"));
                    var anoText = anoElement.Text;

                    var match = Regex.Match(anoText, @"\d{4}");
                    if (match.Success)
                    {
                        esporte.Ano = match.Value;
                    }
                    else
                    {
                        esporte.Ano = null;
                    }
                }
                catch (NoSuchElementException)
                {
                    esporte.Ano = null;
                }

                var imagens = _driver.FindElements(By.CssSelector("div.carrossel.carrosselIMG img"));
                foreach (var img in imagens)
                {
                    var novaImagem = new Imagem
                    {
                        Url = img.GetAttribute("src"),
                        Descricao = img.GetAttribute("alt")
                    };
                    if (!esporte.Imagens.Contains(novaImagem))
                    {
                        esporte.Imagens.Add(novaImagem);
                    }
                }

                var textos = _driver.FindElements(By.CssSelector("div.fullWidth.min-padding.sobre.stdCnt > p"));
                foreach (var texto in textos)
                {
                    var textoContent = texto.Text.Trim();

                    if (textoContent.Contains("Saiba mais"))
                    {
                        var saibaMaisUrl = textoContent.Replace("Saiba mais: ", "").Trim();

                        var imagem = new Imagem
                        {
                            Url = saibaMaisUrl,
                            Descricao = "Saiba Mais Link"
                        };
                        esporte.SaibaMais.Add(imagem);
                    }
                    else
                    {
                        if (textoContent.Trim() != "")
                            esporte.Textos.Add(textoContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            return esporte;
        }




    }
}
