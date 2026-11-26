using System;
using System.Collections.Generic;
using System.Linq;

namespace AnalizadorLexico_LenguajeZAP
{
    public class ResultadoOptimizacion
    {
        public List<InstruccionIntermedia> CodigoOptimizado { get; set; }
        public ReporteCostos CostoInicial { get; set; }
        public ReporteCostos CostoFinal { get; set; }
        public List<string> Bitacora { get; set; }
    }

    public static class GestorOptimizacion
    {
        public static ResultadoOptimizacion EjecutarTodo(List<Cuadruplo> cuadruplosOriginales)
        {
            var bitacora = new List<string>();

            // Convertir tus Cuadruplos actuales al modelo unificado
            var codigo = cuadruplosOriginales
                .Select(c => new InstruccionIntermedia(c.Operador, c.Arg1, c.Arg2, c.Resultado))
                .ToList();

            var costoAntes = ReporteCostos.Evaluar(codigo);

            // Pipeline iterativo de optimización (3.1.1 -> 3.1.2 -> 3.1.3 -> 3.1.4)
            codigo = OptimizadorLocal.Optimizar(codigo, bitacora);
            codigo = OptimizadorCiclos.Optimizar(codigo, bitacora);
            codigo = OptimizadorGlobal.Optimizar(codigo, bitacora);
            codigo = OptimizadorLocal.Optimizar(codigo, bitacora);   // Segunda pasada tras propagar constantes/LICM
            codigo = OptimizadorMirilla.Optimizar(codigo, bitacora);

            var costoDespues = ReporteCostos.Evaluar(codigo);
            costoDespues.BitacoraCambios = bitacora;

            return new ResultadoOptimizacion
            {
                CodigoOptimizado = codigo,
                CostoInicial = costoAntes,
                CostoFinal = costoDespues,
                Bitacora = bitacora
            };
        }

        // Método auxiliar para volcar el resultado optimizado a tus grids de Cuádruplos, Triplos y TAC
        public static void AplicarATablas(
            List<InstruccionIntermedia> optimizado,
            out List<Cuadruplo> nuevosCuadruplos,
            out List<Triplo> nuevosTriplos,
            out List<string> nuevoTac)
        {
            nuevosCuadruplos = new List<Cuadruplo>();
            nuevosTriplos = new List<Triplo>();
            nuevoTac = new List<string>();

            foreach (var inst in optimizado)
            {
                nuevosCuadruplos.Add(new Cuadruplo(inst.Operador, inst.Arg1, inst.Arg2, inst.Resultado));
                nuevoTac.Add(inst.ToString());

                if (inst.Operador == "=")
                    nuevosTriplos.Add(new Triplo("=", inst.Resultado, inst.Arg1));
                else if (inst.Operador == "ifFalse" || inst.Operador == "ifTrue")
                    nuevosTriplos.Add(new Triplo(inst.Operador, inst.Arg1, inst.Resultado));
                else
                    nuevosTriplos.Add(new Triplo(inst.Operador, inst.Arg1, inst.Arg2));
            }
        }
    }
}