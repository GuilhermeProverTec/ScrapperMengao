namespace ScrappingMockArquibancada.Models
{
    public class Torcida
    {
        public string NomeTorcida { get; set; }
        public Imagem LogoTorcida { get; set; } = new Imagem();
        public DateOnly? DataFundacao { get; set; }
        public List<LinkExterno>? FundadoresTorcedoresIlustres { get; set; } = new();
        public string TorcedoresIlustres { get; set; }
        public string? Lema {  get; set; }
        public LinkExterno? LivroBiografia { get; set; } = new();
        public List<string>? Textos { get; set; } = new();
        public List<string>? YoutubeIFrames { get; set; } = new();
        public HashSet<Imagem>? CarroselImagemTorcida { get; set; } = new();
    }
    public class LinkExterno
    {
        public string? Url { get; set; }
        public string? Texto { get; set; }
    }
}
