using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class TraductorNotaciones
    {
        // 1. CONVERTIR DE INFIJA A POSTFIJA
        public static List<string> InfijaAPostfija(List<string> tokensInfijos)
        {
            List<string> salida = new List<string>();
            Stack<string> operadores = new Stack<string>();

            foreach (var t in tokensInfijos)
            {
                if (EsNumero(t) || t.StartsWith("$") || (t.StartsWith("\"") && t.EndsWith("\"")) || EsIdentificadorValido(t))
                {
                    salida.Add(t);
                }
                // Si es un operador con precedencia válida
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
                // Si es paréntesis de apertura
                else if (t == "(")
                {
                    operadores.Push(t);
                }
                // Si es paréntesis de cierre
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

        // 2. CONVERTIR DE INFIJA A PREFIJA (Notación Polaca)
        public static List<string> InfijaAPrefija(List<string> tokensInfijos)
        {
            var invertida = new List<string>(tokensInfijos);
            invertida.Reverse();

            for (int i = 0; i < invertida.Count; i++)
            {
                if (invertida[i] == "(") invertida[i] = ")";
                else if (invertida[i] == ")") invertida[i] = "(";
            }

            var postfijaInvertida = InfijaAPostfija(invertida);

            postfijaInvertida.Reverse();
            return postfijaInvertida;
        }

        // 3. JERARQUÍA Y PRECEDENCIA DE OPERADORES
        private static int ObtenerPrecedencia(string operador)
        {
            switch (operador)
            {
                case "=": return 1; 
                case "||": return 2;
                case "&&": return 3;
                case "==":
                case "!=": return 4;
                case "<":
                case ">":
                case "<=":
                case ">=": return 5;
                case "+":
                case "-": return 6;
                case "*":
                case "/": return 7;
                default: return 0;
            }
        }

        //Metodos Auxiliares
        private static bool EsNumero(string texto)
        {
            return double.TryParse(texto, out _);
        }

        private static bool EsIdentificadorValido(string texto)
        {
            return !string.IsNullOrEmpty(texto) && char.IsLetter(texto[0]);
        }
    }
}