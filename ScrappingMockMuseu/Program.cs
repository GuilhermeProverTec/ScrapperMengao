using ScrappingMockHeroi.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine($"Iniciando scraping dos heróis...");

        var scraper = new HeroiScrapper();
        var herois = scraper.ObterHerois();
        scraper.SalvarHeroisComoJson(herois, "Json/HeroisDaNacao.json");

        Console.WriteLine($"\nTotal de heróis encontrados: {herois.Count}");
    }
}
