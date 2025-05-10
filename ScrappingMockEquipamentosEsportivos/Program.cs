using ScrappingMockEquipamentosEsportivos.Scrapper;

public class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Iniciando scraping de Equipamentos...");

        var scraper = new EquipamentosEsportivosScrapper();
        var vestimentas = scraper.ObterEquipamentos();
        scraper.SalvarEquipamentoComoJson(vestimentas, "Json/EquipamentosEsportivos.json");
    }
}