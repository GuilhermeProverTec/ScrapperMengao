

using ScrappingMockIconografia.Scrapper;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping de Iconografia...");

        var scraper = new IconografiaScrapper();
        var vestimentas = scraper.ObterIconografias();
        scraper.SalvarIconografiaComoJson(vestimentas, "Json/Inconografia.json");
    }
}