using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockMaisEsportes.Models
{
    public class Esporte
    {
        public string Nome { get; set; }
        public string Ano { get; set; }
        public HashSet<Imagem> Imagens { get; set; } = new();
        public List<string> Textos { get; set; } = new();
        public List<Imagem> SaibaMais { get; set; } = new();
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
