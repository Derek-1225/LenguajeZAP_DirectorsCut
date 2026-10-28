using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class ResultadoCompilacion
    {
        public string ResultadoCuadruplo { get; set; }
        public string ResultadoTriplo { get; set; }
    }

    public static class CompiladorExpresiones
    {
        private static int contadorTemporales = 1;

        public static void ReiniciarTemporales()
        {
            contadorTemporales = 1;
        }

        public static ResultadoCompilacion Compilar(
            List<string> tokensInfijos,
            List<Cuadruplo> cuadruplos,
            List<Triplo> triplos,
            List<string> tac)
        {
            List<string> postfija = TraductorNotaciones.InfijaAPostfija(tokensInfijos);

            Stack<string> pilaCuadruplos = new Stack<string>();
            Stack<string> pilaTriplos = new Stack<string>();

            if (postfija.Count == 1)
            {
                return new ResultadoCompilacion
                {
                    ResultadoCuadruplo = postfija[0],
                    ResultadoTriplo = postfija[0]
                };
            }

            foreach (var token in postfija)
            {
                if (EsOperando(token))
                {
                    pilaCuadruplos.Push(token);
                    pilaTriplos.Push(token);
                }
                else // Es un operador (+, -, *, /, etc.)
                {
                    if (pilaCuadruplos.Count < 2 || pilaTriplos.Count < 2) continue;

                    // --- 1. Cuádruplos y TAC (Usan temporales t1, t2...) ---
                    string arg2Cuad = pilaCuadruplos.Pop();
                    string arg1Cuad = pilaCuadruplos.Pop();
                    string tempCuad = $"t{contadorTemporales++}";

                    cuadruplos.Add(new Cuadruplo(token, arg1Cuad, arg2Cuad, tempCuad));
                    tac.Add($"{tempCuad} = {arg1Cuad} {token} {arg2Cuad}");
                    pilaCuadruplos.Push(tempCuad);

                    // --- 2. Triplos (Usan referencias de índice numérico, ej. (0), (1)...) ---
                    string arg2Trip = pilaTriplos.Pop();
                    string arg1Trip = pilaTriplos.Pop();

                    int indiceActual = triplos.Count;
                    triplos.Add(new Triplo(token, arg1Trip, arg2Trip));
                    string refTriplo = $"({indiceActual})";
                    pilaTriplos.Push(refTriplo);
                }
            }

            return new ResultadoCompilacion
            {
                ResultadoCuadruplo = pilaCuadruplos.Count > 0 ? pilaCuadruplos.Pop() : "",
                ResultadoTriplo = pilaTriplos.Count > 0 ? pilaTriplos.Pop() : ""
            };
        }

        private static bool EsOperando(string t)
        {
            return double.TryParse(t, out _) || t.StartsWith("$") || (t.StartsWith("\"") && t.EndsWith("\"")) || char.IsLetter(t[0]);
        }
    }
}