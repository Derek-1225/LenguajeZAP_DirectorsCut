using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class InstruccionP
    {
        public string Operacion { get; set; } // LIT, LOD, STO, ADD, SUB, GT, FJMP, JMP, LABEL, etc.
        public string Argumento1 { get; set; }
        public string Argumento2 { get; set; }

        public InstruccionP(string operacion, string argumento1 = "", string argumento2 = "")
        {
            Operacion = operacion;
            Argumento1 = argumento1;
            Argumento2 = argumento2;
        }

        // Formato visual para las etiquetas (ej: "L1:")
        public override string ToString()
        {
            if (Operacion == "LABEL")
            {
                return $"{Argumento1}:";
            }
            return $"{Operacion} {Argumento1} {Argumento2}".Trim();
        }

        private static int contadorEtiquetas = 0;

        public static string ObtenerNuevaEtiqueta()
        {
            contadorEtiquetas++;
            return $"L{contadorEtiquetas}";
        }

        // =====================================================================
        // GENERADOR DE CÓDIGO P PARA EXPRESIONES Y CONDICIONES
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoExpresion(List<string> expTokens)
        {
            List<InstruccionP> codigoLocal = new List<InstruccionP>();
            List<string> tokensPostfijos = ConvertirAInfijoPostfijo(expTokens);

            foreach (var token in tokensPostfijos)
            {
                if (EsNumero(token) || (token.StartsWith("\"") && token.EndsWith("\"")))
                {
                    codigoLocal.Add(new InstruccionP("LIT", token));
                }
                else if (token.StartsWith("$") || EsIdentificadorValido(token))
                {
                    codigoLocal.Add(new InstruccionP("LOD", token));
                }
                else
                {
                    switch (token)
                    {
                        case "+": codigoLocal.Add(new InstruccionP("ADD")); break;
                        case "-": codigoLocal.Add(new InstruccionP("SUB")); break;
                        case "*": codigoLocal.Add(new InstruccionP("MUL")); break;
                        case "/": codigoLocal.Add(new InstruccionP("DIV")); break;
                        case "==": codigoLocal.Add(new InstruccionP("EQ")); break;
                        case "!=": codigoLocal.Add(new InstruccionP("NE")); break;
                        case "<": codigoLocal.Add(new InstruccionP("LT")); break;
                        case ">": codigoLocal.Add(new InstruccionP("GT")); break;
                        case "<=": codigoLocal.Add(new InstruccionP("LE")); break;
                        case ">=": codigoLocal.Add(new InstruccionP("GE")); break;
                        case "&&": codigoLocal.Add(new InstruccionP("AND")); break;
                        case "||": codigoLocal.Add(new InstruccionP("OR")); break;
                    }
                }
            }
            return codigoLocal;
        }

        // =====================================================================
        // GENERADOR DE CÓDIGO P PARA ASIGNACIONES
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoDinamico(string nombreVariable, List<string> expTokens)
        {
            List<InstruccionP> codigoLocal = GenerarCodigoExpresion(expTokens);
            codigoLocal.Add(new InstruccionP("STO", nombreVariable));
            return codigoLocal;
        }

        // =====================================================================
        // GENERADOR DE CÓDIGO P PARA CICLOS DO-WHILE (NUEVO)
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoDoWhile(List<string> condTokens, List<InstruccionP> cuerpoInstrucciones)
        {
            List<InstruccionP> codigoDoWhile = new List<InstruccionP>();

            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Etiqueta donde inicia el bloque do
            codigoDoWhile.Add(new InstruccionP("LABEL", etiquetaInicio));

            // 2. Instrucciones que componen el cuerpo del ciclo (asignaciones internas)
            codigoDoWhile.AddRange(cuerpoInstrucciones);

            // 3. Evaluar la condición del while(...) (ej: carga $C, carga 10, aplica GT)
            codigoDoWhile.AddRange(GenerarCodigoExpresion(condTokens));

            // 4. Saltos de control: Si la condición es falsa (0), salta al fin. Si no, repite.
            codigoDoWhile.Add(new InstruccionP("FJMP", etiquetaFin));
            codigoDoWhile.Add(new InstruccionP("JMP", etiquetaInicio));

            // 5. Etiqueta de salida del ciclo
            codigoDoWhile.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoDoWhile;
        }

        // =====================================================================
        // PRECEDENCIA Y ALGORITMO SHUNTING-YARD
        // =====================================================================
        private static int ObtenerPrecedencia(string operador)
        {
            if (operador == "||") return 1;
            if (operador == "&&") return 2;
            if (operador == "==" || operador == "!=") return 3;
            if (operador == "<" || operador == ">" || operador == "<=" || operador == ">=") return 4;
            if (operador == "+" || operador == "-") return 5;
            if (operador == "*" || operador == "/") return 6;
            return 0;
        }

        private static List<string> ConvertirAInfijoPostfijo(List<string> tokensInfijos)
        {
            List<string> salida = new List<string>();
            Stack<string> operadores = new Stack<string>();

            foreach (var t in tokensInfijos)
            {
                if (EsNumero(t) || t.StartsWith("$") || (t.StartsWith("\"") && t.EndsWith("\"")))
                {
                    salida.Add(t);
                }
                else if (ObtenerPrecedencia(t) > 0)
                {
                    while (operadores.Count > 0 &&
                           operadores.Peek() != "(" &&
                           ObtenerPrecedencia(operadores.Peek()) >= ObtenerPrecedencia(t))
                    {
                        salida.Add(operadores.Pop());
                    }
                    operadores.Push(t);
                }
                else if (t == "(")
                {
                    operadores.Push(t);
                }
                else if (t == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                    {
                        salida.Add(operadores.Pop());
                    }
                    if (operadores.Count > 0) operadores.Pop();
                }
            }

            while (operadores.Count > 0)
            {
                salida.Add(operadores.Pop());
            }

            return salida;
        }

        private static bool EsNumero(string texto) => double.TryParse(texto, out _);
        private static bool EsIdentificadorValido(string texto) => !string.IsNullOrEmpty(texto) && char.IsLetter(texto[0]);
    }
}