using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockVestimentas.Models
{
    public class CatalogoVestimenta
    {
        public InformacoesCatalogo Info { get; set; }
        public List<Vestimenta> Vestimentas { get; set; }

        public class Vestimenta
        {
            public string Imagem { get; set; }
            public string Url { get; set; }
            public string Nome { get; set; }

            public Detalhes DetalhesVestimenta { get; set; }

            public class Detalhes
            {
                public string Titulo { get; set; }
                public string Modalidade { get; set; }
                public string EspecificacoesTecnicas { get; set; }
                public List<string> CarrosselImagens { get; set; }
                public string Ano { get; set; }
                public List<Cards> MaisItens { get; set; }

                public class Cards
                {
                    public string Nome { get; set; }
                    public string Imagem { get; set; }
                    public string Url { get; set; }
                }
            }
        }

        public class InformacoesCatalogo{ 
            public string Icone { get; set; }
            public string Titulo { get; set; }
            public string Descricao { get; set; }

        }
    }
}
