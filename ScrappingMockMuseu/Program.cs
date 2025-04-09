using ScrappingMockMuseu.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new HeroiScraper();
        var herois = scraper.ObterHerois();
        scraper.SalvarHeroisComoJson(herois, "herois.json");

        foreach (var h in herois)
        {
            Console.WriteLine($"\n==== {h.Apelido} ====");
            Console.WriteLine($"Nome completo: {h.NomeCompleto}");
            Console.WriteLine($"Nascimento: {h.DataNascimento}");
            Console.WriteLine($"Falecimento: {h.DataFalecimento}");
            Console.WriteLine($"Local: {h.LocalNascimento}");
            Console.WriteLine($"Área: {h.AreaAtuacao}");
            Console.WriteLine($"Textos: {string.Join("\n", h.Textos.Take(2))}...");
            Console.WriteLine($"Imagens: {h.Imagens.Count}, Instagram: {h.InstagramIframes.Count}, YouTube: {h.YoutubeIframes.Count}");
        }

        Console.WriteLine($"\nTotal de heróis encontrados: {herois.Count}");
    }
}
