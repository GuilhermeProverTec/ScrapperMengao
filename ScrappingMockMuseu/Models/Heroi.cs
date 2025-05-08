namespace ScrappingMockHeroi.Models
{
    public class Heroi
    {
        public string Apelido { get; set; }
        public List<DadosPessoais> DadosPessoais { get; set; } = new ();
        public string AreaAtuacao { get; set; }
        public string ImagemPersonalidade { get; set; } 
        public string TituloTexto { get; set; }
        public List<string> Textos { get; set; } = new();
        public List<string> InstagramIframes { get; set; } = new();
        public List<string> YoutubeIframes { get; set; } = new();
        public HashSet<Imagem> Imagens { get; set; } = new();
        public List<MaisHerois> MaisHerois { get; set; } = new();
    }

    public class DadosPessoais
    {
        public string NomeCompleto { get; set; }
        public string Apelido { get; set; }
        public string DataNascimento { get; set; }
        public string LocalNascimento { get; set; }
        public string DataFalecimento { get; set; }
    }

    public class MaisHerois
    {
        public string? Nome { get; set; }
        public string? AreaAtuacao { get; set; }
        public string? Url { get; set; }
        public string? Imagem { get; set; }
    }

    public class Imagem
    {
        public string Url { get; set; }
        public string Descricao { get; set; }
        public override bool Equals(object obj)
        {
            return obj is Imagem other &&
                   string.Equals(Url, other.Url, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(
                Url?.ToLowerInvariant()
            );
        }
    }       
}
