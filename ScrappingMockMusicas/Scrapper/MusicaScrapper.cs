using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ScrappingMockMusicas.Scrapper;

public class MusicaScrapper
{
    private readonly IWebDriver _driver;

    public MusicaScrapper()
    {
        var options = new ChromeOptions();
        options.AddArgument("--headless");
        _driver = new ChromeDriver(options);
    }

    public List<Musica> ObterMusica()
    {
        var musicas = new List<Musica>();
        _driver.Navigate().GoToUrl("https://museuflamengo.com/cultura-rubro-negra/musica/");

        var linkElements = _driver.FindElements(By.CssSelector("div.listNamesAlphabet.fullWidth ul li a"));
        var hrefs = linkElements.Select(link => link.GetAttribute("href")).Where(href => !string.IsNullOrEmpty(href)).ToList();

        foreach (var href in hrefs)
        {
            var musica = ObterDadosMusica(href);
            if (musica != null)
                musicas.Add(musica);
        }

        _driver.Quit();
        return musicas;
    }

    public void SalvarMusicasComoJson(List<Musica> musicas, string caminho)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(musicas, options);
        File.WriteAllText(caminho, json);
    }

    private Musica ObterDadosMusica(string url)
    {
        _driver.Navigate().GoToUrl(url);
        var musica = new Musica();

        try
        {
            musica.Titulo = _driver.FindElement(By.CssSelector("body > div.container.musica > div > div.heroBox.fullWidth.musica > h1")).Text;

            try
            {
                var fichaTecnica = _driver.FindElements(By.CssSelector("body > div.container.musica > div > div.heroBox.fullWidth.musica > div.heroContent.ficha_tecnica > p"));

                foreach (var info in fichaTecnica)
                {
                    musica.FichaTecnica.Add(info.Text.Trim());
                }
            }
            catch
            {
                musica.FichaTecnica = [];
            }

            try
            {
                var paragrafos = _driver.FindElements(By.CssSelector("body > div.container.musica > div > div.fullWidth.sobre.stdCnt > p"));

                var saibaMaisSobre = false;
                var teste = 0;

                foreach (var paragrafo in paragrafos)
                {
                    var texto = paragrafo.Text.Trim();
                    if (texto.Contains("Veja a letra") || texto.Contains("letra da música"))
                    {
                        try
                        {
                            musica.VejaLetra = new LinkExterno()
                            {
                                Texto = texto,
                                Link = paragrafo.FindElement(By.CssSelector("a")).GetAttribute("href")
                            };
                        }
                        catch
                        {
                            musica.VejaLetra = null;
                        }
                    }
                    else if (texto.Contains("Discografia"))
                    {
                        musica.Discografia = new LinkExterno()
                        {
                            Texto = texto,
                            Link = paragrafo.FindElement(By.CssSelector("a")).GetAttribute("href")
                        };
                    }
                    else if (texto.Contains("Escute a música") || texto.Contains("Ouça a música") || texto.Contains("ouvir a música"))
                    {
                        try
                        {
                            musica.OuvirMusica = new LinkExterno()
                            {
                                Link = paragrafo.FindElement(By.CssSelector("a")).GetAttribute("href"),
                                Texto = texto
                            };
                        }
                        catch
                        {
                            musica.OuvirMusica = null;
                        }
                    }
                    else if (texto.Contains("Saiba mais sobre"))
                    {
                        try
                        {
                            musica.SaibaMaisSobre = new LinkExterno()
                            {
                                Link = paragrafo.FindElement(By.CssSelector("a")).GetAttribute("href"),
                                Texto = texto
                            };
                            saibaMaisSobre = true;
                        }
                        catch
                        {

                        }
                    }
                    else if (texto == "")
                    {

                    }
                    else
                    {
                        musica.Texto.Add(texto);
                    }

                    if(texto.Contains("Saiba mais sobre o") && saibaMaisSobre)
                    {
                        if (teste != 0)
                        {
                            try
                            {
                                musica.SaibaMais.Add(new LinkExterno()
                                {
                                    Link = paragrafo.FindElement(By.CssSelector("a")).GetAttribute("href"),
                                    Texto = texto
                                });
                            }
                            catch
                            {

                            }
                        }
                        else
                        {
                            musica.Texto.Add(texto);
                        }
                        teste++;
                    }

                    try
                    {
                        musica.Videos.Add(paragrafo.FindElement(By.CssSelector("iframe")).GetAttribute("src"));
                    }
                    catch
                    {
                        
                    }
                }
            }
            catch 
            {
                musica.Texto = [];
            }
            try
            {
                var imagem = _driver.FindElement(By.CssSelector("body > div.container.musica > div > div.fullWidth.saiba_mais > p > img"));
                musica.Imagens.Add(imagem.GetAttribute("src"));
            }
            catch
            {

            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erro ao processar {url}: {ex.Message}");
            return null;
        }

        return musica;
    }
}
