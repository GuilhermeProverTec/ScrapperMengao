namespace ScrappingMockAcervo.Models
{
    public class Acervo
    {
        public string Titulo { get; set; }
        public string Texto { get; set; }
        public List<Icone> Icones { get; set; } = [];
    }

    public class Icone
    {
        public string Nome { get; set; }
        public string URL { get; set; }
        public string Imagem { get; set; }
    }
}
