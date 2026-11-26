using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class OptimizadorCiclos
    {
        public static List<InstruccionIntermedia> Optimizar(List<InstruccionIntermedia> codigo, List<string> bitacora)
        {
            var lista = codigo.Select(c => c.Clonar()).ToList();

            // Mapear posiciones de todas las etiquetas
            var posicionesEtiquetas = new Dictionary<string, int>();
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i].Operador == "LABEL")
                    posicionesEtiquetas[lista[i].Arg1] = i;
            }

            // Buscar saltos hacia atrás (back-edges) que definen ciclos
            for (int i = 0; i < lista.Count; i++)
            {
                string destinoSalto = null;
                if (lista[i].Operador == "JUMP") destinoSalto = lista[i].Arg1;
                else if (lista[i].Operador == "ifTrue" || lista[i].Operador == "ifFalse") destinoSalto = lista[i].Resultado;

                if (destinoSalto != null && posicionesEtiquetas.TryGetValue(destinoSalto, out int inicioCiclo) && inicioCiclo < i)
                {
                    int finCiclo = i;

                    // 1. Recolectar todas las variables/temporales modificados dentro del ciclo
                    var modificadas = new HashSet<string>();
                    var conteoAsignaciones = new Dictionary<string, int>();

                    for (int k = inicioCiclo + 1; k < finCiclo; k++)
                    {
                        var inst = lista[k];
                        if (!string.IsNullOrEmpty(inst.Resultado) && inst.Resultado != "-" &&
                            inst.Operador != "ifFalse" && inst.Operador != "ifTrue")
                        {
                            modificadas.Add(inst.Resultado);
                            if (!conteoAsignaciones.ContainsKey(inst.Resultado)) conteoAsignaciones[inst.Resultado] = 0;
                            conteoAsignaciones[inst.Resultado]++;
                        }
                    }

                    // 2. Identificar instrucciones invariantes (sus operandos no cambian en el ciclo y solo se asigna una vez)
                    var invariantes = new List<InstruccionIntermedia>();
                    var indicesExtraidos = new HashSet<int>();

                    for (int k = inicioCiclo + 1; k < finCiclo; k++)
                    {
                        var inst = lista[k];
                        if (inst.Operador == "LABEL" || inst.Operador == "JUMP" ||
                            inst.Operador == "ifFalse" || inst.Operador == "ifTrue" || inst.Operador == "PRINT")
                            continue;

                        bool arg1Invariante = inst.Arg1 == "-" || !modificadas.Contains(inst.Arg1);
                        bool arg2Invariante = inst.Arg2 == "-" || !modificadas.Contains(inst.Arg2);
                        bool asignacionUnica = conteoAsignaciones.ContainsKey(inst.Resultado) && conteoAsignaciones[inst.Resultado] == 1;

                        // Evitar mover asignaciones directas a variables de usuario si hay saltos condicionales internos complejos
                        if (arg1Invariante && arg2Invariante && asignacionUnica)
                        {
                            invariantes.Add(inst.Clonar());
                            indicesExtraidos.Add(k);
                            // Marcar el resultado como ya disponible fuera del ciclo para permitir movimiento en cascada
                            modificadas.Remove(inst.Resultado);
                            bitacora.Add($"[Ciclos - LICM] Moviendo instrucción invariante fuera de {destinoSalto}: {inst}");
                        }
                    }

                    // 3. Insertar invariantes en el pre-header (justo antes de LABEL inicioCiclo)
                    if (invariantes.Count > 0)
                    {
                        var nuevaLista = new List<InstruccionIntermedia>();
                        for (int k = 0; k < lista.Count; k++)
                        {
                            if (k == inicioCiclo)
                                nuevaLista.AddRange(invariantes);

                            if (!indicesExtraidos.Contains(k))
                                nuevaLista.Add(lista[k]);
                        }
                        lista = nuevaLista;
                        break; // Re-evaluar índices en la siguiente pasada del pipeline
                    }
                }
            }

            return lista;
        }
    }
}