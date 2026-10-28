using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class Triplo
    {
        public string Operador { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }

        public Triplo(string operador, string arg1, string arg2)
        {
            Operador = operador;
            Arg1 = arg1;
            Arg2 = arg2;
        }

        public string ToStringConIndice(int index)
        {
            return $"({index})\t{Operador}\t{Arg1}\t{Arg2}";
        }

        public override string ToString()
        {
            return $"{Operador}\t{Arg1}\t{Arg2}";
        }
    }
}