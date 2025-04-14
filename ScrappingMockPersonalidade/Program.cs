using ScrappingMockPersonalidades.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new PersonalidadesScrapper();
        var personalidades = scraper.ObterPersonalidades();
        scraper.SalvarPersonalidadesComoJson(personalidades, "Json/personalidades.json");

        Console.WriteLine($"\nTotal de heróis encontrados: {personalidades.Count}");
    }
}
