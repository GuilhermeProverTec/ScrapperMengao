namespace ScrappingMockMusicas;

public class Musica
{
    public string Titulo { get; set; }
    public string Autor { get; set; }
    public string Ano { get; set; }
    public string Texto { get; set; }
    public List<LinkExterno> VejaLetra { get; set; }
    public List<LinkExterno> SaibaMais { get; set; }
    public List<LinkExterno> SobreMusica { get; set; }
    public List<string> Imagens { get; set; }
    public List<string> Videos { get; set; }
}

public class LinkExterno
{
    public string Texto { set; get; }
    public string Link { get; set; }
}