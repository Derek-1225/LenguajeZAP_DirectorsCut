using System;
using System.Collections.Generic;
using System.Globalization;

namespace AnalizadorLexico_LenguajeZAP
{
    public class OptimizadorLocal
    {
        public static List<InstruccionIntermedia> Optimizar(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var resultado = new List<InstruccionIntermedia>();
            var bloqueActual = new List<InstruccionIntermedia>();

            // Particionar en bloques básicos delimitados por LABEL, JUMP, ifFalse, ifTrue
            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL")
                {
                    if (bloqueActual.Count > 0)
                    {
                        resultado.AddRange(OptimizarBloqueBasico(bloqueActual, bitacora));
                        bloqueActual.Clear();
                    }
                    resultado.Add(inst.Clonar());
                }
                else if (inst.Operador == "JUMP" || inst.Operador == "ifFalse" || inst.Operador == "ifTrue")
                {
                    bloqueActual.Add(inst.Clonar());
                    resultado.AddRange(OptimizarBloqueBasico(bloqueActual, bitacora));
                    bloqueActual.Clear();
                }
                else
                {
                    bloqueActual.Add(inst.Clonar());
                }
            }

            if (bloqueActual.Count > 0)
                resultado.AddRange(OptimizarBloqueBasico(bloqueActual, bitacora));

            return resultado;
        }

        private static List<InstruccionIntermedia> OptimizarBloqueBasico(List<InstruccionIntermedia> bloque, List<string> bitacora)
        {
            var tablaConstantes = new Dictionary<string, string>();
            var tablaCopias = new Dictionary<string, string>();
            var subexpresiones = new Dictionary<string, string>(); // "op|arg1|arg2" -> temporal
            var optimizado = new List<InstruccionIntermedia>();

            foreach (var inst in bloque)
            {
                if (inst.Operador == "JUMP" || inst.Operador == "ifFalse" || inst.Operador == "ifTrue" || inst.Operador == "PRINT")
                {
                    // Propagar constantes/copias en argumentos de control o impresión
                    if (tablaConstantes.ContainsKey(inst.Arg1)) inst.Arg1 = tablaConstantes[inst.Arg1];
                    else if (tablaCopias.ContainsKey(inst.Arg1)) inst.Arg1 = tablaCopias[inst.Arg1];

                    optimizado.Add(inst);
                    continue;
                }

                // 1. Propagación de constantes y copias en Arg1 y Arg2
                if (tablaConstantes.ContainsKey(inst.Arg1)) inst.Arg1 = tablaConstantes[inst.Arg1];
                else if (tablaCopias.ContainsKey(inst.Arg1)) inst.Arg1 = tablaCopias[inst.Arg1];

                if (tablaConstantes.ContainsKey(inst.Arg2)) inst.Arg2 = tablaConstantes[inst.Arg2];
                else if (tablaCopias.ContainsKey(inst.Arg2)) inst.Arg2 = tablaCopias[inst.Arg2];

                // 2. Reducción de potencia e identidades algebraicas
                AplicarIdentidadesAlgebraicas(inst, bitacora);

                // 3. Plegado de constantes (Constant Folding)
                if (EsNumero(inst.Arg1, out double v1) && EsNumero(inst.Arg2, out double v2))
                {
                    if (EvaluarOperacion(inst.Operador, v1, v2, out string resConst))
                    {
                        bitacora.Add($"[Local - Plegado] {inst.Arg1} {inst.Operador} {inst.Arg2} -> {resConst}");
                        inst.Operador = "=";
                        inst.Arg1 = resConst;
                        inst.Arg2 = "-";
                    }
                }

                // 4. Eliminación de subexpresiones comunes locales (CSE)
                if (inst.Operador != "=" && inst.Arg2 != "-")
                {
                    string firma = $"{inst.Operador}|{inst.Arg1}|{inst.Arg2}";
                    if (subexpresiones.TryGetValue(firma, out string tempPrevio))
                    {
                        bitacora.Add($"[Local - CSE] Reutilizando {tempPrevio} para {inst.Arg1} {inst.Operador} {inst.Arg2}");
                        inst.Operador = "=";
                        inst.Arg1 = tempPrevio;
                        inst.Arg2 = "-";
                    }
                    else
                    {
                        subexpresiones[firma] = inst.Resultado;
                    }
                }

                // Invalidar entradas previas donde 'Resultado' participaba antes de ser sobrescrito
                string destino = inst.Resultado;
                tablaConstantes.Remove(destino);
                tablaCopias.Remove(destino);

                var copiasInvalidas = new List<string>();
                foreach (var kv in tablaCopias)
                    if (kv.Value == destino) copiasInvalidas.Add(kv.Key);
                foreach (var k in copiasInvalidas) tablaCopias.Remove(k);

                var cseInvalidas = new List<string>();
                foreach (var kv in subexpresiones)
                {
                    var partes = kv.Key.Split('|');
                    if (partes[1] == destino || partes[2] == destino || kv.Value == destino)
                        cseInvalidas.Add(kv.Key);
                }
                foreach (var k in cseInvalidas) subexpresiones.Remove(k);

                // Registrar nuevo estado si es asignación simple
                if (inst.Operador == "=")
                {
                    if (EsNumero(inst.Arg1, out _) || inst.Arg1.StartsWith("\""))
                        tablaConstantes[destino] = inst.Arg1;
                    else if (inst.Arg1 != destino)
                        tablaCopias[destino] = inst.Arg1;
                }

                optimizado.Add(inst);
            }

            return optimizado;
        }

