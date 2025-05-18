namespace ScrappingMockMusicas;

public class Musica
{
    public string Titulo { get; set; }
    public List<string> FichaTecnica { get; set; } = [];
    public List<string> Texto { get; set; } = [];
    public LinkExterno VejaLetra { get; set; } = new();
    public LinkExterno SaibaMaisSobre { get; set; } = new();
    public List<LinkExterno> SaibaMais { get; set; } = [];
    public LinkExterno Discografia { get; set; } = new();
    public LinkExterno OuvirMusica { get; set; } = new();
    public List<string> Imagens { get; set; } = [];
    public List<string> Videos { get; set; } = [];
}

public class LinkExterno
{
    public string Texto { set; get; }
    public string Link { get; set; }
}