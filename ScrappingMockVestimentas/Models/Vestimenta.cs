﻿namespace ScrappingMockVestimentas.Models
{
    public class CatalogoVestimenta
    {
        public InformacoesCatalogo Info { get; set; } = new();
        public List<Vestimenta> Vestimentas { get; set; } = []; 

        public class Vestimenta
        {
            public string Imagem { get; set; }
            public string Url { get; set; }
            public string Nome { get; set; }
            public Detalhes DetalhesVestimenta { get; set; } = new();

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

        public class InformacoesCatalogo{ 
            public string? Icone { get; set; }
            public string? Titulo { get; set; }
            public string? Descricao { get; set; }

        }
    }
}
