using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class OptimizadorMirilla
    {
        public static List<InstruccionIntermedia> Optimizar(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var lista = codigo.Select(c => c.Clonar()).ToList();
            bool cambio;

            var redirecciones = new Dictionary<string, string>();
            for (int i = 0; i < lista.Count - 1; i++)
            {
                if (lista[i].Operador == "LABEL" && lista[i + 1].Operador == "JUMP")
                    redirecciones[lista[i].Arg1] = lista[i + 1].Arg1;
            }

            foreach (var inst in lista)
            {
                if (inst.Operador == "JUMP" && redirecciones.TryGetValue(inst.Arg1, out string destFinal))
                {
                    bitacora.Add($"[Mirilla - Jump Threading] goto {inst.Arg1} -> goto {destFinal}");
                    inst.Arg1 = destFinal;
                }
                else if ((inst.Operador == "ifFalse" || inst.Operador == "ifTrue") &&
                         redirecciones.TryGetValue(inst.Resultado, out string destCondFinal))
                {
                    bitacora.Add($"[Mirilla - Jump Threading] {inst.Operador} ... goto {inst.Resultado} -> goto {destCondFinal}");
                    inst.Resultado = destCondFinal;
                }
            }

            do
            {
                cambio = false;
                var nueva = new List<InstruccionIntermedia>();

                for (int i = 0; i < lista.Count; i++)
                {
                    var actual = lista[i];

                    if (actual.Operador == "=" && actual.Arg1 == actual.Resultado)
                    {
                        bitacora.Add($"[Mirilla - Autoasignación] Eliminado: {actual}");
                        cambio = true;
                        continue;
                    }

                    if (i + 1 < lista.Count)
                    {
                        var siguiente = lista[i + 1];

                        if (actual.Operador == "JUMP" && siguiente.Operador == "LABEL" && actual.Arg1 == siguiente.Arg1)
                        {
                            bitacora.Add($"[Mirilla - Salto Redundante] Eliminado goto {actual.Arg1} antes de {siguiente.Arg1}:");
                            cambio = true;
                            continue;
                        }

                        bool actualEsTemporal = actual.Resultado.StartsWith("t") && int.TryParse(actual.Resultado.Substring(1), out _);
                        if (actualEsTemporal &&
                            actual.Operador != "LABEL" && actual.Operador != "JUMP" &&
                            actual.Operador != "ifFalse" && actual.Operador != "ifTrue" &&
                            actual.Operador != "FUNC" && actual.Operador != "ENDFUNC" &&
                            actual.Operador != "PARAM" && actual.Operador != "RETURN" &&
                            siguiente.Operador == "=" && siguiente.Arg1 == actual.Resultado &&
                            !TemporalUsadoDespues(lista, i + 2, actual.Resultado))
                        {
                            string nuevaDesc = actual.Operador == "="
                                ? $"{siguiente.Resultado} = {actual.Arg1}"
                                : actual.Operador == "CALL"
                                    ? $"{siguiente.Resultado} = call {actual.Arg1}, {actual.Arg2}"
                                    : $"{siguiente.Resultado} = {actual.Arg1} {actual.Operador} {actual.Arg2}";
                            bitacora.Add($"[Mirilla - Fusión Temporal] {actual} + {siguiente} -> {nuevaDesc}");
                            nueva.Add(new InstruccionIntermedia(actual.Operador, actual.Arg1, actual.Arg2, siguiente.Resultado));
                            i++;
                            cambio = true;
                            continue;
                        }
                    }

                    nueva.Add(actual);
                }

                var etiquetasRef = new HashSet<string>();
                foreach (var inst in nueva)
                {
                    if (inst.Operador == "JUMP") etiquetasRef.Add(inst.Arg1);
                    else if (inst.Operador == "ifFalse" || inst.Operador == "ifTrue") etiquetasRef.Add(inst.Resultado);
                }

                var sinHuerfanas = new List<InstruccionIntermedia>();
                foreach (var inst in nueva)
                {
                    if (inst.Operador == "LABEL" && !etiquetasRef.Contains(inst.Arg1))
                    {
                        bitacora.Add($"[Mirilla - Etiqueta Huérfana] Eliminando {inst.Arg1}:");
                        cambio = true;
                        continue;
                    }
                    sinHuerfanas.Add(inst);
                }

                lista = sinHuerfanas;
            } while (cambio);

            return lista;
        }

        private static bool TemporalUsadoDespues(List<InstruccionIntermedia> lista, int inicio, string temp)
        {
            for (int k = inicio; k < lista.Count; k++)
            {
                if (lista[k].Arg1 == temp || lista[k].Arg2 == temp) return true;
                if (lista[k].Resultado == temp) return false;
            }
            return false;
        }

        public static List<InstruccionP> Optimizar(List<InstruccionP> codigo, List<string> bitacora)
        {
            var lista = codigo.Select(c => new InstruccionP(c.Operacion, c.Argumento1, c.Argumento2)).ToList();
            bool cambio;

            var redirecciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lista.Count - 1; i++)
            {
                string o1 = (lista[i].Operacion ?? "").ToUpperInvariant();
                string o2 = (lista[i + 1].Operacion ?? "").ToUpperInvariant();
                if (o1 == "LABEL" && o2 == "JMP")
                    redirecciones[lista[i].Argumento1] = lista[i + 1].Argumento1;
            }

            foreach (var inst in lista)
            {
                string op = (inst.Operacion ?? "").ToUpperInvariant();
                if ((op == "JMP" || op == "FJMP") && redirecciones.TryGetValue(inst.Argumento1, out string destFinal))
                {
                    bitacora.Add($"[Mirilla P - Jump Threading] {inst.Argumento1} -> {destFinal}");
                    inst.Argumento1 = destFinal;
                }
            }

            do
            {
                cambio = false;
                var nueva = new List<InstruccionP>();

                for (int i = 0; i < lista.Count; i++)
                {
                    var actual = lista[i];
                    string opAct = (actual.Operacion ?? "").ToUpperInvariant();

                    if (i + 1 < lista.Count)
                    {
                        var siguiente = lista[i + 1];
                        string opSig = (siguiente.Operacion ?? "").ToUpperInvariant();

                        if (opAct == "JMP" && opSig == "LABEL" &&
                            actual.Argumento1.Equals(siguiente.Argumento1, StringComparison.OrdinalIgnoreCase))
                        {
                            bitacora.Add($"[Mirilla P - Salto Redundante] Eliminado JMP {actual.Argumento1} antes de {siguiente.Argumento1}:");
                            cambio = true;
                            continue;
                        }

                        if (opAct == "LOD" && opSig == "STO" &&
                            actual.Argumento1.Equals(siguiente.Argumento1, StringComparison.OrdinalIgnoreCase))
                        {
                            bitacora.Add($"[Mirilla P - Carga/Almacén Redundante] Eliminado par LOD/STO {actual.Argumento1}");
                            i++;
                            cambio = true;
                            continue;
                        }
                    }

                    nueva.Add(actual);
                }

                var etiquetasRefP = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var inst in nueva)
                {
                    string opInst = (inst.Operacion ?? "").ToUpperInvariant();
                    if (opInst == "JMP" || opInst == "FJMP")
                        etiquetasRefP.Add(inst.Argumento1);
                }

                var sinHuerfanasP = new List<InstruccionP>();
                foreach (var inst in nueva)
                {
                    if ((inst.Operacion ?? "").ToUpperInvariant() == "LABEL" && !etiquetasRefP.Contains(inst.Argumento1))
                    {
                        bitacora.Add($"[Mirilla P - Etiqueta Huérfana] Eliminando {inst.Argumento1}:");
                        cambio = true;
                        continue;
                    }
                    sinHuerfanasP.Add(inst);
                }

                lista = sinHuerfanasP;
            } while (cambio);

            return lista;
        }
    }
}