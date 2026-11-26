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

            // 1. Simplificación de saltos con condiciones constantes
            actual = SimplificarSaltosConstantes(actual, bitacora);

            // 2. Eliminación de código inalcanzable después de JUMP
            actual = EliminarCodigoInalcanzable(actual, bitacora);

            // 3. Eliminación de código muerto (temporales/asignaciones no utilizadas)
            actual = EliminarCodigoMuerto(actual, bitacora);

            // 4. Eliminación de etiquetas huérfanas (nunca referenciadas por ningún salto)
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

                // Recolectar todos los símbolos leídos en todo el programa
                foreach (var inst in lista)
                {
                    if (inst.Operador == "LABEL" || inst.Operador == "JUMP") continue;
                    if (!string.IsNullOrEmpty(inst.Arg1) && inst.Arg1 != "-") usados.Add(inst.Arg1);
                    if (!string.IsNullOrEmpty(inst.Arg2) && inst.Arg2 != "-") usados.Add(inst.Arg2);
                }

                var filtrada = new List<InstruccionIntermedia>();
                foreach (var inst in lista)
                {
                    // Eliminar temporales (t1, t2...) o asignaciones internas que nunca se leen
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
    }
}