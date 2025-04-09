namespace ScrappingMockMuseu.Models
{
    public class Heroi
    {
        public string Apelido { get; set; }
        public string NomeCompleto { get; set; }
        public string DataNascimento { get; set; }
        public string DataFalecimento { get; set; }
        public string LocalNascimento { get; set; }
        public string AreaAtuacao { get; set; }

        public List<string> Textos { get; set; } = new();
        public List<string> InstagramIframes { get; set; } = new();
        public List<string> YoutubeIframes { get; set; } = new();

        public List<(string Url, string Credito)> Imagens { get; set; } = new();
    }
}
