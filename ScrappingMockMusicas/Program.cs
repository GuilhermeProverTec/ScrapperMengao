using ScrappingMockMusicas.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping das Musicas...");

        var scraper = new MusicaScrapper();
        var esportes = scraper.ObterMusica();
        scraper.SalvarMusicasComoJson(esportes, "Json/Musicas.json");

        Console.WriteLine($"\nTotal de musicas encontradas: {esportes.Count}");
    }
}