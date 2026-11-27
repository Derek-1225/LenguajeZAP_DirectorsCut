using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class OptimizadorGlobal
    {
        public static List<InstruccionIntermedia> Optimizar(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var actual = codigo.Select(c => c.Clonar()).ToList();

            actual = SimplificarSaltosConstantes(actual, bitacora);
            int countAntes;
            do
            {
                countAntes = actual.Count;
                actual = EliminarEtiquetasHuerfanas(actual, bitacora);
                actual = EliminarCodigoInalcanzable(actual, bitacora);
            } while (actual.Count < countAntes);
            actual = EliminarCodigoMuerto(actual, bitacora);
            actual = EliminarEtiquetasHuerfanas(actual, bitacora);

            return actual;
        }

        private static List<InstruccionIntermedia> SimplificarSaltosConstantes(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var res = new List<InstruccionIntermedia>();
            foreach (var inst in codigo)
            {
                if (inst.Operador == "ifFalse")
                {
                    if (inst.Arg1 == "0" || inst.Arg1.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        bitacora.Add($"[Global - Salto Constante] ifFalse 0 -> goto {inst.Resultado}");
                        res.Add(new InstruccionIntermedia("JUMP", inst.Resultado, "-", "-"));
                        continue;
                    }
                    if (inst.Arg1 == "1" || inst.Arg1.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        bitacora.Add($"[Global - Salto Constante] Eliminando salto muerto: ifFalse 1 goto {inst.Resultado}");
                        continue;
                    }
                }
                else if (inst.Operador == "ifTrue")
                {
                    if (inst.Arg1 == "1" || inst.Arg1.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        bitacora.Add($"[Global - Salto Constante] ifTrue 1 -> goto {inst.Resultado}");
                        res.Add(new InstruccionIntermedia("JUMP", inst.Resultado, "-", "-"));
                        continue;
                    }
                    if (inst.Arg1 == "0" || inst.Arg1.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        bitacora.Add($"[Global - Salto Constante] Eliminando salto muerto: ifTrue 0 goto {inst.Resultado}");
                        continue;
                    }
                }
                res.Add(inst);
            }
            return res;
        }

        private static List<InstruccionIntermedia> EliminarCodigoInalcanzable(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var res = new List<InstruccionIntermedia>();
            bool inalcanzable = false;

            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL")
                {
                    inalcanzable = false;
                    res.Add(inst);
                    continue;
                }

                if (inalcanzable)
                {
                    bitacora.Add($"[Global - Código Inalcanzable] Eliminado: {inst}");
                    continue;
                }

                res.Add(inst);

                if (inst.Operador == "JUMP")
                    inalcanzable = true;
            }
            return res;
        }

        private static List<InstruccionIntermedia> EliminarCodigoMuerto(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            bool huboCambios;
            var lista = codigo;

            do
            {
                huboCambios = false;
                var usados = new HashSet<string>();

                foreach (var inst in lista)
                {
                    if (inst.Operador == "LABEL" || inst.Operador == "JUMP") continue;
                    if (!string.IsNullOrEmpty(inst.Arg1) && inst.Arg1 != "-") usados.Add(inst.Arg1);
                    if (!string.IsNullOrEmpty(inst.Arg2) && inst.Arg2 != "-") usados.Add(inst.Arg2);
                }

                var filtrada = new List<InstruccionIntermedia>();
                foreach (var inst in lista)
                {
                    bool esTemporal = inst.Resultado.StartsWith("t") && int.TryParse(inst.Resultado.Substring(1), out _);
                    bool esOperacionPura = inst.Operador != "LABEL" && inst.Operador != "JUMP" &&
                                           inst.Operador != "ifFalse" && inst.Operador != "ifTrue" &&
                                           inst.Operador != "PRINT";

                    if (esOperacionPura && esTemporal && !usados.Contains(inst.Resultado))
                    {
                        bitacora.Add($"[Global - Código Muerto] Eliminando temporal sin uso: {inst}");
                        huboCambios = true;
                        continue;
                    }

                    filtrada.Add(inst);
                }
                lista = filtrada;
            } while (huboCambios);

            return lista;
        }

        private static List<InstruccionIntermedia> EliminarEtiquetasHuerfanas(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var etiquetasReferenciadas = new HashSet<string>();
            foreach (var inst in codigo)
            {
                if (inst.Operador == "JUMP") etiquetasReferenciadas.Add(inst.Arg1);
                else if (inst.Operador == "ifFalse" || inst.Operador == "ifTrue") etiquetasReferenciadas.Add(inst.Resultado);
            }

            var res = new List<InstruccionIntermedia>();
            foreach (var inst in codigo)
            {
                if (inst.Operador == "LABEL" && !etiquetasReferenciadas.Contains(inst.Arg1))
                {
                    bitacora.Add($"[Global - Etiqueta Huérfana] Eliminando {inst.Arg1}:");
                    continue;
                }
                res.Add(inst);
            }
            return res;
        }

        public static List<InstruccionP> Optimizar(List<InstruccionP> codigo, List<string> bitacora)
        {
            var actual = codigo.Select(c => new InstruccionP(c.Operacion, c.Argumento1, c.Argumento2)).ToList();

            actual = SimplificarSaltosConstantesP(actual, bitacora);
            int countAntes;
            do
            {
                countAntes = actual.Count;
                actual = EliminarEtiquetasHuerfanasP(actual, bitacora);
                actual = EliminarCodigoInalcanzableP(actual, bitacora);
            } while (actual.Count < countAntes);

            return actual;
        }

        private static List<InstruccionP> SimplificarSaltosConstantesP(List<InstruccionP> codigo, List<string> bitacora)
        {
            var res = new List<InstruccionP>();

            for (int i = 0; i < codigo.Count; i++)
            {
                var inst = codigo[i];
                string op = (inst.Operacion ?? "").ToUpperInvariant();

                if (op == "FJMP" && res.Count > 0 && res[res.Count - 1].Operacion.ToUpperInvariant() == "LIT")
                {
                    string lit = res[res.Count - 1].Argumento1;
                    bool esVerdadero = lit == "1" || lit.Equals("true", StringComparison.OrdinalIgnoreCase);
                    bool esFalso = lit == "0" || lit.Equals("false", StringComparison.OrdinalIgnoreCase);

                    if (esVerdadero || esFalso)
                    {
                        res.RemoveAt(res.Count - 1);
                        if (esFalso)
                        {
                            bitacora.Add($"[Global P - Salto Constante] FJMP con 0 -> JMP {inst.Argumento1}");
                            res.Add(new InstruccionP("JMP", inst.Argumento1));
                        }
                        else
                        {
                            bitacora.Add($"[Global P - Salto Constante] Eliminando FJMP con constante verdadera");
                        }
                        continue;
                    }
                }

                res.Add(inst);
            }

            return res;
        }

        private static List<InstruccionP> EliminarCodigoInalcanzableP(List<InstruccionP> codigo, List<string> bitacora)
        {
            var res = new List<InstruccionP>();
            bool inalcanzable = false;

            foreach (var inst in codigo)
            {
                string op = (inst.Operacion ?? "").ToUpperInvariant();
                if (op == "LABEL")
                {
                    inalcanzable = false;
                    res.Add(inst);
                    continue;
                }

                if (inalcanzable)
                {
                    bitacora.Add($"[Global P - Código Inalcanzable] Eliminado: {inst}");
                    continue;
                }

                res.Add(inst);
                if (op == "JMP")
                    inalcanzable = true;
            }

            return res;
        }

        private static List<InstruccionP> EliminarEtiquetasHuerfanasP(List<InstruccionP> codigo, List<string> bitacora)
        {
            var etiquetasReferenciadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var inst in codigo)
            {
                string op = (inst.Operacion ?? "").ToUpperInvariant();
                if (op == "JMP" || op == "FJMP")
                    etiquetasReferenciadas.Add(inst.Argumento1);
            }

            var res = new List<InstruccionP>();
            foreach (var inst in codigo)
            {
                if ((inst.Operacion ?? "").ToUpperInvariant() == "LABEL" && !etiquetasReferenciadas.Contains(inst.Argumento1))
                {
                    bitacora.Add($"[Global P - Etiqueta Huérfana] Eliminando {inst.Argumento1}:");
                    continue;
                }
                res.Add(inst);
            }

            return res;
        }
    }
}