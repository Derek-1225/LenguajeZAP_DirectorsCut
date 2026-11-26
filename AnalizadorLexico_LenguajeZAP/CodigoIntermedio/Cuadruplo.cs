using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class Cuadruplo
    {
        public string Operador { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Resultado { get; set; }

        public Cuadruplo(string operador, string arg1, string arg2, string resultado)
        {
            Operador = operador;
            Arg1 = arg1;
            Arg2 = arg2;
            Resultado = resultado;
        }

        public override string ToString()
        {
            return $"{Operador}\t{Arg1}\t{Arg2}\t{Resultado}";
        }
    }
}