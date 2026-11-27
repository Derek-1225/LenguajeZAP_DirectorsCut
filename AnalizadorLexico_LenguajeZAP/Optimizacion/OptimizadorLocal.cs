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

            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL" || inst.Operador == "FUNC" || inst.Operador == "ENDFUNC")
                {
                    if (bloqueActual.Count > 0)
                    {
                        resultado.AddRange(OptimizarBloqueBasico(bloqueActual, bitacora));
                        bloqueActual.Clear();
                    }
                    resultado.Add(inst.Clonar());
                }
                else if (inst.Operador == "JUMP" || inst.Operador == "ifFalse" || inst.Operador == "ifTrue" ||
                         inst.Operador == "CALL" || inst.Operador == "RETURN")
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
            var subexpresiones = new Dictionary<string, string>();
            var optimizado = new List<InstruccionIntermedia>();

            foreach (var inst in bloque)
            {
                if (inst.Operador == "JUMP" || inst.Operador == "ifFalse" || inst.Operador == "ifTrue" ||
                    inst.Operador == "PRINT" || inst.Operador == "PARAM" || inst.Operador == "RETURN")
                {
                    if (!string.IsNullOrEmpty(inst.Arg1) && inst.Arg1 != "-")
                    {
                        if (tablaConstantes.ContainsKey(inst.Arg1)) inst.Arg1 = tablaConstantes[inst.Arg1];
                        else if (tablaCopias.ContainsKey(inst.Arg1)) inst.Arg1 = tablaCopias[inst.Arg1];
                    }

                    optimizado.Add(inst);
                    continue;
                }

                if (inst.Operador == "CALL")
                {
                    tablaConstantes.Clear();
                    tablaCopias.Clear();
                    subexpresiones.Clear();
                    optimizado.Add(inst);
                    continue;
                }

                if (tablaConstantes.ContainsKey(inst.Arg1)) inst.Arg1 = tablaConstantes[inst.Arg1];
                else if (tablaCopias.ContainsKey(inst.Arg1)) inst.Arg1 = tablaCopias[inst.Arg1];

                if (tablaConstantes.ContainsKey(inst.Arg2)) inst.Arg2 = tablaConstantes[inst.Arg2];
                else if (tablaCopias.ContainsKey(inst.Arg2)) inst.Arg2 = tablaCopias[inst.Arg2];

                AplicarIdentidadesAlgebraicas(inst, bitacora);

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
                }

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

                if (inst.Operador != "=" && inst.Arg2 != "-" && inst.Arg1 != destino && inst.Arg2 != destino)
                {
                    string firma = $"{inst.Operador}|{inst.Arg1}|{inst.Arg2}";
                    if (!subexpresiones.ContainsKey(firma))
                        subexpresiones[firma] = destino;
                }

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

        public static List<InstruccionP> Optimizar(List<InstruccionP> codigo, List<string> bitacora)
        {
            var resultado = new List<InstruccionP>();
            var bloqueActual = new List<InstruccionP>();

            foreach (var inst in codigo)
            {
                string op = (inst.Operacion ?? "").ToUpperInvariant();
                if (op == "LABEL" || op == "FUNC" || op == "ENDFUNC")
                {
                    if (bloqueActual.Count > 0)
                    {
                        resultado.AddRange(OptimizarBloqueBasicoP(bloqueActual, bitacora));
                        bloqueActual.Clear();
                    }
                    resultado.Add(new InstruccionP(inst.Operacion, inst.Argumento1, inst.Argumento2));
                }
                else if (op == "JMP" || op == "FJMP" || op == "CALL" || op == "RET")
                {
                    bloqueActual.Add(new InstruccionP(inst.Operacion, inst.Argumento1, inst.Argumento2));
                    resultado.AddRange(OptimizarBloqueBasicoP(bloqueActual, bitacora));
                    bloqueActual.Clear();
                }
                else
                {
                    bloqueActual.Add(new InstruccionP(inst.Operacion, inst.Argumento1, inst.Argumento2));
                }
            }

            if (bloqueActual.Count > 0)
                resultado.AddRange(OptimizarBloqueBasicoP(bloqueActual, bitacora));

            return resultado;
        }

        private static List<InstruccionP> OptimizarBloqueBasicoP(List<InstruccionP> bloque, List<string> bitacora)
        {
            var constantesVariables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var optimizado = new List<InstruccionP>();

            for (int i = 0; i < bloque.Count; i++)
            {
                var actual = new InstruccionP(bloque[i].Operacion, bloque[i].Argumento1, bloque[i].Argumento2);
                string opAct = (actual.Operacion ?? "").ToUpperInvariant();

                if (opAct == "LOD" && constantesVariables.TryGetValue(actual.Argumento1, out string valConst))
                {
                    bitacora.Add($"[Local P - Propagación] LOD {actual.Argumento1} -> LIT {valConst}");
                    actual = new InstruccionP("LIT", valConst);
                    opAct = "LIT";
                }

                if (optimizado.Count >= 2 && EsOperacionBinariaP(opAct))
                {
                    var prev1 = optimizado[optimizado.Count - 1];
                    var prev2 = optimizado[optimizado.Count - 2];

                    if (prev2.Operacion.ToUpperInvariant() == "LIT" && prev1.Operacion.ToUpperInvariant() == "LIT" &&
                        EsNumero(prev2.Argumento1, out double v1) && EsNumero(prev1.Argumento1, out double v2) &&
                        EvaluarOperacionP(opAct, v1, v2, out string resConst))
                    {
                        bitacora.Add($"[Local P - Plegado] {prev2.Argumento1} {opAct} {prev1.Argumento1} -> {resConst}");
                        optimizado.RemoveAt(optimizado.Count - 1);
                        optimizado.RemoveAt(optimizado.Count - 1);
                        optimizado.Add(new InstruccionP("LIT", resConst));
                        continue;
                    }

                    if (prev1.Operacion.ToUpperInvariant() == "LIT" && EsNumero(prev1.Argumento1, out double k))
                    {
                        if ((opAct == "ADD" || opAct == "SUB") && Math.Abs(k) < 1e-9)
                        {
                            bitacora.Add($"[Local P - Identidad] {opAct} 0 eliminado");
                            optimizado.RemoveAt(optimizado.Count - 1);
                            continue;
                        }
                        if ((opAct == "MUL" || opAct == "DIV") && Math.Abs(k - 1.0) < 1e-9)
                        {
                            bitacora.Add($"[Local P - Identidad] {opAct} 1 eliminado");
                            optimizado.RemoveAt(optimizado.Count - 1);
                            continue;
                        }
                        if (opAct == "MUL" && Math.Abs(k) < 1e-9)
                        {
                            int idx = optimizado.Count - 2;
                            int necesidad = 1;
                            bool esPura = true;

                            while (idx >= 0)
                            {
                                string opPrev = (optimizado[idx].Operacion ?? "").ToUpperInvariant();
                                if (opPrev == "LIT" || opPrev == "LOD")
                                {
                                    necesidad--;
                                }
                                else if (EsOperacionBinariaP(opPrev) || opPrev == "AND" || opPrev == "OR")
                                {
                                    necesidad++;
                                }
                                else
                                {
                                    esPura = false;
                                    break;
                                }

                                if (necesidad == 0) break;
                                idx--;
                            }

                            if (esPura && necesidad == 0)
                            {
                                bitacora.Add("[Local P - Anulación] Producto por 0 -> LIT 0");
                                optimizado.RemoveRange(idx, optimizado.Count - idx);
                                optimizado.Add(new InstruccionP("LIT", "0"));
                                continue;
                            }
                        }
                        if (opAct == "MUL" && Math.Abs(k - 2.0) < 1e-9 &&
                            (prev2.Operacion.ToUpperInvariant() == "LOD" || prev2.Operacion.ToUpperInvariant() == "LIT"))
                        {
                            bitacora.Add("[Local P - Reducción Potencia] * 2 -> suma duplicada");
                            optimizado.RemoveAt(optimizado.Count - 1);
                            optimizado.Add(new InstruccionP(prev2.Operacion, prev2.Argumento1, prev2.Argumento2));
                            optimizado.Add(new InstruccionP("ADD"));
                            continue;
                        }
                    }
                }

                if (opAct == "STO" && !string.IsNullOrEmpty(actual.Argumento1))
                {
                    constantesVariables.Remove(actual.Argumento1);
                    if (optimizado.Count > 0 && optimizado[optimizado.Count - 1].Operacion.ToUpperInvariant() == "LIT")
                        constantesVariables[actual.Argumento1] = optimizado[optimizado.Count - 1].Argumento1;
                }

                optimizado.Add(actual);
            }

            return optimizado;
        }

        private static bool EsOperacionBinariaP(string op)
        {
            switch (op)
            {
                case "ADD":
                case "SUB":
                case "MUL":
                case "DIV":
                case "POW":
                case "EQ":
                case "NE":
                case "LT":
                case "GT":
                case "LE":
                case "GE":
                    return true;
                default:
                    return false;
            }
        }

        private static bool EvaluarOperacionP(string op, double a, double b, out string res)
        {
            switch (op)
            {
                case "ADD": return EvaluarOperacion("+", a, b, out res);
                case "SUB": return EvaluarOperacion("-", a, b, out res);
                case "MUL": return EvaluarOperacion("*", a, b, out res);
                case "DIV": return EvaluarOperacion("/", a, b, out res);
                case "POW": return EvaluarOperacion("^", a, b, out res);
                case "GT": return EvaluarOperacion(">", a, b, out res);
                case "LT": return EvaluarOperacion("<", a, b, out res);
                case "GE": return EvaluarOperacion(">=", a, b, out res);
                case "LE": return EvaluarOperacion("<=", a, b, out res);
                case "EQ": return EvaluarOperacion("==", a, b, out res);
                case "NE": return EvaluarOperacion("!=", a, b, out res);
                default: res = ""; return false;
            }
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
                else if (inst.Arg1 == "2") { bitacora.Add($"[Local - Reducción Potencia] 2 * {inst.Arg2} -> {inst.Arg2} + {inst.Arg2}"); inst.Operador = "+"; inst.Arg1 = inst.Arg2; }
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