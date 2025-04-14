using ScrappingMockIdolos.Scrapper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping dos heróis...");

        var scraper = new IdolosScrapper();
        var idolos = scraper.ObterIdolos();
        scraper.SalvarIdolosComoJson(idolos, "Json/futebol.json");

        //foreach (var h in herois)
        //{
        //    Console.WriteLine($"\n==== {h.Nome} ====");
        //    Console.WriteLine($"Nome completo: {h.NomeCompleto}");
        //    Console.WriteLine($"Nascimento: {h.DataNascimento}");
        //    Console.WriteLine($"Falecimento: {h.DataFalecimento}");
        //    Console.WriteLine($"Local: {h.LocalNascimento}");
        //    Console.WriteLine($"Área: {h.AreaAtuacao}");
        //    Console.WriteLine($"Textos: {string.Join("\n", h.Textos.Take(2))}...");
        //    Console.WriteLine($"Imagens: {h.Imagens.Count}, Instagram: {h.InstagramIframes.Count}, YouTube: {h.YoutubeIframes.Count}");
        //}

        Console.WriteLine($"\nTotal de heróis encontrados: {idolos.Count}");
    }
}
