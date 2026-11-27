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

                        // Eliminación de instrucción semejante/idéntica consecutiva ($x = k; $x = k)
                        if (actual.Operador == siguiente.Operador &&
                            actual.Arg1 == siguiente.Arg1 &&
                            actual.Arg2 == siguiente.Arg2 &&
                            actual.Resultado == siguiente.Resultado &&
                            actual.Operador != "LABEL")
                        {
                            bitacora.Add($"[Mirilla - Instrucción Semejante] Eliminada repetición: {siguiente}");
                            nueva.Add(actual);
                            i++;
                            cambio = true;
                            continue;
                        }

                        // Eliminación de asignación sobrescrita sin lectura intermedia ($x = a; $x = b)
                        if (actual.Operador == "=" && siguiente.Operador == "=" &&
                            actual.Resultado == siguiente.Resultado &&
                            actual.Arg1 != siguiente.Resultado && actual.Arg2 != siguiente.Resultado)
                        {
                            bitacora.Add($"[Mirilla - Escritura Sobrescrita] Eliminada: {actual}");
                            cambio = true;
                            continue;
                        }

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
                            siguiente.Operador == "=" && siguiente.Arg1 == actual.Resultado &&
                            !TemporalUsadoDespues(lista, i + 2, actual.Resultado))
                        {
                            string nuevaDesc = actual.Operador == "="
                                ? $"{siguiente.Resultado} = {actual.Arg1}"
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

                    // Eliminación de secuencia semejante de 2 instrucciones repetidas (ej. LIT 15; STO $x repetido)
                    if (i + 3 < lista.Count)
                    {
                        var i1 = lista[i + 1];
                        var i2 = lista[i + 2];
                        var i3 = lista[i + 3];

                        string o1 = (i1.Operacion ?? "").ToUpperInvariant();
                        string o2 = (i2.Operacion ?? "").ToUpperInvariant();
                        string o3 = (i3.Operacion ?? "").ToUpperInvariant();

                        bool mismaOpCarga = opAct == o2 &&
                                            string.Equals(actual.Argumento1, i2.Argumento1, StringComparison.OrdinalIgnoreCase) &&
                                            string.Equals(actual.Argumento2, i2.Argumento2, StringComparison.OrdinalIgnoreCase);

                        bool mismaOpAlmacen = o1 == o3 &&
                                              string.Equals(i1.Argumento1, i3.Argumento1, StringComparison.OrdinalIgnoreCase) &&
                                              string.Equals(i1.Argumento2, i3.Argumento2, StringComparison.OrdinalIgnoreCase);

                        if (mismaOpCarga && mismaOpAlmacen && opAct != "LABEL" && o1 != "LABEL")
                        {
                            bitacora.Add($"[Mirilla P - Secuencia Semejante] Eliminada repetición: {i2} / {i3}");
                            nueva.Add(actual);
                            nueva.Add(i1);
                            i += 3;
                            cambio = true;
                            continue;
                        }
                    }

                    if (i + 1 < lista.Count)
                    {
                        var siguiente = lista[i + 1];
                        string opSig = (siguiente.Operacion ?? "").ToUpperInvariant();

                        // Eliminación de instrucción P semejante/idéntica consecutiva
                        if (opAct == opSig &&
                            opAct != "LABEL" &&
                            string.Equals(actual.Argumento1, siguiente.Argumento1, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(actual.Argumento2, siguiente.Argumento2, StringComparison.OrdinalIgnoreCase))
                        {
                            bitacora.Add($"[Mirilla P - Instrucción Semejante] Eliminada repetición: {siguiente}");
                            nueva.Add(actual);
                            i++;
                            cambio = true;
                            continue;
                        }

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