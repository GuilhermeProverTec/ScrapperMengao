using ScrappingMockBandeirasFaixasFlamulas.Scrapper;


namespace ScrappingMockVestimentas
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Iniciando scraping de Bandeiras Faixas e Flamulas...");

            var scraper = new BandeiraFaixaFlamulaScrapper();
            var vestimentas = scraper.ObterBandeiraFaixaFlamulas();
            scraper.SalvarBandeiraFaixaFlamulaComoJson(vestimentas, "Json/BandeiraFaixaFlamula.json");
        }
    }
}
