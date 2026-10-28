using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public static class CompiladorExpresiones
    {
        private static int contadorTemporales = 1;

        public static void ReiniciarTemporales()
        {
            contadorTemporales = 1;
        }

        public static string Compilar(
            List<string> tokensInfijos,
            List<Cuadruplo> cuadruplos,
            List<Triplo> triplos,
            List<string> tac)
        {
            List<string> postfija = TraductorNotaciones.InfijaAPostfija(tokensInfijos);

            // Pila separada para Cuádruplos/TAC (maneja temporales: t1, t2...)
            Stack<string> pilaCuadruplos = new Stack<string>();

            // Pila separada para Triplos (maneja referencias por índice: (0), (1)...)
            Stack<string> pilaTriplos = new Stack<string>();

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

                    // --- 1. PROCESAMIENTO PARA CUÁDRUPLOS Y TAC ---
                    string arg2Cuad = pilaCuadruplos.Pop();
                    string arg1Cuad = pilaCuadruplos.Pop();
                    string tempCuad = $"t{contadorTemporales++}";

                    cuadruplos.Add(new Cuadruplo(token, arg1Cuad, arg2Cuad, tempCuad));
                    tac.Add($"{tempCuad} = {arg1Cuad} {token} {arg2Cuad}");
                    pilaCuadruplos.Push(tempCuad);

                    // --- 2. PROCESAMIENTO PARA TRIPLOS (Referencias por índice) ---
                    string arg2Trip = pilaTriplos.Pop();
                    string arg1Trip = pilaTriplos.Pop();

                    // El índice del triplo que vamos a crear a continuación
                    int indiceActual = triplos.Count;

                    triplos.Add(new Triplo(token, arg1Trip, arg2Trip));

                    // Apuntamos a la posición en formato de paréntesis, ej: "(2)"
                    string referenciaTriplo = $"({indiceActual})";
                    pilaTriplos.Push(referenciaTriplo);
                }
            }

            // Retornamos el resultado final para el operador de asignación (=)
            return pilaCuadruplos.Count > 0 ? pilaCuadruplos.Pop() : "";
        }

        private static bool EsOperando(string t)
        {
            return double.TryParse(t, out _) || t.StartsWith("$") || (t.StartsWith("\"") && t.EndsWith("\"")) || char.IsLetter(t[0]);
        }
    }
}