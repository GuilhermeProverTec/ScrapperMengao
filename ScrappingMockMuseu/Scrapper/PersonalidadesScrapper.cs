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
using System.Reflection.Emit;

namespace ScrappingMockMuseu.Scrapper
{
    public class PersonalidadesScrapper
    {
        private readonly IWebDriver _driver;

        public PersonalidadesScrapper()
        {
            var options = new ChromeOptions();
            options.AddArgument("--headless");
            _driver = new ChromeDriver(options);
        }

        public List<Personalidade> ObterHerois()
        {
            var herois = new List<Personalidade>();
            _driver.Navigate().GoToUrl("https://www.museuflamengo.com.br/cultura-rubro-negra/personalidades/");

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

        public void SalvarHeroisComoJson(List<Personalidade> herois, string caminho)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(herois, options);
            File.WriteAllText(caminho, json);
        }

        private Personalidade ObterDadosHeroi(string url)
        {
            // Increase page load timeout (e.g., 3 minutes)
            _driver.Manage().Timeouts().PageLoad = TimeSpan.FromMinutes(3);

            // Increase asynchronous JavaScript timeout (e.g., 2 minutes)
            _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromMinutes(2);

            _driver.Navigate().GoToUrl(url);
            var heroi = new Personalidade();

            try
            {
                heroi.Apelido = _driver.FindElement(By.CssSelector("body > div.container.personalidades > div > div.heroBox.fullWidth.personalidades > h1")).Text;
                var paragrafos = _driver.FindElements(By.CssSelector("div.heroContent.ficha_tecnica > p"));
                string lastLabel = null;

                if (paragrafos.Count > 3)
                {
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

                        // Match and assign based on different label types
                        if (label.Contains("nome completo"))
                            heroi.NomeCompleto = value;
                        else if (label.Contains("data de nascimento"))
                            heroi.DataNascimento = value;
                        else if (label.Contains("local de nascimento"))
                            heroi.LocalNascimento = value;
                        else if (label.Contains("data de falecimento"))
                            heroi.DataFalecimento = value;
                        else if (label.Contains("área de atuação"))
                            heroi.AreaAtuacao = value;
                    }
                }
                else
                {
                    // Handle case where all data is inside a single <p> tag
                    string fullText = paragrafos.FirstOrDefault()?.Text;
                    var label = fullText.ToLower();

                    if (!string.IsNullOrEmpty(fullText))
                    {
                        // Split the full text based on known labels
                        if (label.Contains("nome completo"))
                            heroi.NomeCompleto = ExtractValue(fullText, "nome completo");
                        if (label.Contains("nome"))
                            heroi.NomeCompleto = ExtractValue(fullText, "nome");
                        if (label.Contains("data de nascimento"))
                            heroi.DataNascimento = ExtractValue(fullText, "data de nascimento");
                        if (label.Contains("local de nascimento"))
                            heroi.LocalNascimento = ExtractValue(fullText, "local de nascimento");
                        if (label.Contains("data de falecimento"))
                            heroi.DataFalecimento = ExtractValue(fullText, "data de falecimento");
                        if (label.Contains("área de atuação"))
                            heroi.AreaAtuacao = ExtractValue(fullText, "área de atuação");
                    }
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
                return null;
            }

            WebDriverWait wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10)); // Ajuste o tempo conforme necessário
            try
            {
                wait.Until(driver => driver.FindElements(By.CssSelector(".slick-track img")).Count > 0);

                // Get all <img> elements inside the slick-track
                var imgElements = _driver.FindElements(By.CssSelector(".slick-track img"));

                // Use HashSet to avoid duplicates from cloned slides
                var imageUrls = new HashSet<string>();

                foreach (var img in imgElements)
                {
                    var src = img.GetAttribute("src");
                    if (!string.IsNullOrEmpty(src))
                    {
                        heroi.ImagemPersonalidade.Add(src);
                    }
                }
            }
            catch (Exception e)
            {
                heroi.ImagemPersonalidade = null;
            }

            try
            {
                heroi.TituloTexto = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt > h1")).Text;
            }
            catch(Exception e) 
            {
                heroi.TituloTexto = null;
            }

            try
            {
                var textos = _driver.FindElements(By.CssSelector("div.fullWidth.sobre.stdCnt > p"));
                foreach (var texto in textos)
                    heroi.Textos.Add(texto.Text.Trim());
            }
            catch (Exception e)
            {
                heroi.Textos = null;
            }

            try
            {
                var saibaMais = _driver.FindElements(By.CssSelector("div.fullWidth.saiba_mais p a"));
                foreach (var saibaMaisElement in saibaMais)
                {
                    heroi.SaibaMais.Add(new Imagem
                    {
                        Url = saibaMaisElement.GetAttribute("href"),
                        Legenda = saibaMaisElement.Text.Trim()
                    });
                }
            }
            catch(Exception e) 
            { 
                heroi.SaibaMais = null;
            }

            try
            {
                var imagens = _driver.FindElements(By.CssSelector("dl.gallery-item.slick-slide"));
                foreach (var imagem in imagens)
                {
                    var imageElement = imagem.FindElement(By.CssSelector("img"));
                    heroi.Imagens.Add(new Imagem
                    {
                        Url = imageElement.GetAttribute("src"),
                        Legenda = imagem.FindElement(By.CssSelector("dd.gallery-caption")).Text.Trim()
                    });
                }
            }
            catch (Exception e)
            {
                heroi.Imagens = null;
            }

            try
            {
                var imagem = _driver.FindElement(By.CssSelector("div.fullWidth.sobre.stdCnt")); ;
                var imageElement = imagem.FindElement(By.CssSelector("img.wp-image-3375"));
                heroi.ImagemTexto.Add(new Imagem
                {
                    Url = imageElement.GetAttribute("src"),
                    Legenda = imagem.FindElement(By.CssSelector(".wp-caption-text")).Text.Trim()
                });
                
            }
            catch (Exception e)
            {
                heroi.ImagemTexto = null;
            }

            try
            {
                var saibaMaisSection = _driver.FindElement(By.CssSelector("div.fullWidth.saiba_mais"));
                var iframeElements = saibaMaisSection.FindElements(By.TagName("iframe"));

                foreach (var iframe in iframeElements)
                {         
                    heroi.YoutubeIframes.Add(iframe.GetAttribute("src"));
                }
            }
            catch (NoSuchElementException)
            {
                heroi.YoutubeIframes = null;
            }

            return heroi;
        }
        private string ExtractValue(string fullText, string label)
        {
            var temp = fullText.ToLower();
            var index = temp.IndexOf(label);
            
            if (index != -1)
            {
                return fullText.Substring(index + label.Length).Split("\r").FirstOrDefault().Split(":").LastOrDefault().Trim();
            }
            return string.Empty;
        }
    }
}
