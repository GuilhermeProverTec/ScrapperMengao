namespace ScrappingMockArquibancada.Models
{
    public class Arquibancada
    {
        public string Titulo { get; set; }
        public HashSet<Imagem> CarrosselImagens { get; set; } = new();
        public string Descricao { get; set; }
        public List<Torcida> Torcidas { get; set; } = new();
    }

    public class Imagem
    {
        public string Url { get; set; }
        public string Alt { get; set; }
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
