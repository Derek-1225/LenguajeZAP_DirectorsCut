using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnalizadorLexico_LenguajeZAP
{
    public class PushdownParser
    {
        private static readonly Dictionary<string, Dictionary<string, List<string>>> ParsingTable =
            new Dictionary<string, Dictionary<string, List<string>>>(StringComparer.OrdinalIgnoreCase)
        {
            { "S", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR27", new List<string> { "PR27", "PR20", "BODY", "PR12", "PR20" } },
                { "ID",   new List<string> { "ID", "OPASIG", "EXP", "CS15" } },
                { "PR19", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PR11", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PR13", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PR02", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PR03", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PRO3", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },
                { "PR28", new List<string> { "TIPO", "ID", "OPASIG", "EXP", "CS15" } },

                { "PR18", new List<string> { "PR18", "OP-","OP>","ID", "CS15" } },
                { "PR22", new List<string> { "PR22", "ARG_PRINT", "CS15" } },
                { "PR16", new List<string> { "PR16", "CS20", "CONDIC", "CS21", "CS24", "BODY", "CS25", "ELSE_OPT" } },
                { "PR29", new List<string> { "PR29", "CS20", "ID", "CS21", "CS24", "SW", "SW2", "SW3" } },
                { "PR10", new List<string> { "PR10", "CS20", "CONDIC", "CS21", "CS24", "BODY", "CS25" } },
                { "PR09", new List<string> { "PR09", "CS24", "BODY", "CS25", "DW" } },
                { "PR14", new List<string> { "PR14", "CS20", "ID", "OPASIG", "ARGF", "CS13", "CONDIC", "CS13", "ID", "OPAF", "CS21", "CS24", "BODY", "CS25" } },
                { "PR15", new List<string> { "PR15", "TIPO", "ID", "CS20", "P", "CS21", "CS24", "B", "CS25" } },
                { "PR01", new List<string> { "PR01", "TIPO", "ID", "FIN_AR" } }          
            }},
            { "BODY", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR27", new List<string> { "S", "BODY" } },
                { "ID",   new List<string> { "S", "BODY" } },
                { "PR16", new List<string> { "S", "BODY" } },
                { "PR18", new List<string> { "S", "BODY" } },
                { "PR22", new List<string> { "S", "BODY" } },
                { "PR29", new List<string> { "S", "BODY" } },
                { "PR10", new List<string> { "S", "BODY" } },
                { "PR09", new List<string> { "S", "BODY" } },
                { "PR14", new List<string> { "S", "BODY" } },
                { "PR26", new List<string> { "S", "BODY" } },
                { "PR19", new List<string> { "S", "BODY" } },
                { "PR11", new List<string> { "S", "BODY" } },
                { "PR13", new List<string> { "S", "BODY" } },
                { "PR02", new List<string> { "S", "BODY" } },
                { "PR03", new List<string> { "S", "BODY" } },
                { "PR28", new List<string> { "S", "BODY" } },
                { "PR01", new List<string> { "S", "BODY" } },
                { "CS25", new List<string>() },
                { "PR12", new List<string>() },
                { "PR31", new List<string>() } 
            }},
            { "LLAMADA", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR26", new List<string> { "PR26", "ID", "CS20", "ARG_RUN", "CS21" } }
            }},
            { "ARG_RUN", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "VALOR", "RE_A" } },
                { "CN",   new List<string> { "VALOR", "RE_A" } },
                { "CAD",  new List<string> { "VALOR", "RE_A" } },
                { "CS21", new List<string>() }
            }},
            { "RE_A", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS13", new List<string> { "CS13", "VALOR", "RE_A" } },
                { "CS21", new List<string>() },
                { "CS15", new List<string>() }
            }},
            { "VALOR", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ID" } },
                { "CN",   new List<string> { "CN" } },
                { "CAD",  new List<string> { "CAD" } }
            }},
            { "ARG_PRINT", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CAD",  new List<string> { "CAD", "ARG_PRINT_REST" } },
                { "ID",   new List<string> { "ID", "ARG_PRINT_REST" } },
                { "CN",   new List<string> { "CN", "ARG_PRINT_REST" } },
                { "CS13", new List<string> { "CS13", "ARG_PRINT" } }
            }},
            { "ARG_PRINT_REST", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS13", new List<string> { "CS13", "ARG_PRINT" } },
                { "CS15", new List<string>() } // Epsilon
            }},
            { "ELSE_OPT", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR17", new List<string> { "PR17", "CS24", "BODY", "CS25" } },
   
                { "ID",    new List<string>() },
                { "PR22",  new List<string>() },
                { "PR16",  new List<string>() },
                { "PR18",  new List<string>() },
                { "PR10",  new List<string>() },
                { "PR14",  new List<string>() },
                { "PR29",  new List<string>() },
                { "PR19",  new List<string>() },
                { "PR11",  new List<string>() },
                { "PR13",  new List<string>() },
                { "PR02",  new List<string>() },
                { "PR03",  new List<string>() },
                { "PR28",  new List<string>() },
                { "CS25",  new List<string>() },
                { "PR12",  new List<string>() },
                { "$",     new List<string>() } 
            }},
            { "EXP", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID", new List<string> { "ARGC" } },
                { "CN", new List<string> { "ARGC" } },
                { "CS20", new List<string> { "OPA" } },
                { "CAD", new List<string> { "VALOR" } }
            }},
            { "CONDIC", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ARGC", "CONDIC_REST" } },
                { "CN",   new List<string> { "ARGC", "CONDIC_REST" } },
                { "CS20", new List<string> { "ARGC", "CONDIC_REST" } }
            }},
            { "CONDIC_REST", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OPOR",  new List<string> { "OL", "CONDIC" } },
                { "OPAND", new List<string> { "OL", "CONDIC" } },
                { "OPNOT", new List<string> { "OL", "CONDIC" } },
                
                { "OP>",   new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "OP>=",  new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "OP<",   new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "OP<=",  new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "OP==",  new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "OP!=",  new List<string> { "OPR_OPERATOR", "ARGC", "CONDIC_REST" } },
                { "CS21",  new List<string>() },
                { "CS13",  new List<string>() } 
            }},
            { "OL", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OPOR",  new List<string> { "OPOR" } },
                { "OPAND", new List<string> { "OPAND" } },
                { "OPNOT", new List<string> { "OPNOT" } }
            }},
            { "OPR", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ARGC", "OR", "ARGC" } },
                { "CN",   new List<string> { "ARGC", "OR", "ARGC" } }
            }},
            { "OR", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OP>",  new List<string> { "OP>" } },
                { "OP>=", new List<string> { "OP>=" } },
                { "OP<",  new List<string> { "OP<" } },
                { "OP<=", new List<string> { "OP<=" } },
                { "OP==", new List<string> { "OP==" } },
                { "OP!=", new List<string> { "OP!=" } }
            }},
            { "ARGC", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ID", "ARGC_REST" } },
                { "CN",   new List<string> { "CN", "ARGC_REST" } },
                { "CS20", new List<string> { "CS20", "OPA", "CS21", "ARGC_REST" } }
            }},
            { "ARGC_REST", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OP+",  new List<string> { "OA", "ARGO" } }, { "OP-",  new List<string> { "OA", "ARGO" } },
                { "OP*",  new List<string> { "OA", "ARGO" } }, { "OP/",  new List<string> { "OA", "ARGO" } },
                { "OP++", new List<string> { "OA", "ARGO" } }, { "OP--", new List<string> { "OA", "ARGO" } },
                { "OP^",  new List<string> { "OA", "ARGO" } },
                { "OP>",  new List<string>() }, { "OP>=", new List<string>() },
                { "OP<",  new List<string>() }, { "OP<=", new List<string>() },
                { "OP==", new List<string>() }, { "OP!=", new List<string>() },
                { "OPAND", new List<string>() }, { "OPOR", new List<string>() }, { "OPNOT", new List<string>() },
                { "CS21", new List<string>() }, { "CS13", new List<string>() },
                { "CS15", new List<string>() }
            }},
            { "OPA", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ARGO" } },
                { "CN",   new List<string> { "ARGO" } },
                { "CS20", new List<string> { "ARGO" } }
            }},
            { "OA", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OP+",  new List<string> { "OP+" } },  { "OP-",  new List<string> { "OP-" } },
                { "OP*",  new List<string> { "OP*" } },  { "OP/",  new List<string> { "OP/" } },
                { "OP++", new List<string> { "OP++" } }, { "OP--", new List<string> { "OP--" } },
                { "OP^",  new List<string> { "OP^" } }
            }},
            { "ARGO", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ID", "ARGO_REST" } },
                { "CN",   new List<string> { "CN", "ARGO_REST" } },
                { "CS20", new List<string> { "CS20", "OPA", "CS21", "ARGO_REST" } }
            }},
            { "ARGO_REST", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OP+",  new List<string> { "OA", "ARGO" } }, { "OP-",  new List<string> { "OA", "ARGO" } },
                { "OP*",  new List<string> { "OA", "ARGO" } }, { "OP/",  new List<string> { "OA", "ARGO" } },
                { "OP++", new List<string> { "OA", "ARGO" } }, { "OP--", new List<string> { "OA", "ARGO" } },
                { "OP^",  new List<string> { "OA", "ARGO" } },
    
                { "CS15", new List<string>() },
                { "OP>",  new List<string>() }, { "OP>=", new List<string>() },
                { "OP<",  new List<string>() }, { "OP<=", new List<string>() },
                { "OP==", new List<string>() }, { "OP!=", new List<string>() },
                { "CS21", new List<string>() }, { "CS13", new List<string>() }
            }},
            { "SW", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR30", new List<string> { "PR30", "ARGSW", "CS14", "BODY", "PR31", "CS15" } },
                { "PR32", new List<string>() },
                { "CS25", new List<string>() }
            }},
            { "SW2", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR32", new List<string> { "PR32", "CS14","BODY", "PR31", "CS15" } }
            }},
            { "SW3", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS25", new List<string> { "CS25" } }
            }},
            { "DW", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR10", new List<string> { "PR10", "CS20", "CONDIC", "CS21", "CS15" } }
            }},
            { "ARGF", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "ID" } },
                { "CN",   new List<string> { "CN" } }
            }},
            { "OPAF", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "OP++", new List<string> { "OP++" } },
                { "OP--", new List<string> { "OP--" } }
            }},
            { "VC", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "white",  new List<string> { "white" } },
                { "green",  new List<string> { "green" } },
                { "yellow", new List<string> { "yellow" } },
                { "red",    new List<string> { "red" } }
            }},
            { "TIPO", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR19", new List<string> { "PR19" } }, { "PR11", new List<string> { "PR11" } },
                { "PR13", new List<string> { "PR13" } }, { "PR02", new List<string> { "PR02" } },
                { "PR03", new List<string> { "PR03" } }, { "PR28", new List<string> { "PR28" } }
            }},
            { "P", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR19", new List<string> { "TIPO", "ID", "RE_P" } }, { "PR11", new List<string> { "TIPO", "ID", "RE_P" } },
                { "PR13", new List<string> { "TIPO", "ID", "RE_P" } }, { "PR02", new List<string> { "TIPO", "ID", "RE_P" } },
                { "PR03", new List<string> { "TIPO", "ID", "RE_P" } }, { "PR28", new List<string> { "TIPO", "ID", "RE_P" } },
                { "CS21", new List<string>() } 
            }},
            { "RE_P", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS13", new List<string> { "CS13", "TIPO", "ID", "RE_P" } },
                { "CS21", new List<string>() } 
            }},
            { "B", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "PR25", new List<string> { "PR25", "VALOR", "CS15" } },
                { "PR27", new List<string> { "BODY", "B" } }, { "ID",   new List<string> { "BODY", "B" } },
                { "PR16", new List<string> { "BODY", "B" } }, { "PR17", new List<string> { "BODY", "B" } },
                { "PR29", new List<string> { "BODY", "B" } }, { "PR10", new List<string> { "BODY", "B" } },
                { "PR09", new List<string> { "BODY", "B" } }, { "PR14", new List<string> { "BODY", "B" } },
                { "PR26", new List<string> { "BODY", "B" } }, { "PR15", new List<string> { "BODY", "B" } },
                { "PR01", new List<string> { "BODY", "B" } },
                { "CS25", new List<string>() }
            }},
            { "FIN_AR", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS15",   new List<string> { "CS15" } },
                { "OPASIG", new List<string> { "OPASIG", "CS22", "LISTA", "CS23", "CS15" } }
            }},
            { "LISTA", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "VALOR", "RE_L" } },
                { "CN",   new List<string> { "VALOR", "RE_L" } },
                { "CAD",  new List<string> { "VALOR", "RE_L" } }
            }},
            { "RE_L", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "CS13", new List<string> { "CS13", "VALOR", "RE_L" } },
                { "CS23", new List<string>() }
            }},
            { "ARGR", new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase) {
                { "ID",   new List<string> { "VALOR", "RE_A" } },
                { "CN",   new List<string> { "VALOR", "RE_A" } },
                { "CAD",  new List<string> { "VALOR", "RE_A" } },
                { "CS21", new List<string>() }
            }}
        };

        private static readonly Dictionary<string, List<string>> EmptyTable = new Dictionary<string, List<string>>();
        private static Dictionary<string, List<string>> GetOrEmpty(string top) => ParsingTable.TryGetValue(top, out var val) ? val : EmptyTable;

        public static ZapParsingResult ExecuteParser(List<string> rawTokens)
        {
            ZapParsingResult result = new ZapParsingResult();

            List<string> inputTokens = new List<string>(rawTokens);
            inputTokens.Add("$"); 
            if (inputTokens != null)
            {
                inputTokens = inputTokens
                    .Where(token => !token.Equals("COMEN", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (inputTokens == null || inputTokens.Count == 0)
            {
                result.Errors.Add("Error: La lista de tokens está vacía.");
                result.Success = false;
                return result;
            }

            Stack<string> stack = new Stack<string>();
            stack.Push("$"); 
            stack.Push("S"); 

            int index = 0;
            int lineNumber = 1;

            while (stack.Count > 0)
            {
                string top = stack.Peek();
                string currentInput = inputTokens[index];
                string lookahead = currentInput;

                if (lookahead.StartsWith("IDEN", StringComparison.OrdinalIgnoreCase))
                {
                    lookahead = "ID";
                }
                else if (lookahead.Equals("CONENTERO", StringComparison.OrdinalIgnoreCase) ||
                         lookahead.Equals("CONDEC", StringComparison.OrdinalIgnoreCase) ||
                         lookahead.Equals("CONEXP", StringComparison.OrdinalIgnoreCase))
                {
                    lookahead = "CN";
                }
                else if (lookahead.StartsWith("CAD", StringComparison.OrdinalIgnoreCase) ||
                         lookahead.StartsWith("\"", StringComparison.OrdinalIgnoreCase))
                {
                    lookahead = "CAD";
                }
                if (currentInput.Equals("\n") || currentInput.Equals("\r"))
                {
                    lineNumber++;
                    index++;
                    continue;
                }
                // --- Coincidencia con Simbolos Terminales---
                if (top.Equals(lookahead, StringComparison.OrdinalIgnoreCase))
                {
                    result.TraceSteps.Add($"[MATCH] Consumiendo componente terminal: '{currentInput}'");
                    stack.Pop();
                    index++;
                    continue;
                }
                if (top.Equals("S", StringComparison.OrdinalIgnoreCase) && lookahead.Equals("PR16", StringComparison.OrdinalIgnoreCase))
                {
                    string nextToken = (index + 1 < inputTokens.Count) ? inputTokens[index + 1] : "";
                    stack.Pop();
                    if (nextToken.Equals("OP-", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("CS15"); stack.Push("ID"); stack.Push("OP>"); stack.Push("OP-"); stack.Push("PR16");
                        result.TraceSteps.Add("[REGLA APLICADA] S -> PR18 OP- OP> ID CS15 (Input)");
                    }
                    else
                    {
                        stack.Push("ELSE_OPT"); stack.Push("CS25"); stack.Push("BODY"); stack.Push("CS24");
                        stack.Push("CS21"); stack.Push("CONDIC"); stack.Push("CS20"); stack.Push("PR16");
                        result.TraceSteps.Add("[REGLA APLICADA] S -> PR16 CS20 CONDIC... (If)");
                    }
                    continue;
                }
                if (top.Equals("S", StringComparison.OrdinalIgnoreCase) && lookahead.Equals("PR26", StringComparison.OrdinalIgnoreCase))
                {
                    string nextToken = (index + 1 < inputTokens.Count) ? inputTokens[index + 1] : "";
                    stack.Pop();
                    if (nextToken.Equals("PR05", StringComparison.OrdinalIgnoreCase) || nextToken.Equals("PRO5", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("CS15"); stack.Push("CS21"); stack.Push("CS20"); stack.Push("PR05"); stack.Push("PR26");
                        result.TraceSteps.Add("[REGLA APLICADA] S -> PR26 PR05 CS20 CS21 CS15 (Clear)");
                    }
                    else if (nextToken.Equals("PR06", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("CS15"); stack.Push("CS21"); stack.Push("CS16"); stack.Push("VC"); stack.Push("CS16");
                        stack.Push("CS20"); stack.Push("PR06"); stack.Push("PR26");
                        result.TraceSteps.Add("[REGLA APLICADA] S -> PR26 PR06 CS20 CS16 VC CS16 CS21 CS15 (Color)");
                    }
                    else
                    {
                        stack.Push("CS15"); stack.Push("CS21"); stack.Push("ARGR"); stack.Push("CS20"); stack.Push("ID"); stack.Push("PR26");
                        result.TraceSteps.Add("[REGLA APLICADA] S -> PR26 ID CS20 ARGR CS21 CS15 (Run)");
                    }
                    continue;
                }
                if (top.Equals("BODY", StringComparison.OrdinalIgnoreCase))
                {
                    string[] firstS = {
                        "PR27", "ID", "PR16", "PR17", "PR29", "PR10", "PR09", "PR14", "PR26", "PR15", "PR01",
                        "PR19", "PR11", "PR13", "PR02", "PR03", "PRO3", "PR28","PR22","PR18"
                    };

                    string[] followBody = { "PR12", "CS25", "PR31", "PR25", "PR17", "$" };

                    stack.Pop();
                    if (firstS.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("BODY");
                        stack.Push("S");
                        result.TraceSteps.Add("[REGLA APLICADA] BODY -> S BODY");
                    }
                    else if (followBody.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        result.TraceSteps.Add("[REGLA APLICADA] BODY -> ε");
                    }
                    else
                    {
                        result.Errors.Add($"Error en la línea {lineNumber}: No se esperaba encontrar {GetFriendlyTokenName(currentInput)} en este bloque de código.");
                        result.Success = false;
                        return result;
                    }
                    continue;
                }
                if (top.Equals("CONDIC_REST", StringComparison.OrdinalIgnoreCase))
                {
                    string[] relationalOps = { "OP>", "OP>=", "OP<", "OP<=", "OP==", "OP!=" };
                    string[] logicOps = { "OPAND", "OPOR", "OPNOT" };

                    stack.Pop();

                    if (relationalOps.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("CONDIC_REST");
                        stack.Push("ARGC");
                        stack.Push(lookahead); 
                        result.TraceSteps.Add($"[REGLA APLICADA] CONDIC_REST -> {lookahead} ARGC CONDIC_REST");
                    }
                    else if (logicOps.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("CONDIC");
                        stack.Push("OL");
                        result.TraceSteps.Add("[REGLA APLICADA] CONDIC_REST -> OL CONDIC");
                    }
                    else if (lookahead.Equals("CS21", StringComparison.OrdinalIgnoreCase) || lookahead.Equals("CS13", StringComparison.OrdinalIgnoreCase))
                    {
                        result.TraceSteps.Add("[REGLA APLICADA] CONDIC_REST -> ε (Condición Booleana Única)");
                    }
                    else
                    {
                        result.Errors.Add($"Error en la línea {lineNumber}: No se esperaba encontrar {GetFriendlyTokenName(currentInput)} en este bloque de código.");
                        result.Success = false;
                        return result;
                    }
                    continue;
                }
                

                if (top.Equals("OA", StringComparison.OrdinalIgnoreCase))
                {
                    string[] firstOA = { "OP+", "OP-", "OP*", "OP/", "OP++", "OP--", "OP^" };
                    string[] followOA = { "CS21", "CS15", "CS13", "OP>", "OP>=", "OP<", "OP<=", "OP==", "OP!=", "OPAND", "OPOR", "OPNOT" };

                    if (firstOA.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        
                    }
                    else if (followOA.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Pop();
                        result.TraceSteps.Add("[REGLA APLICADA] OA -> ε (Cierre Expresión)");
                        continue;
                    }
                }

                if (top.Equals("EXP", StringComparison.OrdinalIgnoreCase))
                {
                    stack.Pop();
                    if (lookahead.Equals("PR26", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("LLAMADA");
                        result.TraceSteps.Add("[REGLA APLICADA] EXP -> LLAMADA");
                    }
                    else if (lookahead.Equals("CAD", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("CAD");
                        result.TraceSteps.Add("[REGLA APLICADA] EXP -> CAD");
                    }
                    else if (lookahead.Equals("CS20", StringComparison.OrdinalIgnoreCase))
                    {
                        stack.Push("OPA");
                        result.TraceSteps.Add("[REGLA APLICADA] EXP -> OPA (Inicio con Paréntesis)");
                    }
                    else if (lookahead.Equals("ID", StringComparison.OrdinalIgnoreCase) || lookahead.Equals("CN", StringComparison.OrdinalIgnoreCase))
                    {
                        string nextToken = (index + 1 < inputTokens.Count) ? inputTokens[index + 1] : "";
                        string[] arithmeticOps = { "OP+", "OP-", "OP*", "OP/", "OP++", "OP--", "OP^" };

                        if (arithmeticOps.Contains(nextToken, StringComparer.OrdinalIgnoreCase))
                        {
                            stack.Push("OPA");
                            result.TraceSteps.Add("[REGLA APLICADA] EXP -> OPA");
                        }
                        else
                        {
                            stack.Push(lookahead);
                            result.TraceSteps.Add($"[REGLA APLICADA] EXP -> {lookahead}");
                        }
                    }
                    continue;
                }

                if (top.Equals("ARGO_REST", StringComparison.OrdinalIgnoreCase))
                {
                    string[] firstArgo = { "OP+", "OP-", "OP*", "OP/", "OP++", "OP--", "OP^" };
                    string[] followArgo = { "CS21", "CS15", "CS13", "OP>", "OP>=", "OP<", "OP<=", "OP==", "OP!=", "OPAND", "OPOR", "OPNOT" };

                    stack.Pop();
                    if (firstArgo.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("ARGO");
                        stack.Push("OA");
                        result.TraceSteps.Add("[REGLA APLICADA] ARGO_REST -> OA ARGO");
                    }
                    else if (followArgo.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        result.TraceSteps.Add("[REGLA APLICADA] ARGO_REST -> ε");
                    }
                    else
                    {
                        result.Errors.Add($"Error en la línea {lineNumber}: Estructura inválida. No se esperaba {GetFriendlyTokenName(currentInput)} después de la operación matemática.");
                        result.Success = false;
                        return result;
                    }
                    continue;
                }

                if (top.Equals("ARGC_REST", StringComparison.OrdinalIgnoreCase))
                {
                    string[] firstArgo = { "OP+", "OP-", "OP*", "OP/", "OP++", "OP--", "OP^" };
                    string[] followArgo = { "OP>", "OP>=", "OP<", "OP==", "OP<=", "OP!=", "OPAND", "OPOR", "OPNOT", "CS21", "CS13", "CS15" };

                    stack.Pop();
                    if (firstArgo.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("ARGO");
                        stack.Push("OA");
                    }
                    else if (followArgo.Contains(lookahead, StringComparer.OrdinalIgnoreCase))
                    {
                        result.TraceSteps.Add("[REGLA APLICADA] ARGC_REST -> ε");
                    }
                    else
                    {
                        result.Errors.Add($"Error en la línea {lineNumber}: Estructura inválida. No se esperaba {GetFriendlyTokenName(currentInput)} dentro de la condición.");
                        result.Success = false;
                        return result;
                    }
                    continue;
                }

                if (top.Equals("ARGSW", StringComparison.OrdinalIgnoreCase))
                {
                    stack.Pop();
                    string nextToken = (index + 1 < inputTokens.Count) ? inputTokens[index + 1] : "";
                    string[] relOps = { "OP>", "OP>=", "OP<", "OP<=", "OP==", "OP!=" };

                    if (relOps.Contains(nextToken, StringComparer.OrdinalIgnoreCase))
                    {
                        stack.Push("CONDIC");
                        result.TraceSteps.Add("[REGLA APLICADA] ARGSW -> CONDIC");
                    }
                    else
                    {
                        stack.Push(lookahead);
                        result.TraceSteps.Add($"[REGLA APLICADA] ARGSW -> {lookahead}");
                    }
                    continue;
                }

                // --- 3. EXPANSIÓN DE TABLA PREDICTIVA LL(1) ORDINARIA ---
                if (ParsingTable.ContainsKey(top))
                {
                    var topRow = GetOrEmpty(top);
                    if (topRow.ContainsKey(lookahead))
                    {
                        List<string> production = topRow[lookahead];
                        stack.Pop();

                        // Inserción inversa en pila
                        for (int i = production.Count - 1; i >= 0; i--)
                        {
                            stack.Push(production[i]);
                        }

                        string ruleStr = production.Count == 0 ? "ε" : string.Join(" ", production);
                        result.TraceSteps.Add($"[REGLA APLICADA] {top} -> {ruleStr}");
                    }
                    else
                    {
                        result.Errors.Add($"Error en la línea {lineNumber}: Se esperaba {GetFriendlyTokenName(top)}, pero se encontró {GetFriendlyTokenName(currentInput)}.");
                        result.Success = false;
                        return result;
                    }
                }
                else
                {
                    result.Errors.Add($"Error en la línea {lineNumber}: Se esperaba {GetFriendlyTokenName(top)}, pero el código tiene {GetFriendlyTokenName(currentInput)}.");
                    result.Success = false;
                    return result;
                }
            }

            // --- 4. Validación Final ---
            if (index >= inputTokens.Count - 1)
            {
                result.Success = true;
            }
            else
            {
                result.Errors.Add("Error de sintaxis: El programa parece haber terminado, pero sobró código extra al final del archivo que no debería estar ahí.");
                result.Success = false;
            }

            return result;
        }
        
        // Manejo de Errores
        private static string GetFriendlyTokenName(string token)
        {
            switch (token.ToUpper())
            {
                // Caracteres Especiales (CS)
                case "CS15": return "un punto y coma (;)";
                case "CS20": return "un paréntesis de apertura '('";
                case "CS21": return "un paréntesis de cierre ')'";
                case "CS24": return "una llave de apertura '{'";
                case "CS25": return "una llave de cierre '}'";
                case "CS13": return "una coma (,)";
                case "CS14": return "dos puntos (:)";
                case "CS22": return "un corchete de apertura '['";
                case "CS23": return "un corchete de cierre ']'";
                case "CS16": return "una comilla (\")";

                // Operadores
                case "OPASIG": return "un signo de asignación (=)";
                case "OP+": return "el operador aritmético suma (+)";
                case "OP-": return "el operador aritmético resta (-)";
                case "OP*": return "el operador aritmético multiplicación (*)";
                case "OP/": return "el operador aritmético división (/)";
                case "OP^": return "el operador aritmético potenciación (^)";
                case "OP++": return "el operador aritmético incremento (++)";
                case "OP--": return "el operador aritmético decremento (--)";
                case "OP<": return "el operador relacional menor que (<)";
                case "OP>": return "el operador relacional mayor que (>)";
                case "OP<=": return "el operador relacional menor o igual (<=)";
                case "OP>=": return "el operador relacional mayor o igual (>=)";
                case "OP==": return "el operador relacional de igualdad (==)";
                case "OP!=": return "el operador relacional diferente (!=)";
                case "OPAND": return "el operador lógico AND (&&)";
                case "OPOR": return "el operador lógico OR (||)";
                case "OPNOT": return "el operador lógico NOT (!)";

                // Palabras Reservadas (PR) de Estructura e I/O
                case "PR27": return "la palabra reservada 'start'";
                case "PR12": return "la palabra reservada 'end'";
                case "PR18": return "la palabra reservada 'input'";
                case "PR22": return "la palabra reservada 'print'";
                case "PR20": return "la palabra reservada 'main'";

                // Palabras Reservadas de Selección y Ciclos
                case "PR16": return "la palabra reservada 'if'";
                case "PR17": return "la palabra reservada 'else'";
                case "PR29": return "la palabra reservada 'switch'";
                case "PR30": return "la palabra reservada 'case'";
                case "PR31": return "la palabra reservada 'break'";
                case "PR32": return "la palabra reservada 'default'";
                case "PR10": return "la palabra reservada 'while'";
                case "PR09": return "la palabra reservada 'do'";
                case "PR14": return "la palabra reservada 'for'";

                // Tipos de Datos y Simples Adicionales
                case "PR19": return "el tipo de dato 'int'";
                case "PR11": return "el tipo de dato 'double'";
                case "PR28": return "el tipo de dato 'string'";
                case "PRO3": return "el tipo de dato 'char'";
                case "PRO2": return "el tipo de dato 'bool'";
                case "PR13": return "el tipo de dato 'float'";
                case "PR01": return "la palabra reservada 'array'";
                case "PR21": return "el valor 'null'";

                // Funciones
                case "PR15": return "la palabra reservada 'function'";
                case "PR35": return "la palabra reservada 'void'";
                case "PR25": return "la palabra reservada 'return'";
                case "PR07": return "la palabra reservada 'const'";

                // Gráficas
                case "PR05": return "la palabra reservada 'clear'";
                case "PR26": return "una instrucción de consola válida (ej. run)";
                case "VC": return "un color válido (white, green, yellow, red)";

                // Elementos Básicos (Identificadores y Valores)
                case "ID": return "un identificador válido (debe iniciar obligatoriamente con el símbolo $)";
                case "CN": return "una constante numérica";
                case "CAD": return "una cadena de texto (entre comillas dobles)";

                // Estructuras abstractas del parser para la pila
                case "S":
                case "BODY": return "una instrucción válida";
                case "CONDIC": return "una expresión de condición lógica";
                case "EXP": return "una expresión o un valor";
                case "TIPO": return "la declaración de un tipo de dato";
                case "LLAMADA": return "una llamada a función";
                case "ARG_RUN":
                case "ARGC": return "otro elemento para realizar la condicion (Identificador, Número)";
                case "ARG_PRINT": return "argumentos válidos para la instrucción";
                case "$": return "el final del archivo";

                default:
                    return $"'{token}'";
            }
        }
    }
}