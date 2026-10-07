using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    //Clase que representa el resultado del análisis sintáctico del lenguaje ZAP
    public class ZapParsingResult
    {
        //Propiedad que ayuda a determinar si el análisis sintáctico fue exitoso o no
        public bool Success { get; set; }
        //Propiedad que contiene los pasos del análisis sintáctico
        public List<string> TraceSteps { get; set; } = new List<string>();
        //Propiedad que contiene los errores encontrados durante el análisis sintáctico
        public List<string> Errors { get; set; } = new List<string>();
    }
}
