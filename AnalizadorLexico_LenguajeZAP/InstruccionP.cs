using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class SwitchCaseNode
    {
        public string ValorCaso { get; set; } 
        public List<InstruccionP> Instrucciones { get; set; }
        public bool TieneBreak { get; set; }
    }

    public class InstruccionP
    {
        public string Operacion { get; set; } 
        public string Argumento1 { get; set; }
        public string Argumento2 { get; set; }

        public InstruccionP(string operacion, string argumento1 = "", string argumento2 = "")
        {
            Operacion = operacion;
            Argumento1 = argumento1;
            Argumento2 = argumento2;
        }

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

        // 1. TRADUCTOR DE EXPRESIONES (Aritméticas, Relacionales y Lógicas)
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

        // 2. ASIGNACIÓN
        public static List<InstruccionP> GenerarCodigoDinamico(string nombreVariable, List<string> expTokens)
        {
            List<InstruccionP> codigoLocal = GenerarCodigoExpresion(expTokens);
            codigoLocal.Add(new InstruccionP("STO", nombreVariable));
            return codigoLocal;
        }

        // 3. ESTRUCTURA IF (Simple)
        public static List<InstruccionP> GenerarCodigoIf(List<string> condTokens, List<InstruccionP> cuerpoIf)
        {
            List<InstruccionP> codigoIf = new List<InstruccionP>();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            codigoIf.AddRange(GenerarCodigoExpresion(condTokens));

            codigoIf.Add(new InstruccionP("FJMP", etiquetaFin));

            codigoIf.AddRange(cuerpoIf);

            codigoIf.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoIf;
        }

        // 4. ESTRUCTURA IF-ELSE
        public static List<InstruccionP> GenerarCodigoIfElse(List<string> condTokens, List<InstruccionP> cuerpoIf, List<InstruccionP> cuerpoElse)
        {
            List<InstruccionP> codigoIfElse = new List<InstruccionP>();
            string etiquetaElse = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            codigoIfElse.AddRange(GenerarCodigoExpresion(condTokens));

            codigoIfElse.Add(new InstruccionP("FJMP", etiquetaElse));

            codigoIfElse.AddRange(cuerpoIf);
            codigoIfElse.Add(new InstruccionP("JMP", etiquetaFin)); // Salta al final para evitar el else

            codigoIfElse.Add(new InstruccionP("LABEL", etiquetaElse));
            codigoIfElse.AddRange(cuerpoElse);

            codigoIfElse.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoIfElse;
        }

        // 5. CICLO WHILE
        public static List<InstruccionP> GenerarCodigoWhile(List<string> condTokens, List<InstruccionP> cuerpoWhile)
        {
            List<InstruccionP> codigoWhile = new List<InstruccionP>();
            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            codigoWhile.Add(new InstruccionP("LABEL", etiquetaInicio));

            codigoWhile.AddRange(GenerarCodigoExpresion(condTokens));

            codigoWhile.Add(new InstruccionP("FJMP", etiquetaFin));

            codigoWhile.AddRange(cuerpoWhile);

            codigoWhile.Add(new InstruccionP("JMP", etiquetaInicio));

            codigoWhile.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoWhile;
        }

        
        // 6. CICLO DO-WHILE
        
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

        
        // 7. CICLO FOR
        
        public static List<InstruccionP> GenerarCodigoFor(List<InstruccionP> initInsts, List<string> condTokens, List<InstruccionP> incInsts, List<InstruccionP> cuerpoFor)
        {
            List<InstruccionP> codigoFor = new List<InstruccionP>();
            string etiquetaInicio = ObtenerNuevaEtiqueta();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            codigoFor.AddRange(initInsts);

            codigoFor.Add(new InstruccionP("LABEL", etiquetaInicio));

            codigoFor.AddRange(GenerarCodigoExpresion(condTokens));
            codigoFor.Add(new InstruccionP("FJMP", etiquetaFin));

            codigoFor.AddRange(cuerpoFor);

            codigoFor.AddRange(incInsts);

            codigoFor.Add(new InstruccionP("JMP", etiquetaInicio));

            codigoFor.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoFor;
        }
        
        public static List<InstruccionP> GenerarCodigoSwitch(List<string> exprTokens, List<SwitchCaseNode> casos)
        {
            List<InstruccionP> codigoSwitch = new List<InstruccionP>();
            string etiquetaFin = ObtenerNuevaEtiqueta();

            var casosNormales = casos.Where(c => c.ValorCaso != null).ToList();
            var casoDefault = casos.FirstOrDefault(c => c.ValorCaso == null);

            List<string> etiquetasCuerpo = new List<string>();
            for (int i = 0; i < casosNormales.Count; i++)
            {
                etiquetasCuerpo.Add(ObtenerNuevaEtiqueta());
            }

            string etiquetaDefaultCuerpo = casoDefault != null ? ObtenerNuevaEtiqueta() : etiquetaFin;

            for (int i = 0; i < casosNormales.Count; i++)
            {
                codigoSwitch.AddRange(GenerarCodigoExpresion(exprTokens));
                codigoSwitch.Add(new InstruccionP("LIT", casosNormales[i].ValorCaso));
                codigoSwitch.Add(new InstruccionP("NE"));
                codigoSwitch.Add(new InstruccionP("FJMP", etiquetasCuerpo[i]));
            }

            if (casoDefault != null)
            {
                codigoSwitch.Add(new InstruccionP("JMP", etiquetaDefaultCuerpo));
            }
            else
            {
                codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
            }

            for (int i = 0; i < casosNormales.Count; i++)
            {
                codigoSwitch.Add(new InstruccionP("LABEL", etiquetasCuerpo[i]));
                codigoSwitch.AddRange(casosNormales[i].Instrucciones);

                if (casosNormales[i].TieneBreak)
                {
                    codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
                }
            }

            if (casoDefault != null)
            {
                codigoSwitch.Add(new InstruccionP("LABEL", etiquetaDefaultCuerpo));
                codigoSwitch.AddRange(casoDefault.Instrucciones);

                if (casoDefault.TieneBreak)
                {
                    codigoSwitch.Add(new InstruccionP("JMP", etiquetaFin));
                }
            }

            codigoSwitch.Add(new InstruccionP("LABEL", etiquetaFin));

            return codigoSwitch;
        }

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