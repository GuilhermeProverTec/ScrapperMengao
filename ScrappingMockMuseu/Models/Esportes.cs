using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockMuseu.Models
{
    public class Esportes
    {
        public string Nome { get; set; }
        public string Ano { get; set; }
        public List<string> ImagensAtletas { get; set; } = new();
        public List<string> NomesAtletas { get; set; } = new();
        public List<string> Textos { get; set; } = new();
    }
}
