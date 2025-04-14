using ScrappingMockIdolos.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new IdolosScrapper();
        var idolos = scraper.ObterIdolos();
        scraper.SalvarIdolosComoJson(idolos, "Json/Remo.json");

        Console.WriteLine($"\nTotal de heróis encontrados: {idolos.Count}");
    }
}
