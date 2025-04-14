using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScrappingMockPresidentes.Models
{
    public class Presidente
    {
        public string Nome {  get; set; }
        public string DataNascimento { get; set; }
        public string LocalNascimento { get; set; }
        public string DataFalecimento { get; set; }
        public string Profissao {  get; set; }
        public string Mandato { get; set; }
        public string Observacao { get; set; }
        public string Imagem { get; set; }
    }
}
