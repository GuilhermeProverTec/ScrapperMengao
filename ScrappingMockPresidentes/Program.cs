using ScrappingMockPresidentes.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new PresidentesScrapper();
        var presidentes = scraper.ObterPresidentes();
        scraper.SalvarPresidentesComoJson(presidentes, "Json/presidentes.json");

        Console.WriteLine($"\nTotal de heróis encontrados: {presidentes.Count}");
    }
}
