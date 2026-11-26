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

            var posicionesEtiquetas = new Dictionary<string, int>();
            for (int i = 0; i < lista.Count; i++)
            {
                if (lista[i].Operador == "LABEL")
                    posicionesEtiquetas[lista[i].Arg1] = i;
            }

            for (int i = 0; i < lista.Count; i++)
            {
                string destinoSalto = null;
                if (lista[i].Operador == "JUMP") destinoSalto = lista[i].Arg1;
                else if (lista[i].Operador == "ifTrue" || lista[i].Operador == "ifFalse") destinoSalto = lista[i].Resultado;

                if (destinoSalto != null && posicionesEtiquetas.TryGetValue(destinoSalto, out int inicioCiclo) && inicioCiclo < i)
                {
                    int finCiclo = i;

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

                        if (arg1Invariante && arg2Invariante && asignacionUnica)
                        {
                            invariantes.Add(inst.Clonar());
                            indicesExtraidos.Add(k);
                            modificadas.Remove(inst.Resultado);
                            bitacora.Add($"[Ciclos - LICM] Moviendo instrucción invariante fuera de {destinoSalto}: {inst}");
                        }
                    }

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
                        break;
                    }
                }
            }

            return lista;
        }

        public static List<InstruccionP> Optimizar(List<InstruccionP> codigo, List<string> bitacora)
        {
            var lista = codigo.Select(c => new InstruccionP(c.Operacion, c.Argumento1, c.Argumento2)).ToList();

            var posicionesEtiquetas = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < lista.Count; i++)
            {
                if ((lista[i].Operacion ?? "").ToUpperInvariant() == "LABEL" && !string.IsNullOrEmpty(lista[i].Argumento1))
                    posicionesEtiquetas[lista[i].Argumento1] = i;
            }

            for (int i = 0; i < lista.Count; i++)
            {
                string op = (lista[i].Operacion ?? "").ToUpperInvariant();
                if (op == "JMP" && posicionesEtiquetas.TryGetValue(lista[i].Argumento1, out int inicioCiclo) && inicioCiclo < i)
                {
                    var modificadas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    for (int k = inicioCiclo + 1; k < i; k++)
                    {
                        if ((lista[k].Operacion ?? "").ToUpperInvariant() == "STO" && !string.IsNullOrEmpty(lista[k].Argumento1))
                            modificadas.Add(lista[k].Argumento1);
                    }

                    var invariantes = new List<InstruccionP>();
                    var indicesExtraidos = new HashSet<int>();

                    for (int k = inicioCiclo + 1; k + 3 < i; k++)
                    {
                        string o0 = (lista[k].Operacion ?? "").ToUpperInvariant();
                        string o1 = (lista[k + 1].Operacion ?? "").ToUpperInvariant();
                        string o2 = (lista[k + 2].Operacion ?? "").ToUpperInvariant();
                        string o3 = (lista[k + 3].Operacion ?? "").ToUpperInvariant();

                        bool carga0 = (o0 == "LIT") || (o0 == "LOD" && !modificadas.Contains(lista[k].Argumento1));
                        bool carga1 = (o1 == "LIT") || (o1 == "LOD" && !modificadas.Contains(lista[k + 1].Argumento1));
                        bool opPura = o2 == "ADD" || o2 == "SUB" || o2 == "MUL" || o2 == "DIV";
                        bool stoTemp = o3 == "STO" && !string.IsNullOrEmpty(lista[k + 3].Argumento1) &&
                                       lista[k + 3].Argumento1.StartsWith("t", StringComparison.OrdinalIgnoreCase);

                        if (carga0 && carga1 && opPura && stoTemp)
                        {
                            for (int off = 0; off < 4; off++)
                            {
                                invariantes.Add(new InstruccionP(lista[k + off].Operacion, lista[k + off].Argumento1, lista[k + off].Argumento2));
                                indicesExtraidos.Add(k + off);
                            }
                            bitacora.Add($"[Ciclos P - LICM] Moviendo cálculo invariante fuera de {lista[i].Argumento1}: {lista[k + 3].Argumento1}");
                            k += 3;
                        }
                    }

                    if (invariantes.Count > 0)
                    {
                        var nuevaLista = new List<InstruccionP>();
                        for (int k = 0; k < lista.Count; k++)
                        {
                            if (k == inicioCiclo)
                                nuevaLista.AddRange(invariantes);

                            if (!indicesExtraidos.Contains(k))
                                nuevaLista.Add(lista[k]);
                        }
                        lista = nuevaLista;
                        break;
                    }
                }
            }

            return lista;
        }
    }
}