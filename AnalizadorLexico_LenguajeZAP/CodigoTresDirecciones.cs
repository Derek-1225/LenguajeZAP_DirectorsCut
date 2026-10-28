using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class CodigoTresDirecciones
    {
        public string InstruccionTexto { get; set; }

        public CodigoTresDirecciones(string instruccionTexto)
        {
            InstruccionTexto = instruccionTexto;
        }

        public override string ToString()
        {
            return InstruccionTexto;
        }
    }
}