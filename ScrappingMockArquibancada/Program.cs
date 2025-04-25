using ScrappingMockArquibancada.Scrapper;

namespace ScrappingMockArquibancada
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando scraping da Arquibancada...");

            var scraper = new ArquibancadaScrapper();
            var arquibancada = scraper.ObterArquibancada();
            scraper.SalvarArquibancadaComoJson(arquibancada, "Json/Arquibancada.json");
        }
    }
}
