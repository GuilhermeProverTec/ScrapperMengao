using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using ScrappingMockMuseu.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            //_driver.Navigate().GoToUrl("https://www.museuflamengo.com/herois");
           // _driver.Navigate().GoToUrl("https://museuflamengo.com/personagens/idolos/futebol/");
           // _driver.Navigate().GoToUrl("https://museuflamengo.com/personagens/idolos/basquete/");
           _driver.Navigate().GoToUrl("https://museuflamengo.com/personagens/idolos/remo/"); 


            var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
            var hrefs = linkElements
                .Select(link => link.GetAttribute("href"))
                .Where(href => !string.IsNullOrEmpty(href))
                .ToList();

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
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            var heroi = new Heroi();
            try
            {
                _driver.Navigate().GoToUrl(url);

                heroi.Apelido = _driver.FindElement(By.CssSelector("div.heroBox h1")).Text;

                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent > p"));
                var fullText = string.Join("\n", paragrafos.Select(p => p.Text));

                int countNomeCompleto = Regex.Matches(fullText, @"(?i)nome completo").Count;

                if (countNomeCompleto > 1)
                {
                    // Handle special cases (e.g. multiple people)
                    // [Optional]: Implement logic if needed
                }
                else
                {
                    foreach (var p in paragrafos)
                    {
                        string rawText = p.Text;

                        var knownLabels = new Dictionary<string, Action<string>>(StringComparer.OrdinalIgnoreCase)
                        {
                            { "nome completo", val => heroi.NomeCompleto = val },
                            { "data de nascimento", val => heroi.DataNascimento = val },
                            { "data de falecimento", val => heroi.DataFalecimento = val },
                            { "local de nascimento", val => heroi.LocalNascimento = val },
                            { "área de atuação", val => heroi.AreaAtuacao = val }
                        };

                        foreach (var kvp in knownLabels)
                        {
                            var label = kvp.Key;
                            var assign = kvp.Value;

                            int index = rawText.IndexOf(label, StringComparison.OrdinalIgnoreCase);
                            if (index >= 0)
                            {
                                string value = GetField(rawText, label);
                                assign(value);
                            }
                        }
                    }
                }

                // Imagem do herói
                try
                {
                    var img = _driver.FindElement(By.CssSelector("div.heroBox > img"));
                    heroi.ImagemPersonalidade = img?.GetAttribute("src");
                }
                catch
                {
                    heroi.ImagemPersonalidade = null;
                }

                // Título e textos
                try
                {
                    heroi.TituloTexto = _driver.FindElement(By.CssSelector("div.infoBox.fullWidth.stdCnt > h1")).Text;
                }
                catch
                {
                    heroi.TituloTexto = null;
                }

                try
                {
                    var textos = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt > p"));
                    foreach (var texto in textos)
                        heroi.Textos.Add(texto.Text.Trim());
                }
                catch
                {
                    heroi.Textos = null;
                }

                // Galeria de imagens
                try
                {
                    var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                    foreach (var imagem in imagens)
                    {
                        var img = imagem.FindElement(By.CssSelector("img"));
                        var legenda = imagem.FindElement(By.CssSelector("dd.gallery-caption")).Text.Trim();

                        heroi.Imagens.Add(new Image
                        {
                            Url = img.GetAttribute("src"),
                            Descricao = legenda
                        });
                    }
                }
                catch
                {
                    heroi.Imagens = null;
                }

                // Iframes (YouTube e Instagram)
                try
                {
                    var iframes = _driver.FindElements(By.CssSelector("div.infoBox.fullWidth.stdCnt iframe"));
                    foreach (var iframe in iframes)
                    {
                        var src = iframe.GetAttribute("src");
                        if (src.Contains("instagram", StringComparison.OrdinalIgnoreCase))
                            heroi.InstagramIframes.Add(src);
                        else if (src.Contains("youtube", StringComparison.OrdinalIgnoreCase) || src.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                            heroi.YoutubeIframes.Add(src);
                    }
                }
                catch
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

        private string GetField(string text, string label)
        {
            int index = text.IndexOf(label, StringComparison.OrdinalIgnoreCase);
            if (index < 0) return null;

            int start = index + label.Length;
            int end = text.Length;

            var knownLabels = new[] {
                "Nome completo", "Data de nascimento", "Data de falecimento",
                "Local de nascimento", "Área de atuação"
            };

            foreach (var nextLabel in knownLabels)
            {
                if (nextLabel.Equals(label, StringComparison.OrdinalIgnoreCase))
                    continue;

                int labelIndex = text.IndexOf(nextLabel, start, StringComparison.OrdinalIgnoreCase);
                if (labelIndex >= 0 && labelIndex < end)
                    end = labelIndex;
            }

            return text.Substring(start, end - start).Trim(':', '-', ' ', '\n', '\r');
        }
    }
}