        private static void AplicarIdentidadesAlgebraicas(InstruccionIntermedia inst, List<string> bitacora)
        {
            if (inst.Operador == "+")
            {
                if (inst.Arg2 == "0") { bitacora.Add($"[Local - Identidad] {inst.Arg1} + 0 -> {inst.Arg1}"); inst.Operador = "="; inst.Arg2 = "-"; }
                else if (inst.Arg1 == "0") { bitacora.Add($"[Local - Identidad] 0 + {inst.Arg2} -> {inst.Arg2}"); inst.Operador = "="; inst.Arg1 = inst.Arg2; inst.Arg2 = "-"; }
            }
            else if (inst.Operador == "-" && inst.Arg2 == "0")
            {
                bitacora.Add($"[Local - Identidad] {inst.Arg1} - 0 -> {inst.Arg1}");
                inst.Operador = "="; inst.Arg2 = "-";
            }
            else if (inst.Operador == "*")
            {
                if (inst.Arg2 == "1") { bitacora.Add($"[Local - Identidad] {inst.Arg1} * 1 -> {inst.Arg1}"); inst.Operador = "="; inst.Arg2 = "-"; }
                else if (inst.Arg1 == "1") { bitacora.Add($"[Local - Identidad] 1 * {inst.Arg2} -> {inst.Arg2}"); inst.Operador = "="; inst.Arg1 = inst.Arg2; inst.Arg2 = "-"; }
                else if (inst.Arg2 == "0" || inst.Arg1 == "0") { bitacora.Add($"[Local - Anulación] Multiplicación por 0 -> 0"); inst.Operador = "="; inst.Arg1 = "0"; inst.Arg2 = "-"; }
                else if (inst.Arg2 == "2") { bitacora.Add($"[Local - Reducción Potencia] {inst.Arg1} * 2 -> {inst.Arg1} + {inst.Arg1}"); inst.Operador = "+"; inst.Arg2 = inst.Arg1; }
            }
            else if (inst.Operador == "/" && inst.Arg2 == "1")
            {
                bitacora.Add($"[Local - Identidad] {inst.Arg1} / 1 -> {inst.Arg1}");
                inst.Operador = "="; inst.Arg2 = "-";
            }
            else if (inst.Operador == "^")
            {
                if (inst.Arg2 == "2") { bitacora.Add($"[Local - Reducción Potencia] {inst.Arg1} ^ 2 -> {inst.Arg1} * {inst.Arg1}"); inst.Operador = "*"; inst.Arg2 = inst.Arg1; }
                else if (inst.Arg2 == "1") { bitacora.Add($"[Local - Identidad] {inst.Arg1} ^ 1 -> {inst.Arg1}"); inst.Operador = "="; inst.Arg2 = "-"; }
                else if (inst.Arg2 == "0") { bitacora.Add($"[Local - Identidad] {inst.Arg1} ^ 0 -> 1"); inst.Operador = "="; inst.Arg1 = "1"; inst.Arg2 = "-"; }
            }
        }

        private static bool EsNumero(string s, out double val) =>
            double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out val);

        private static bool EvaluarOperacion(string op, double a, double b, out string res)
        {
            res = "";
            switch (op)
            {
                case "+": res = (a + b).ToString(CultureInfo.InvariantCulture); return true;
                case "-": res = (a - b).ToString(CultureInfo.InvariantCulture); return true;
                case "*": res = (a * b).ToString(CultureInfo.InvariantCulture); return true;
                case "/": if (b == 0) return false; res = (a / b).ToString(CultureInfo.InvariantCulture); return true;
                case "^": res = Math.Pow(a, b).ToString(CultureInfo.InvariantCulture); return true;
                case ">": res = (a > b) ? "1" : "0"; return true;
                case "<": res = (a < b) ? "1" : "0"; return true;
                case ">=": res = (a >= b) ? "1" : "0"; return true;
                case "<=": res = (a <= b) ? "1" : "0"; return true;
                case "==": res = (Math.Abs(a - b) < 1e-9) ? "1" : "0"; return true;
                case "!=": res = (Math.Abs(a - b) >= 1e-9) ? "1" : "0"; return true;
                default: return false;
            }
        }
    }
}