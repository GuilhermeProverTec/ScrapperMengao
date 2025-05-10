

using ScrappingMockDiversos.Scrapper;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping de Iconografia...");

        var scraper = new DiversosScrapper();
        var vestimentas = scraper.ObterDiversos();
        scraper.SalvarDiversosComoJson(vestimentas, "Json/Diversos.json");
    }
}