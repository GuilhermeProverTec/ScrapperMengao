using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockDiversos.Models
{
    public class CatalogoDiversos
    {
        public InformacoesCatalogo Info { get; set; } = new();
        public List<Diversos> Diversoss { get; set; } = [];

        public class Diversos
        {
            public string Imagem { get; set; }
            public string Url { get; set; }
            public string Nome { get; set; }
            public Detalhes DetalhesDiversos { get; set; } = new();

            public class Detalhes
            {
                public string Titulo { get; set; }
                public string Modalidade { get; set; }
                public string EspecificacoesTecnicas { get; set; }
                public List<string> CarrosselImagens { get; set; } = [];
                public string Ano { get; set; }
                public List<Cards> MaisItens { get; set; } = [];

                public class Cards
                {
                    public string Nome { get; set; }
                    public string Imagem { get; set; }
                    public string Url { get; set; }
                }
            }
        }

        public class InformacoesCatalogo
        {
            public string? Icone { get; set; }
            public string? Titulo { get; set; }
            public string? Descricao { get; set; }

        }
    }
}
