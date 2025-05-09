using ScrappingMockAcervo.Scrapper;

namespace ScrappingMockAcervo
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando scraping de Acervo...");

            var scraper = new AcervoScrapper();
            var arquibancada = scraper.ObterAcervo();
            scraper.SalvarArquibancadaComoJson(arquibancada, "Json/Acervo.json");
        }
    }
}
