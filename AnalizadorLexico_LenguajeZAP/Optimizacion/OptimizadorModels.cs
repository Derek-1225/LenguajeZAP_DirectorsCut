using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class InstruccionIntermedia
    {
        public string Operador { get; set; }
        public string Arg1 { get; set; }
        public string Arg2 { get; set; }
        public string Resultado { get; set; }

        public InstruccionIntermedia(string op, string a1, string a2, string res)
        {
            Operador = op ?? "-";
            Arg1 = a1 ?? "-";
            Arg2 = a2 ?? "-";
            Resultado = res ?? "-";
        }

        public InstruccionIntermedia Clonar() =>
            new InstruccionIntermedia(Operador, Arg1, Arg2, Resultado);

        public override string ToString()
        {
            if (Operador == "LABEL") return $"{Arg1}:";
            if (Operador == "JUMP") return $"goto {Arg1}";
            if (Operador == "ifFalse") return $"ifFalse {Arg1} goto {Resultado}";
            if (Operador == "ifTrue") return $"ifTrue {Arg1} goto {Resultado}";
            if (Operador == "PRINT") return $"print {Arg1}";
            if (Operador == "=") return $"{Resultado} = {Arg1}";
            return $"{Resultado} = {Arg1} {Operador} {Arg2}";
        }
    }

    public class ReporteCostos
    {
        public int TotalInstrucciones { get; set; }
        public int TemporalesUtilizados { get; set; }     // Simula presión de registros/pila
        public int AccesosMemoria { get; set; }           // Variables ($) y asignaciones
        public int CostoCiclosEstimado { get; set; }      // Peso computacional por tipo de op
        public List<string> BitacoraCambios { get; set; } = new List<string>();

        public static ReporteCostos Evaluar(List<InstruccionIntermedia> codigo)
        {
            var reporte = new ReporteCostos();
            HashSet<string> temporales = new HashSet<string>();
            HashSet<string> variablesMemoria = new HashSet<string>();

            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL") continue;
                reporte.TotalInstrucciones++;

                // Detectar temporales (t1, t2...) y variables ($a, $b...)
                foreach (var op in new[] { inst.Arg1, inst.Arg2, inst.Resultado })
                {
                    if (string.IsNullOrEmpty(op) || op == "-") continue;
                    if (op.StartsWith("t") && int.TryParse(op.Substring(1), out _))
                        temporales.Add(op);
                    else if (op.StartsWith("$"))
                    {
                        variablesMemoria.Add(op);
                        reporte.AccesosMemoria++;
                    }
                }

                // Ponderación de costo de ejecución (3.2.1)
                switch (inst.Operador)
                {
                    case "^": reporte.CostoCiclosEstimado += 8; break;
                    case "/": reporte.CostoCiclosEstimado += 6; break;
                    case "*": reporte.CostoCiclosEstimado += 4; break;
                    case "+":
                    case "-":
                    case "==":
                    case "!=":
                    case "<":
                    case ">":
                    case "<=":
                    case ">=": reporte.CostoCiclosEstimado += 1; break;
                    case "JUMP":
                    case "ifFalse":
                    case "ifTrue": reporte.CostoCiclosEstimado += 2; break;
                    default: reporte.CostoCiclosEstimado += 1; break;
                }
            }

            reporte.TemporalesUtilizados = temporales.Count;
            return reporte;
        }
    }
}