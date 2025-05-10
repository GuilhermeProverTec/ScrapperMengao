using ScrappingMockVestimentas.Scrapper;

namespace ScrappingMockVestimentas
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando scraping de Vestimenta...");

            var scraper = new VestimentaScrapper();
            var vestimentas = scraper.ObterVestimentas();
            scraper.SalvarVestimentaComoJson(vestimentas, "Json/Vestimentas.json");
        }
    }
}
