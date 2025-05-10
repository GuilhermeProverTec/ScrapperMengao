using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using ScrappingMockBandeirasFaixasFlamulas.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static ScrappingMockBandeirasFaixasFlamulas.Models.CatalogoBandeiraFaixaFlamula;

namespace ScrappingMockBandeirasFaixasFlamulas.Scrapper
{
        public class BandeiraFaixaFlamulaScrapper
        {
            private readonly IWebDriver _driver;

            public BandeiraFaixaFlamulaScrapper()
            {
                var options = new ChromeOptions();
                options.AddArgument("--headless");
                _driver = new ChromeDriver(options);
            }
            public CatalogoBandeiraFaixaFlamula ObterBandeiraFaixaFlamulas()
            {
                _driver.Navigate().GoToUrl("https://museuflamengo.com/acervo/tipo/bandeiras-faixas-e-flamulas");

                CatalogoBandeiraFaixaFlamula BandeiraFaixaFlamulas = new CatalogoBandeiraFaixaFlamula();

                BandeiraFaixaFlamulas.Info.Titulo = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > h1")).Text.Trim();

                var style = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > h1")).GetAttribute("style");
                string imageUrl = "https://museuflamengo.com.br" + System.Text.RegularExpressions.Regex.Match(style, @"url\(['""]?(.*?)['""]?\)").Groups[1].Value;
                BandeiraFaixaFlamulas.Info.Icone = imageUrl;
                BandeiraFaixaFlamulas.Info.Descricao = _driver.FindElement(By.CssSelector("div.cabecalho > div > div > p")).Text.Trim();

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
                    BandeiraFaixaFlamulas.BandeiraFaixaFlamulas.Add(new BandeiraFaixaFlamula
                    {
                        Nome = names[i],
                        Imagem = images[i],
                        Url = urls[i]
                    });
                    BandeiraFaixaFlamulas.BandeiraFaixaFlamulas[i].DetalhesBandeiraFaixaFlamula = ObterDadosBandeiraFaixaFlamula(urls[i]);
                }

                _driver.Quit();
                return BandeiraFaixaFlamulas;
            }

            public void SalvarBandeiraFaixaFlamulaComoJson(CatalogoBandeiraFaixaFlamula BandeiraFaixaFlamulas, string caminho)
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(BandeiraFaixaFlamulas, options);
                File.WriteAllText(caminho, json);
            }

            private BandeiraFaixaFlamula.Detalhes ObterDadosBandeiraFaixaFlamula(string url)
            {
                _driver.Navigate().GoToUrl(url);
                _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
                _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);
                BandeiraFaixaFlamula.Detalhes BandeiraFaixaFlamula = new BandeiraFaixaFlamula.Detalhes();

                BandeiraFaixaFlamula.Titulo = _driver.FindElement(By.CssSelector("div.lado_lado.acervoBox.single > div.texto > div > h1")).Text.Trim();
                var paragraphs = _driver.FindElements(By.CssSelector("div.lado_lado.acervoBox.single > div.texto > div > p"));

                foreach (var paragraph in paragraphs)
                {
                    try
                    {
                        var strong = paragraph.FindElement(By.TagName("strong"));

                        string fullText = paragraph.Text;
                        if (strong.Text.ToLower().Contains("modalidade"))
                        {
                            BandeiraFaixaFlamula.Modalidade = fullText.Replace(strong.Text, "").Trim();
                        }

                        else if (strong.Text.ToLower().Contains("especifica"))
                        {
                            BandeiraFaixaFlamula.EspecificacoesTecnicas = fullText.Replace(strong.Text, "").Trim();
                        }
                    }
                    catch { continue; }
                }
                var imagens = _driver.FindElements(By.CssSelector("div.lado_lado.acervoBox.single .slick-track .slick-slide:not(.slick-cloned) img"));

                foreach (var imagem in imagens)
                {
                    BandeiraFaixaFlamula.CarrosselImagens.Add(imagem.GetAttribute("src"));
                }
                BandeiraFaixaFlamula.Ano = _driver.FindElement(By.CssSelector("div.lado_lado.acervoBox.single > div.item > div > p")).Text.Trim();

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

                    BandeiraFaixaFlamula.MaisItens.Add(new BandeiraFaixaFlamula.Detalhes.Cards()
                    {
                        Nome = nome,
                        Url = item.FindElement(By.TagName("a")).GetAttribute("href"),
                        Imagem = item.FindElement(By.TagName("img")).GetAttribute("src")
                    });
                }

                return BandeiraFaixaFlamula;
            }
        }
    }
