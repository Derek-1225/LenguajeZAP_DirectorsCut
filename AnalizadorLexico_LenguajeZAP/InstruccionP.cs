using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    // 1. CLASE AUXILIAR PÚBLICA A NIVEL DE NAMESPACE
    public class SwitchCaseNode
    {
        public string ValorCaso { get; set; } // null si es 'default'
        public List<InstruccionP> Instrucciones { get; set; }
        public bool TieneBreak { get; set; }
    }

    public class InstruccionP
    {
        public string Operacion { get; set; } // LIT, LOD, STO, ADD, SUB, EQ, LT, GT, FJMP, JMP, LABEL, etc.
        public string Argumento1 { get; set; }
        public string Argumento2 { get; set; }

        public InstruccionP(string operacion, string argumento1 = "", string argumento2 = "")
        {
            Operacion = operacion;
            Argumento1 = argumento1;
            Argumento2 = argumento2;
        }


        // Formato visual para las etiquetas (ej: "L1:")
        public override string ToString()
        {
            if (Operacion == "LABEL")
            {
                return $"{Argumento1}:";
            }
            return $"{Operacion} {Argumento1} {Argumento2}".Trim();
        }

        private static int contadorEtiquetas = 0;

        public static void ReiniciarContadorEtiquetas()
        {       
            contadorEtiquetas = 0;
        }

        public static string ObtenerNuevaEtiqueta()
        {
            return $"L{contadorEtiquetas++}";
        }

        // =====================================================================
        // 1. TRADUCTOR DE EXPRESIONES (Aritméticas, Relacionales y Lógicas)
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoExpresion(List<string> expTokens)
        {
            List<InstruccionP> codigoLocal = new List<InstruccionP>();
            List<string> tokensPostfijos = ConvertirAInfijoPostfijo(expTokens);

            foreach (var token in tokensPostfijos)
            {
                if (EsNumero(token) || (token.StartsWith("\"") && token.EndsWith("\"")))
                {
                    codigoLocal.Add(new InstruccionP("LIT", token));
                }
                else if (token.StartsWith("$") || EsIdentificadorValido(token))
                {
                    codigoLocal.Add(new InstruccionP("LOD", token));
                }
                else
                {
                    switch (token)
                    {
                        case "+": codigoLocal.Add(new InstruccionP("ADD")); break;
                        case "-": codigoLocal.Add(new InstruccionP("SUB")); break;
                        case "*": codigoLocal.Add(new InstruccionP("MUL")); break;
                        case "/": codigoLocal.Add(new InstruccionP("DIV")); break;
                        case "==": codigoLocal.Add(new InstruccionP("EQ")); break;
                        case "!=": codigoLocal.Add(new InstruccionP("NE")); break;
                        case "<": codigoLocal.Add(new InstruccionP("LT")); break;
                        case ">": codigoLocal.Add(new InstruccionP("GT")); break;
                        case "<=": codigoLocal.Add(new InstruccionP("LE")); break;
                        case ">=": codigoLocal.Add(new InstruccionP("GE")); break;
                        case "&&": codigoLocal.Add(new InstruccionP("AND")); break;
                        case "||": codigoLocal.Add(new InstruccionP("OR")); break;
                    }
                }
            }
            return codigoLocal;
        }

        // =====================================================================
        // 2. ASIGNACIÓN
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoDinamico(string nombreVariable, List<string> expTokens)
        {
            List<InstruccionP> codigoLocal = GenerarCodigoExpresion(expTokens);
            codigoLocal.Add(new InstruccionP("STO", nombreVariable));
            return codigoLocal;
        }

        // =====================================================================
        // 3. ESTRUCTURA IF (Simple)
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoIf(List<string> condTokens, List<InstruccionP> cuerpoIf)
        {
            // El contador ya se reinicia al inicio global de la compilación, no aquí.
            List<InstruccionP> codigoIf = new List<InstruccionP>();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Evaluar condición
            codigoIf.AddRange(GenerarCodigoExpresion(condTokens));

            // 2. Si la condición es falsa, salta al final
            codigoIf.Add(new InstruccionP("FJMP", etiquetaFin));

            // 3. Instrucciones del cuerpo del if
            codigoIf.AddRange(cuerpoIf);

            // 4. Etiqueta de salida
            codigoIf.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoIf;
        }

        // =====================================================================
        // 4. ESTRUCTURA IF-ELSE
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoIfElse(List<string> condTokens, List<InstruccionP> cuerpoIf, List<InstruccionP> cuerpoElse)
        {
            List<InstruccionP> codigoIfElse = new List<InstruccionP>();
            string etiquetaElse = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Evaluar condición
            codigoIfElse.AddRange(GenerarCodigoExpresion(condTokens));

            // 2. Si es falsa, salta al bloque else
            codigoIfElse.Add(new InstruccionP("FJMP", etiquetaElse));

            // 3. Cuerpo IF
            codigoIfElse.AddRange(cuerpoIf);
            codigoIfElse.Add(new InstruccionP("JMP", etiquetaFin)); // Salta al final para evitar el else

            // 4. Bloque ELSE
            codigoIfElse.Add(new InstruccionP("LABEL", etiquetaElse));
            codigoIfElse.AddRange(cuerpoElse);

            // 5. Fin
            codigoIfElse.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoIfElse;
        }

        // =====================================================================
        // 5. CICLO WHILE
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoWhile(List<string> condTokens, List<InstruccionP> cuerpoWhile)
        {
            List<InstruccionP> codigoWhile = new List<InstruccionP>();
            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Etiqueta de inicio de iteración
            codigoWhile.Add(new InstruccionP("LABEL", etiquetaInicio));

            // 2. Evaluar condición
            codigoWhile.AddRange(GenerarCodigoExpresion(condTokens));

            // 3. Si la condición es falsa, sale del ciclo
            codigoWhile.Add(new InstruccionP("FJMP", etiquetaFin));

            // 4. Cuerpo del while
            codigoWhile.AddRange(cuerpoWhile);

            // 5. Regresar al inicio a reevaluar
            codigoWhile.Add(new InstruccionP("JMP", etiquetaInicio));

            // 6. Fin del ciclo
            codigoWhile.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoWhile;
        }

        // =====================================================================
        // 6. CICLO DO-WHILE
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoDoWhile(List<string> condTokens, List<InstruccionP> cuerpoDoWhile)
        {
            List<InstruccionP> codigoDoWhile = new List<InstruccionP>();
            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Etiqueta de inicio del do
            codigoDoWhile.Add(new InstruccionP("LABEL", etiquetaInicio));

            // 2. Cuerpo del do-while (se ejecuta al menos una vez)
            codigoDoWhile.AddRange(cuerpoDoWhile);

            // 3. Evaluar condición del while
            codigoDoWhile.AddRange(GenerarCodigoExpresion(condTokens));

            // 4. Si la condición es falsa, sale; si es verdadera, repite
            codigoDoWhile.Add(new InstruccionP("FJMP", etiquetaFin));
            codigoDoWhile.Add(new InstruccionP("JMP", etiquetaInicio));

            // 5. Fin del ciclo
            codigoDoWhile.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoDoWhile;
        }

        // =====================================================================
        // 7. CICLO FOR
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoFor(List<InstruccionP> initInsts, List<string> condTokens, List<InstruccionP> incInsts, List<InstruccionP> cuerpoFor)
        {
            List<InstruccionP> codigoFor = new List<InstruccionP>();
            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // 1. Inicialización (ej: int $i = 0)
            codigoFor.AddRange(initInsts);

            // 2. Etiqueta de inicio de validación
            codigoFor.Add(new InstruccionP("LABEL", etiquetaInicio));

            // 3. Evaluar condición (ej: $i < 10)
            codigoFor.AddRange(GenerarCodigoExpresion(condTokens));
            codigoFor.Add(new InstruccionP("FJMP", etiquetaFin));

            // 4. Cuerpo del for
            codigoFor.AddRange(cuerpoFor);

            // 5. Incremento o actualización (ej: $i = $i + 1)
            codigoFor.AddRange(incInsts);

            // 6. Regresar a evaluar la condición
            codigoFor.Add(new InstruccionP("JMP", etiquetaInicio));

            // 7. Fin del ciclo
            codigoFor.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoFor;
        }

        // =====================================================================
        // GENERADOR DE CÓDIGO P PARA SWITCH-CASE-DEFAULT
        // =====================================================================
        public static List<InstruccionP> GenerarCodigoSwitch(List<string> exprTokens, List<SwitchCaseNode> casos)
        {
            List<InstruccionP> codigoSwitch = new List<InstruccionP>();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            // Separar los casos normales (case X) del caso por defecto (default)
            var casosNormales = casos.Where(c => c.ValorCaso != null).ToList();
            var casoDefault = casos.FirstOrDefault(c => c.ValorCaso == null);

            // Asignar etiquetas secuenciales para los cuerpos de cada caso
            List<string> etiquetasCuerpo = new List<string>();
            for (int i = 0; i < casosNormales.Count; i++)
            {
                etiquetasCuerpo.Add(ObtenerNuevaEtiqueta());
            }

            // Si hay default, se le asigna su etiqueta; si no, apunta directo al fin
            string etiquetaDefaultCuerpo = casoDefault != null ? ObtenerNuevaEtiqueta() : etiquetaFin;

            // 1. Bloque de Evaluaciones (Comparaciones secuenciales)
            for (int i = 0; i < casosNormales.Count; i++)
            {
                codigoSwitch.AddRange(GenerarCodigoExpresion(exprTokens));
                codigoSwitch.Add(new InstruccionP("LIT", casosNormales[i].ValorCaso));
                codigoSwitch.Add(new InstruccionP("NE")); // Compara: si son distintos da 1, si son iguales da 0
                codigoSwitch.Add(new InstruccionP("FJMP", etiquetasCuerpo[i])); // Si da 0 (son iguales), salta al cuerpo del caso
            }

            // Si ningún case coincidió, saltar al default (o al fin si no hay default)
            if (casoDefault != null)
            {
                codigoSwitch.Add(new InstruccionP("JMP", etiquetaDefaultCuerpo));
            }
            else
            {
                codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
            }

            // 2. Cuerpos de los casos normales (case)
            for (int i = 0; i < casosNormales.Count; i++)
            {
                codigoSwitch.Add(new InstruccionP("LABEL", etiquetasCuerpo[i]));
                codigoSwitch.AddRange(casosNormales[i].Instrucciones);

                if (casosNormales[i].TieneBreak)
                {
                    codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
                }
            }

            // 3. Cuerpo del caso por defecto (default)
            if (casoDefault != null)
            {
                codigoSwitch.Add(new InstruccionP("LABEL", etiquetaDefaultCuerpo));
                codigoSwitch.AddRange(casoDefault.Instrucciones);

                if (casoDefault.TieneBreak)
                {
                    codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
                }
            }

            // 4. Etiqueta de salida general
            codigoSwitch.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoSwitch;
        }

        // =====================================================================
        // PRECEDENCIA Y SHUNTING-YARD
        // =====================================================================
        private static int ObtenerPrecedencia(string operador)
        {
            if (operador == "||") return 1;
            if (operador == "&&") return 2;
            if (operador == "==" || operador == "!=") return 3;
            if (operador == "<" || operador == ">" || operador == "<=" || operador == ">=") return 4;
            if (operador == "+" || operador == "-") return 5;
            if (operador == "*" || operador == "/") return 6;
            return 0;
        }

        private static List<string> ConvertirAInfijoPostfijo(List<string> tokensInfijos)
        {
            List<string> salida = new List<string>();
            Stack<string> operadores = new Stack<string>();

            foreach (var t in tokensInfijos)
            {
                if (EsNumero(t) || t.StartsWith("$") || (t.StartsWith("\"") && t.EndsWith("\"")))
                {
                    salida.Add(t);
                }
                else if (ObtenerPrecedencia(t) > 0)
                {
                    while (operadores.Count > 0 &&
                           operadores.Peek() != "(" &&
                           ObtenerPrecedencia(operadores.Peek()) >= ObtenerPrecedencia(t))
                    {
                        salida.Add(operadores.Pop());
                    }
                    operadores.Push(t);
                }
                else if (t == "(")
                {
                    operadores.Push(t);
                }
                else if (t == ")")
                {
                    while (operadores.Count > 0 && operadores.Peek() != "(")
                    {
                        salida.Add(operadores.Pop());
                    }
                    if (operadores.Count > 0) operadores.Pop();
                }
            }

            while (operadores.Count > 0)
            {
                salida.Add(operadores.Pop());
            }

            return salida;
        }

        private static bool EsNumero(string texto) => double.TryParse(texto, out _);
        private static bool EsIdentificadorValido(string texto) => !string.IsNullOrEmpty(texto) && char.IsLetter(texto[0]);
    }
}