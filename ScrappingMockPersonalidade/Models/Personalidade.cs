using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockPersonalidades.Models
{
    public class Personalidade
    {
        public string Apelido { get; set; }
        public List<DadosPessoais> DadosPessoais { get; set; } = new();
        public string AreaAtuacao { get; set; }
        public List<string> ImagemPersonalidade { get; set; } = new();
        public string TituloTexto { get; set; }
        public List<string> Textos { get; set; } = new();
        public List<Imagem> Imagens { get; set; } = new();
        public List<Imagem> ImagemTexto { get; set; } = new(); //Só no Hebert Vianna, onde tem uma reportagem
        public List<Imagem> SaibaMais { get; set; } = new();
        public List<string> YoutubeIframes { get; set; } = new();
    }

    public class DadosPessoais
    {
        public string NomeCompleto { get; set; }
        public string Apelido { get; set; }
        public string DataNascimento { get; set; }
        public string LocalNascimento { get; set; }
        public string DataFalecimento { get; set; }
    }
    public class Imagem
    {
        public string Url { get; set; }
        public string Legenda { get; set; }
    }
}
