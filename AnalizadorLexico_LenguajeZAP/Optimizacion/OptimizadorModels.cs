using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Policy;

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
            if (Operador == "FUNC") return $"func {Arg1}:";
            if (Operador == "ENDFUNC") return $"endfunc {Arg1}";
            if (Operador == "PARAM") return $"param {Arg1}";
            if (Operador == "RETURN") return (Arg1 == "-" || string.IsNullOrEmpty(Arg1)) ? "return" : $"return {Arg1}";
            if (Operador == "CALL") return (Resultado == "-" || string.IsNullOrEmpty(Resultado)) ? $"call {Arg1}, {Arg2}" : $"{Resultado} = call {Arg1}, {Arg2}";
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
        public int TemporalesUtilizados { get; set; }     // Temporales en TAC o profundidad máxima de pila en Código P
        public int AccesosMemoria { get; set; }           // Variables ($) o LOD/STO en Código P
        public int CostoCiclosEstimado { get; set; }      // Peso computacional por tipo de op
        public List<string> BitacoraCambios { get; set; } = new List<string>();

        public static ReporteCostos Evaluar(List<InstruccionIntermedia> codigo)
        {
            var reporte = new ReporteCostos();
            HashSet<string> temporales = new HashSet<string>();
            HashSet<string> variablesMemoria = new HashSet<string>();

            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL" || inst.Operador == "FUNC" || inst.Operador == "ENDFUNC") continue;
                reporte.TotalInstrucciones++;

                foreach (var op in new[] { inst.Arg1, inst.Arg2, inst.Resultado })
                {
                    if (string.IsNullOrEmpty(op) || op == "-") continue;
                    if (op.StartsWith("t") && int.TryParse(op.Substring(1), out _))
                        temporales.Add(op);
                    else if (op.StartsWith("$") && !(inst.Operador == "CALL" && op == inst.Arg1))
                    {
                        variablesMemoria.Add(op);
                        reporte.AccesosMemoria++;
                    }
                }

                switch (inst.Operador)
                {
                    case "^": reporte.CostoCiclosEstimado += 8; break;
                    case "/": reporte.CostoCiclosEstimado += 6; break;
                    case "*": reporte.CostoCiclosEstimado += 4; break;
                    case "CALL": reporte.CostoCiclosEstimado += 5; break;
                    case "RETURN": reporte.CostoCiclosEstimado += 3; break;
                    case "PARAM": reporte.CostoCiclosEstimado += 1; break;
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

        public static ReporteCostos Evaluar(List<InstruccionP> codigo)
        {
            var reporte = new ReporteCostos();
            int pilaActual = 0;

            foreach (var inst in codigo)
            {
                string op = (inst.Operacion ?? "").ToUpperInvariant();
                if (op == "LABEL" || op == "FUNC" || op == "ENDFUNC") continue;

                reporte.TotalInstrucciones++;

                switch (op)
                {
                    case "LIT":
                        pilaActual++;
                        reporte.CostoCiclosEstimado += 1;
                        break;
                    case "LOD":
                        pilaActual++;
                        reporte.AccesosMemoria++;
                        reporte.CostoCiclosEstimado += 2;
                        break;
                    case "STO":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.AccesosMemoria++;
                        reporte.CostoCiclosEstimado += 2;
                        break;
                    case "ADD":
                    case "SUB":
                    case "EQ":
                    case "NE":
                    case "LT":
                    case "GT":
                    case "LE":
                    case "GE":
                    case "AND":
                    case "OR":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.CostoCiclosEstimado += 1;
                        break;
                    case "MUL":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.CostoCiclosEstimado += 4;
                        break;
                    case "DIV":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.CostoCiclosEstimado += 6;
                        break;
                    case "POW":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.CostoCiclosEstimado += 8;
                        break;
                    case "FJMP":
                        pilaActual = Math.Max(0, pilaActual - 1);
                        reporte.CostoCiclosEstimado += 2;
                        break;
                    case "JMP":
                        reporte.CostoCiclosEstimado += 2;
                        break;
                    case "CALL":
                        reporte.CostoCiclosEstimado += 5;
                        break;
                    case "RET":
                        reporte.CostoCiclosEstimado += 3;
                        break;
                    default:
                        reporte.CostoCiclosEstimado += 1;
                        break;
                }

                if (pilaActual > reporte.TemporalesUtilizados)
                    reporte.TemporalesUtilizados = pilaActual;
            }

            return reporte;
        }
    }
}