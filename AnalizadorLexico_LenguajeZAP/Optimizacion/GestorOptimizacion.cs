using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class ResultadoOptimizacion
    {
        public List<InstruccionIntermedia> CodigoOptimizado { get; set; }
        public List<InstruccionP> CodigoPOptimizado { get; set; }
        public ReporteCostos CostoInicial { get; set; }
        public ReporteCostos CostoFinal { get; set; }
        public ReporteCostos CostoPInicial { get; set; }
        public ReporteCostos CostoPFinal { get; set; }
        public List<string> Bitacora { get; set; }
    }

    public static class GestorOptimizacion
    {
        public static ResultadoOptimizacion EjecutarTodo(
            List<Cuadruplo> cuadruplosOriginales,
            IEnumerable<InstruccionP> codigoPOriginal = null)
        {
            var bitacora = new List<string>();

            var codigo = cuadruplosOriginales
                .Select(c => new InstruccionIntermedia(c.Operador, c.Arg1, c.Arg2, c.Resultado))
                .ToList();

            var costoAntes = ReporteCostos.Evaluar(codigo);

            codigo = OptimizadorLocal.Optimizar(codigo, bitacora);
            codigo = OptimizadorCiclos.Optimizar(codigo, bitacora);
            codigo = OptimizadorGlobal.Optimizar(codigo, bitacora);
            codigo = OptimizadorLocal.Optimizar(codigo, bitacora);
            codigo = OptimizadorMirilla.Optimizar(codigo, bitacora);

            var costoDespues = ReporteCostos.Evaluar(codigo);
            costoDespues.BitacoraCambios = bitacora;

            List<InstruccionP> codigoPOpt = new List<InstruccionP>();
            ReporteCostos costoPAntes = new ReporteCostos();
            ReporteCostos costoPDespues = new ReporteCostos();

            if (codigoPOriginal != null)
            {
                codigoPOpt = codigoPOriginal
                    .Select(p => new InstruccionP(p.Operacion, p.Argumento1, p.Argumento2))
                    .ToList();

                costoPAntes = ReporteCostos.Evaluar(codigoPOpt);

                codigoPOpt = OptimizadorLocal.Optimizar(codigoPOpt, bitacora);
                codigoPOpt = OptimizadorCiclos.Optimizar(codigoPOpt, bitacora);
                codigoPOpt = OptimizadorGlobal.Optimizar(codigoPOpt, bitacora);
                codigoPOpt = OptimizadorLocal.Optimizar(codigoPOpt, bitacora);
                codigoPOpt = OptimizadorMirilla.Optimizar(codigoPOpt, bitacora);

                costoPDespues = ReporteCostos.Evaluar(codigoPOpt);
            }

            return new ResultadoOptimizacion
            {
                CodigoOptimizado = codigo,
                CodigoPOptimizado = codigoPOpt,
                CostoInicial = costoAntes,
                CostoFinal = costoDespues,
                CostoPInicial = costoPAntes,
                CostoPFinal = costoPDespues,
                Bitacora = bitacora
            };
        }

        public static void AplicarATablas(
            List<InstruccionIntermedia> optimizado,
            out List<Cuadruplo> nuevosCuadruplos,
            out List<Triplo> nuevosTriplos,
            out List<string> nuevoTac)
        {
            nuevosCuadruplos = new List<Cuadruplo>();
            nuevosTriplos = new List<Triplo>();
            nuevoTac = new List<string>();

            var mapaTempTriplo = new Dictionary<string, string>();

            string ResolverRefTriplo(string op) =>
                !string.IsNullOrEmpty(op) && mapaTempTriplo.TryGetValue(op, out string refIdx) ? refIdx : op;

            foreach (var inst in optimizado)
            {
                nuevosCuadruplos.Add(new Cuadruplo(inst.Operador, inst.Arg1, inst.Arg2, inst.Resultado));
                nuevoTac.Add(inst.ToString());

                int idxTriplo = nuevosTriplos.Count;
                string a1 = ResolverRefTriplo(inst.Arg1);
                string a2 = ResolverRefTriplo(inst.Arg2);

                if (inst.Operador == "=")
                    nuevosTriplos.Add(new Triplo("=", inst.Resultado, a1));
                else if (inst.Operador == "ifFalse" || inst.Operador == "ifTrue")
                    nuevosTriplos.Add(new Triplo(inst.Operador, a1, inst.Resultado));
                else
                    nuevosTriplos.Add(new Triplo(inst.Operador, a1, a2));

                if (!string.IsNullOrEmpty(inst.Resultado) &&
                    inst.Resultado.StartsWith("t") &&
                    int.TryParse(inst.Resultado.Substring(1), out _))
                {
                    mapaTempTriplo[inst.Resultado] = $"({idxTriplo})";
                }
            }
        }
    }
}