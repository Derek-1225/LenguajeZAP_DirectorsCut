using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class ZapParsingResult
    {
        public bool Success { get; set; }
        public List<string> TraceSteps { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
    }
}
