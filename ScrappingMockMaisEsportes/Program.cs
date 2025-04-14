using ScrappingMockMaisEsportes.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new EsportesScrapper();
        var esportes = scraper.ObterEsportes();
        scraper.SalvarEsportesComoJson(esportes, "Json/maisEsportes.json");

        Console.WriteLine($"\nTotal de heróis encontrados: {esportes.Count}");
    }
}
