using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using System.IO;

namespace AnalizadorLexico_LenguajeZAP
{
    public partial class Form1 : Form
    {
        private List<string> listaTokensExtraidos = new List<string>();
        private List<(string Token, string Lexema, int Linea)> listaTokensCompletos = new List<(string, string, int)>();
        private Dictionary<string, string> tablaTiposVariables = new Dictionary<string, string>();

        public class Simbolo
        {
            public int Id { get; set; }
            public string Lexema { get; set; }
            public string TipoDato { get; set; }     
            public string Scope { get; set; }        
            public int Direccion { get; set; }       
            public bool EstaInicializada { get; set; } 
            public object Valor { get; set; }        
        }

        private Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        private int contadorID = 1;

        private int offsetGlobal = 0;
        private int offsetLocal = 0;

        public void ConfigurarNumeracion()
        {
            rTxtCodigoFuente.TextChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.VScroll += ActualizarNumerosLinea;
            rTxtCodigoFuente.SelectionChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.Resize += ActualizarNumerosLinea;

            rTxtNumeros.Font = rTxtCodigoFuente.Font;
            rTxtNumeros.SelectionAlignment = HorizontalAlignment.Right;
            rTxtNumeros.ScrollBars = RichTextBoxScrollBars.None;

            rTxtTokens.TextChanged += ActualizarNumerosTokens;
            rTxtTokens.VScroll += ActualizarNumerosTokens;
            rTxtTokens.Resize += ActualizarNumerosTokens;

            rTxtNumerosTokens.Font = rTxtTokens.Font;
            rTxtNumerosTokens.ReadOnly = true;
            rTxtNumerosTokens.SelectionAlignment = HorizontalAlignment.Right;
            rTxtNumerosTokens.BackColor = Color.LightGray;
            rTxtNumerosTokens.ScrollBars = RichTextBoxScrollBars.None;

            ActualizarNumerosLinea(null, null);
        }

        private void ActualizarNumerosLinea(object sender, EventArgs e)
        {
            int totalLineas = rTxtCodigoFuente.Lines.Length;
            if (totalLineas == 0) totalLineas = 1;

            StringBuilder sb = new StringBuilder();
            for (int i = 1; i <= totalLineas; i++)
            {
                sb.AppendLine(i.ToString());
            }
            rTxtNumeros.Text = sb.ToString();
        }

        private void ActualizarNumerosTokens(object sender, EventArgs e)
        {
            Point pos = new Point(0, 0);
            int primerIndice = rTxtTokens.GetCharIndexFromPosition(pos);
            int primeraLinea = rTxtTokens.GetLineFromCharIndex(primerIndice);

            pos.X = rTxtTokens.ClientRectangle.Width;
            pos.Y = rTxtTokens.ClientRectangle.Height;
            int ultimoIndice = rTxtTokens.GetCharIndexFromPosition(pos);
            int ultimaLinea = rTxtTokens.GetLineFromCharIndex(ultimoIndice);

            StringBuilder sb = new StringBuilder();

            if (string.IsNullOrEmpty(rTxtTokens.Text))
            {
                sb.AppendLine("1");
            }
            else
            {
                for (int i = primeraLinea; i <= ultimaLinea; i++)
                {
                    sb.AppendLine((i + 1).ToString());
                }
            }

            rTxtNumerosTokens.Text = sb.ToString();
        }

        private void AplicarColorErrores(RichTextBox rtb)
        {
            int posicionOriginal = rtb.SelectionStart;
            string contenido = rtb.Text;
            string[] tokens = contenido.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int busquedaDesde = 0;
            foreach (string t in tokens)
            {
                if (t.StartsWith("ER"))
                {
                    int inicio = rtb.Find(t, busquedaDesde, RichTextBoxFinds.WholeWord);
                    if (inicio != -1)
                    {
                        rtb.Select(inicio, t.Length);
                        rtb.SelectionColor = Color.Red;
                        rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold);
                        busquedaDesde = inicio + t.Length;
                    }
                }
            }

            rtb.SelectionStart = posicionOriginal;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = Color.Black;
        }

        string connectionString = "Server=localhost; Database=ZAP; Integrated Security=True; TrustServerCertificate=True;";

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConfigurarNumeracion();
        }

        private void btnAnalizarCodigo_Click(object sender, EventArgs e)
        {
            dtgTablaSimbolos.Rows.Clear();
            tablaSimbolos.Clear();
            listaTokensCompletos.Clear();
            tablaTiposVariables.Clear();
            contadorID = 1;
            offsetGlobal = 0;
            offsetLocal = 0;

            rTxtTokens.Clear();
            rTxtErrores.Clear();
            int contadorGlobal = 0;
            DataTable matriz = ObtenerMatriz();

            rTxtErrores.SelectionFont = new Font(rTxtErrores.Font, FontStyle.Bold);
            rTxtErrores.AppendText("LÍNEA\tERROR" + Environment.NewLine);
            rTxtErrores.AppendText("----------------------------------------------------------" + Environment.NewLine);

            string[] lineas = rTxtCodigoFuente.Lines;

            for (int i = 0; i < lineas.Length; i++)
            {
                string lineaDeTokens = ProcesarLineaParaTokens(matriz, lineas[i], i + 1, rTxtErrores, ref contadorGlobal);
                rTxtTokens.AppendText(lineaDeTokens + Environment.NewLine);
            }
            AplicarColorErrores(rTxtTokens);

            rTxtErrores.AppendText(Environment.NewLine + "----------------------------------------------------------" + Environment.NewLine);
            rTxtErrores.SelectionFont = new Font(rTxtErrores.Font, FontStyle.Bold);
            rTxtErrores.AppendText("TOTAL DE ERRORES: " + contadorGlobal);

            if (contadorGlobal == 0)
            {
                bool resultadoSintactico = AnalizadorSintactico();
                if (resultadoSintactico)
                {
                    AnalizadorSemantico();
                }
            }
        }

        private void PegarTextoPlano()
        {
            if (Clipboard.ContainsText())
            {
                string texto = Clipboard.GetText();
                int inicio = rTxtCodigoFuente.SelectionStart;

                rTxtCodigoFuente.SelectedText = texto;
                rTxtCodigoFuente.Select(inicio, texto.Length);
                rTxtCodigoFuente.SelectionFont = new Font("Consolas", 10.2f);
                rTxtCodigoFuente.SelectionStart = inicio + texto.Length;
                rTxtCodigoFuente.SelectionLength = 0;
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V))
            {
                PegarTextoPlano();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public DataTable ObtenerMatriz()
        {
            DataTable tabla = new DataTable();
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM [Matriz$]";
                SqlDataAdapter adapter = new SqlDataAdapter(query, conn);
                adapter.Fill(tabla);
            }
            return tabla;
        }

        public string ValidarCadena(DataTable matriz, string cadenaEntrada)
        {
            int estadoActual = 1;
            int[] estadosError = { 276, 277, 278, 279, 280, 281, 282, 283, 284, 285 };

            foreach (char c in cadenaEntrada)
            {
                string nombreColumna = ObtenerNombreColumnaSQL(c);
                estadoActual = MoverSiguienteEstado(matriz, estadoActual, nombreColumna, estadosError);

                if (estadosError.Contains(estadoActual))
                {
                    return ObtenerTokenOError(matriz, estadoActual);
                }
            }

            estadoActual = MoverSiguienteEstado(matriz, estadoActual, "FDC", estadosError);
            return ObtenerTokenOError(matriz, estadoActual);
        }

        private int MoverSiguienteEstado(DataTable matriz, int estado, string columna, int[] errores)
        {
            DataRow[] filas = matriz.Select("F1 = " + estado);
            if (filas.Length > 0 && matriz.Columns.Contains(columna))
            {
                object valor = filas[0][columna];
                if (valor != DBNull.Value)
                {
                    return Convert.ToInt32(valor);
                }
            }
            return errores[0];
        }

        private string ObtenerTokenOError(DataTable matriz, int estado)
        {
            DataRow[] filas = matriz.Select("F1 = " + estado);
            if (filas.Length > 0 && filas[0]["ACEPTA"] != DBNull.Value)
            {
                return filas[0]["ACEPTA"].ToString();
            }
            return "CADENA_NO_VALIDA";
        }

        private string ObtenerNombreColumnaSQL(char c)
        {
            if (char.IsUpper(c)) return c.ToString() + "1";

            switch (c)
            {
                case '.': return "#1";
                case ',': return ".";
                case '[': return "(1";
                case ']': return ")1";
                case '!': return "_1";
                default: return c.ToString();
            }
        }

        private string ProcesarLineaParaTokens(DataTable matriz, string textoLinea, int numLinea, RichTextBox rtbErrores, ref int totalErrores)
        {
            if (string.IsNullOrWhiteSpace(textoLinea)) return "";

            StringBuilder sbLinea = new StringBuilder();
            string acumulador = "";
            string lineaConEspacio = textoLinea + " ";

            for (int i = 0; i < lineaConEspacio.Length; i++)
            {
                char c = lineaConEspacio[i];
                if (c == ',')
                {
                    if (acumulador.Length > 0)
                    {
                        string resPrevio = ValidarCadena(matriz, acumulador);
                        if (resPrevio.StartsWith("ER") || resPrevio == "CADENA_NO_VALIDA" || resPrevio == "IDEN")
                        {
                            if (acumulador.StartsWith("$")) resPrevio = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                            else if (char.IsDigit(acumulador[0])) resPrevio = "CONENTERO";
                        }

                        VerificarSiEsError(resPrevio, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resPrevio + " ");
                        acumulador = "";
                    }

                    string resComa = ValidarCadena(matriz, ",");
                    if (resComa.StartsWith("ER") || resComa == "CADENA_NO_VALIDA" || string.IsNullOrEmpty(resComa)) resComa = "CS13";

                    sbLinea.Append(resComa + " ");
                    continue;
                }
                else if (c == ' ' || c == '\t')
                {
                    if (acumulador.Length > 0)
                    {
                        string resultado = ValidarCadena(matriz, acumulador);
                        if (resultado == "IDEN") resultado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);

                        VerificarSiEsError(resultado, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resultado + " ");
                        acumulador = "";
                    }
                    sbLinea.Append(c);
                }
                else if (c == '"')
                {
                    string cadena = "";
                    i++;
                    bool seCerraronComillas = false;

                    while (i < lineaConEspacio.Length)
                    {
                        if (lineaConEspacio[i] == '"')
                        {
                            seCerraronComillas = true;
                            break;
                        }
                        cadena += lineaConEspacio[i]; 
                        i++;
                    }

                    string resCadena;
                    if (seCerraronComillas)
                    {
                        resCadena = ValidarCadena(matriz, "\"" + cadena + "\"");
                        if (resCadena.StartsWith("ER") || resCadena == "CADENA_NO_VALIDA" || string.IsNullOrEmpty(resCadena)) resCadena = "CAD";
                    }
                    else
                    {
                        resCadena = "ER02 \"Error: Cadena constante sin cerrar\"";
                    }

                    VerificarSiEsError(resCadena, "\"" + cadena + (seCerraronComillas ? "\"" : ""), numLinea, rtbErrores, ref totalErrores);
                    sbLinea.Append(resCadena + " ");
                    continue;
                }
                else if ("+-".Contains(c.ToString()))
                {
                    bool esExponencial = acumulador.Length > 0 && acumulador.ToUpper().EndsWith("E") && char.IsDigit(acumulador[0]);

                    if (esExponencial)
                    {
                        acumulador += c;
                    }
                    else
                    {
                        if (acumulador.Length > 0)
                        {
                            string resPrev = ValidarCadena(matriz, acumulador);
                            if (resPrev.StartsWith("ER") || resPrev == "CADENA_NO_VALIDA" || resPrev == "IDEN")
                            {
                                string intentoConEspacio = ValidarCadena(matriz, acumulador + " ");
                                if (!intentoConEspacio.StartsWith("ER") && intentoConEspacio != "CADENA_NO_VALIDA") resPrev = intentoConEspacio;
                                else if (acumulador.StartsWith("$")) resPrev = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                                else if (char.IsDigit(acumulador[0])) resPrev = "CONENTERO";
                            }

                            if (resPrev == "IDEN") resPrev = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);

                            VerificarSiEsError(resPrev, acumulador, numLinea, rtbErrores, ref totalErrores);
                            sbLinea.Append(resPrev + " ");
                            acumulador = "";
                        }

                        string lexemaOp = c.ToString();
                        if (i + 1 < lineaConEspacio.Length)
                        {
                            char sig = lineaConEspacio[i + 1];
                            if ((c == '+' && sig == '+') || (c == '-' && sig == '-'))
                            {
                                lexemaOp += sig;
                                i++;
                            }
                        }
                        string resOp = ValidarCadena(matriz, lexemaOp);
                        VerificarSiEsError(resOp, lexemaOp, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resOp + " ");
                    }
                }
                else if ("()[]{};*/!<>=:".Contains(c.ToString()))
                {
                    if (acumulador.Length > 0)
                    {
                        string resAcumulado = ValidarCadena(matriz, acumulador);
                        if (resAcumulado.StartsWith("ER") || resAcumulado == "CADENA_NO_VALIDA" || resAcumulado == "IDEN")
                        {
                            string intentoConEspacio = ValidarCadena(matriz, acumulador + " ");
                            if (!intentoConEspacio.StartsWith("ER") && intentoConEspacio != "CADENA_NO_VALIDA") resAcumulado = intentoConEspacio;
                            else if (acumulador.StartsWith("$")) resAcumulado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                            else if (char.IsDigit(acumulador[0])) resAcumulado = "CONENTERO";
                        }

                        if (resAcumulado == "IDEN") resAcumulado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);

                        VerificarSiEsError(resAcumulado, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resAcumulado + " ");
                        acumulador = "";
                    }

                    string lexemaEspecial = c.ToString();
                    if (i + 1 < lineaConEspacio.Length)
                    {
                        char sig = lineaConEspacio[i + 1];
                        if (c == '/' && sig == '/')
                        {
                            sbLinea.Append("COMEN ");
                            break;
                        }
                        if ((c == '+' && sig == '+') || (c == '-' && sig == '-'))
                        {
                            lexemaEspecial += sig; i++;
                        }
                        else if ((c == '&' && sig == '&') || (c == '=' && sig == '=') || (c == '|' && sig == '|') ||
                                 (c == '>' && sig == '=') || (c == '<' && sig == '=') || (c == '!' && sig == '='))
                        {
                            lexemaEspecial += sig; i++;
                        }
                    }
                    string resEspecial = ValidarCadena(matriz, lexemaEspecial);
                    if (lexemaEspecial == ":" && (resEspecial.StartsWith("ER") || resEspecial == "CADENA_NO_VALIDA")) resEspecial = "CS16";

                    VerificarSiEsError(resEspecial, lexemaEspecial, numLinea, rtbErrores, ref totalErrores);
                    sbLinea.Append(resEspecial + " ");
                }
                else
                {
                    acumulador += c;
                }
            }
            return sbLinea.ToString();
        }

        private void VerificarSiEsError(string resultado, string lexema, int linea, RichTextBox rtbErrores, ref int total)
        {
            bool esError = resultado.StartsWith("ER") || resultado.Equals("CADENA_NO_VALIDA");

            if (resultado == "CONENTERO" || resultado == "IDEN" || resultado.StartsWith("PR")) esError = false;

            if (!esError && !string.IsNullOrWhiteSpace(resultado))
            {
                listaTokensCompletos.Add((resultado.Trim(), lexema.Trim(), linea));
            }

            if (esError)
            {
                total++;
                string entrada = string.Format("{0}\t{1} (Lexema: '{2}'){3}", linea, resultado, lexema, Environment.NewLine);

                int inicio = rtbErrores.TextLength;
                rtbErrores.AppendText(entrada);
                rtbErrores.Select(inicio, entrada.Length);
                rtbErrores.SelectionColor = Color.Red;
                rtbErrores.DeselectAll();
                rtbErrores.SelectionColor = Color.Black;
            }
        }

        private string ObtenerTokenIdentificador(string lexema, DataGridView dgvSimbolos)
        {
            if (tablaSimbolos.ContainsKey(lexema))
            {
                return "IDEN" + tablaSimbolos[lexema].Id;
            }

            int nuevoID = contadorID++;
            Simbolo nuevoSimbolo = new Simbolo
            {
                Id = nuevoID,
                Lexema = lexema,
                TipoDato = "Desconocido",
                Scope = "Global",
                Direccion = 0,
                EstaInicializada = false,
                Valor = null
            };

            tablaSimbolos.Add(lexema, nuevoSimbolo);
            return "IDEN" + nuevoID;
        }

        private void btnGuardarArchivo_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Archivos ZAP (*.zap)|*.zap|Archivos de texto (*.txt)|*.txt";
            saveFileDialog.Title = "Guardar código fuente";
            saveFileDialog.DefaultExt = "zap";
            saveFileDialog.AddExtension = true;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, rTxtCodigoFuente.Text);
                    MessageBox.Show("Archivo guardado con éxito.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al guardar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnCargarPrograma_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Archivos ZAP (*.zap)|*.zap|Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            openFileDialog.Title = "Seleccionar código fuente ZAP";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    string contenido = File.ReadAllText(openFileDialog.FileName);
                    rTxtCodigoFuente.Text = contenido;
                    rTxtCodigoFuente.ReadOnly = true;
                    rTxtCodigoFuente.BackColor = SystemColors.ControlLight;
                    MessageBox.Show("Archivo cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnGuardarArchivoTokens_Click(object sender, EventArgs e)
        {
            if (rTxtTokens.Text.Contains("ER0") || rTxtTokens.Text.Contains("ER1"))
            {
                MessageBox.Show("No se puede guardar el archivo de tokens porque contiene errores.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog();
                saveFileDialog.Filter = "Archivos de Tokens ZAP (*.ztk)|*.ztk|Archivos de texto (*.txt)|*.txt";
                saveFileDialog.Title = "Guardar archivo de tokens";
                saveFileDialog.DefaultExt = "ztk";
                saveFileDialog.AddExtension = true;

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        File.WriteAllText(saveFileDialog.FileName, rTxtTokens.Text);
                        MessageBox.Show("Archivo guardado con éxito.", "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error al guardar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void btnEditarPrograma_Click(object sender, EventArgs e)
        {
            rTxtCodigoFuente.ReadOnly = false;
            rTxtCodigoFuente.BackColor = SystemColors.Window;
        }

        public bool AnalizadorSintactico()
        {
            listaTokensExtraidos.Clear();
            string contenidoActual = rTxtTokens.Text;
            string contenidoNormalizado = contenidoActual.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", " \n ");

            string[] palabras = contenidoNormalizado.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in palabras)
            {
                if (token == "\n") listaTokensExtraidos.Add("\n");
                else
                {
                    string tokenLimpio = token.Trim();
                    if (!string.IsNullOrEmpty(tokenLimpio)) listaTokensExtraidos.Add(tokenLimpio);
                }
            }

            ZapParsingResult resultado = PushdownParser.ExecuteParser(listaTokensExtraidos);
            rTxtErrores.Clear();

            if (resultado.Success)
            {
                rTxtErrores.SelectionColor = Color.Green;
                rTxtErrores.AppendText("¡Análisis sintáctico completado con éxito! Estructura válida.\n");
                MessageBox.Show("El código de tokens cumple perfectamente con la gramática ZAP.", "Análisis Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information);
                rTxtErrores.AppendText("\n=== INICIANDO ANÁLISIS SEMÁNTICO ===\n\n");
                return true;
            }
            else
            {
                rTxtErrores.SelectionColor = Color.Red;
                rTxtErrores.AppendText("=== ERRORES SINTÁCTICOS DETECTADOS ===\n\n");
                foreach (string error in resultado.Errors)
                {
                    rTxtErrores.SelectionColor = Color.DarkRed;
                    rTxtErrores.AppendText($"• {error}\n");
                }
                MessageBox.Show("Se encontraron fallas sintácticas en el orden de los tokens.", "Error de Sintaxis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private int ObtenerTamanoBytes(string tipoDato)
        {
            switch (tipoDato.ToLower())
            {
                case "int":
                case "pr19":
                case "float":
                case "double":
                case "pr11":
                case "string":
                case "pr28":
                    return 4;
                case "char":
                    return 2;
                case "boolean":
                case "bool":
                case "pr02":
                    return 1;
                default:
                    return 4;
            }
        }

        public void AnalizadorSemantico()
        {
            int erroresSemanticos = 0;
            int nivelBloque = 0;
            offsetGlobal = 0;
            offsetLocal = 0;

            string codigo = rTxtCodigoFuente.Text;
            string codigoLimpio = Regex.Replace(codigo, @"//.*", "");

            for (int i = 0; i < listaTokensCompletos.Count; i++)
            {
                var actual = listaTokensCompletos[i];

                if (actual.Lexema == "{")
                {
                    nivelBloque++;
                }
                else if (actual.Lexema == "}")
                {
                    if (nivelBloque > 0) nivelBloque--;
                }

                string lexemaMin = actual.Lexema.ToLower();
                bool esDeclaracion = actual.Token == "PR19" || actual.Token == "PR28" || actual.Token == "PR02" || actual.Token == "PR11" ||
                                     lexemaMin == "int" || lexemaMin == "float" || lexemaMin == "string" ||
                                     lexemaMin == "char" || lexemaMin == "boolean" || lexemaMin == "bool";

                if (esDeclaracion && i + 1 < listaTokensCompletos.Count)
                {
                    var siguiente = listaTokensCompletos[i + 1];

                    if (siguiente.Token.StartsWith("IDEN") || siguiente.Lexema.StartsWith("$"))
                    {
                        string idenLexema = siguiente.Lexema;
                        string tipoNormalizado = NormalizarTipo(actual.Lexema, actual.Token);

                        tablaTiposVariables[siguiente.Token] = tipoNormalizado;

                        if (tablaSimbolos.ContainsKey(idenLexema))
                        {
                            Simbolo sim = tablaSimbolos[idenLexema];
                            sim.TipoDato = tipoNormalizado;
                            sim.Scope = (nivelBloque == 0) ? "Global" : "Local";

                            int bytes = ObtenerTamanoBytes(tipoNormalizado);
                            if (sim.Scope == "Global")
                            {
                                sim.Direccion = offsetGlobal;
                                offsetGlobal += bytes;
                            }
                            else
                            {
                                sim.Direccion = offsetLocal;
                                offsetLocal += bytes;
                            }

                            if (i + 2 < listaTokensCompletos.Count && listaTokensCompletos[i + 2].Lexema == "=")
                            {
                                sim.EstaInicializada = true; 
                            }
                        }
                    }
                }

                if ((actual.Token.StartsWith("IDEN") || actual.Lexema.StartsWith("$")) && !esDeclaracion)
                {
                    if (i > 0 && listaTokensCompletos[i - 1].Lexema != "int" && listaTokensCompletos[i - 1].Lexema != "string" &&
                        listaTokensCompletos[i - 1].Lexema != "float" && listaTokensCompletos[i - 1].Lexema != "char" &&
                        listaTokensCompletos[i - 1].Lexema != "boolean" && !listaTokensCompletos[i - 1].Token.StartsWith("PR"))
                    {
                        if (tablaSimbolos.ContainsKey(actual.Lexema))
                        {
                            Simbolo sim = tablaSimbolos[actual.Lexema];

                            if (string.IsNullOrEmpty(sim.TipoDato) || sim.TipoDato == "Desconocido")
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error CS0103: El nombre '{actual.Lexema}' no existe en el contexto o ámbito actual.");
                                erroresSemanticos++;
                            }
                            else if (!sim.EstaInicializada && i + 1 < listaTokensCompletos.Count && listaTokensCompletos[i + 1].Lexema != "=")
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error Semántico: Uso de la variable '{actual.Lexema}' no inicializada.");
                                erroresSemanticos++;
                            }
                        }
                    }
                }

                if (actual.Token.StartsWith("IDEN") && i + 2 < listaTokensCompletos.Count)
                {
                    var operador = listaTokensCompletos[i + 1];
                    var valor = listaTokensCompletos[i + 2];

                    if (operador.Lexema == "=" || operador.Token == "OPASIG")
                    {
                        if (tablaSimbolos.ContainsKey(actual.Lexema))
                        {
                            Simbolo sim = tablaSimbolos[actual.Lexema];
                            sim.EstaInicializada = true;

                            string tipoVariable = sim.TipoDato;

                            if (valor.Token.StartsWith("IDEN") || valor.Lexema.StartsWith("$"))
                            {
                                if (tablaSimbolos.ContainsKey(valor.Lexema))
                                {
                                    string tipoAsignado = tablaSimbolos[valor.Lexema].TipoDato;
                                    if (!EsCompatible(tipoVariable, tipoAsignado))
                                    {
                                        ImprimirErrorSemantico(actual.Linea, $"Incompatibilidad de tipos. No se puede asignar tipo '{tipoAsignado}' a la variable '{actual.Lexema}' de tipo '{tipoVariable}'.");
                                        erroresSemanticos++;
                                    }
                                }
                            }
                            else
                            {
                                if ((tipoVariable == "int" || tipoVariable == "PR19") && valor.Token != "CONENTERO" && !int.TryParse(valor.Lexema, out _))
                                {
                                    ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: La variable '{actual.Lexema}' es de tipo 'int'. No se le puede asignar el valor '{valor.Lexema}'.");
                                    erroresSemanticos++;
                                }
                                else if ((tipoVariable == "string" || tipoVariable == "PR28") && valor.Token != "CAD" && !valor.Lexema.StartsWith("\""))
                                {
                                    ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: La variable '{actual.Lexema}' es de tipo 'string'. No se le puede asignar el valor '{valor.Lexema}'.");
                                    erroresSemanticos++;
                                }
                                else if (tipoVariable == "boolean" || tipoVariable == "bool" || tipoVariable == "PR02")
                                {
                                    if (valor.Lexema != "0" && valor.Lexema != "1" && valor.Lexema != "true" && valor.Lexema != "false")
                                    {
                                        ImprimirErrorSemantico(actual.Linea, $"Error Semántico: La variable booleana '{actual.Lexema}' solo admite valores booleanos (0, 1, true, false). Se encontró '{valor.Lexema}'.");
                                        erroresSemanticos++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            EvaluarValoresVariables();

            ActualizarDataGrid();

            if (erroresSemanticos > 0)
            {
                MessageBox.Show($"Se detectaron {erroresSemanticos} errores semánticos. Revisa el reporte de errores.", "Errores Semánticos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            else
            {
                rTxtErrores.SelectionColor = Color.Green;
                rTxtErrores.AppendText("¡Análisis semántico completado con éxito! Tipos, Scope y Direcciones de Memoria correctos.\n");
            }
        }

        private string NormalizarTipo(string lexema, string token)
        {
            if (token == "PR19" || lexema == "int") return "int";
            if (token == "PR28" || lexema == "string") return "string";
            if (token == "PR02" || lexema == "boolean" || lexema == "bool") return "boolean";
            if (token == "PR11" || lexema == "float" || lexema == "double") return "float";
            if (lexema == "char") return "char";
            return lexema;
        }

        private bool EsCompatible(string tipoDestino, string tipoOrigen)
        {
            if (tipoDestino == tipoOrigen) return true;
            if (tipoDestino == "float" && tipoOrigen == "int") return true; 
            return false;
        }

        public void EvaluarValoresVariables()
        {
            string codigo = rTxtCodigoFuente.Text;
            string codigoLimpio = Regex.Replace(codigo, @"//.*", "");

            string patronDeclaracionConValor = @"\b(int|double|float|string|char|bool|boolean)\b\s+(\$[a-zA-Z0-9_]+)\s*=\s*([^;]+);";
            string patronDeclaracionSinValor = @"\b(int|double|float|string|char|bool|boolean)\b\s+(\$[a-zA-Z0-9_]+)\s*;";
            string patronAsignacion = @"(\$[a-zA-Z0-9_]+)\s*=\s*([^;]+);";

            MatchCollection declaracionesConValor = Regex.Matches(codigoLimpio, patronDeclaracionConValor);
            foreach (Match m in declaracionesConValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;
                string valor = m.Groups[3].Value.Trim();

                if (tablaSimbolos.ContainsKey(iden))
                {
                    tablaSimbolos[iden].TipoDato = NormalizarTipo(tipo, "");
                    tablaSimbolos[iden].Valor = valor;
                    tablaSimbolos[iden].EstaInicializada = true;
                }
            }

            MatchCollection declaracionesSinValor = Regex.Matches(codigoLimpio, patronDeclaracionSinValor);
            foreach (Match m in declaracionesSinValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;

                if (tablaSimbolos.ContainsKey(iden))
                {
                    tablaSimbolos[iden].TipoDato = NormalizarTipo(tipo, "");
                    if (tablaSimbolos[iden].Valor == null)
                    {
                        tablaSimbolos[iden].Valor = "null";
                    }
                }
            }

            MatchCollection asignaciones = Regex.Matches(codigoLimpio, patronAsignacion);
            foreach (Match m in asignaciones)
            {
                string iden = m.Groups[1].Value;
                string expresionOriginal = m.Groups[2].Value.Trim();

                if (tablaSimbolos.ContainsKey(iden))
                {
                    string valorResuelto = EvaluarExpresion(expresionOriginal);
                    tablaSimbolos[iden].Valor = valorResuelto;
                    tablaSimbolos[iden].EstaInicializada = true;
                }
            }
        }

        private string EvaluarExpresion(string expresion)
        {
            if (string.IsNullOrWhiteSpace(expresion)) return "";

            bool esOperacionCadena = expresion.Contains("\"");
            MatchCollection variables = Regex.Matches(expresion, @"\$[a-zA-Z0-9_]+");

            foreach (Match varMatch in variables)
            {
                string nombreVar = varMatch.Value;

                if (tablaSimbolos.ContainsKey(nombreVar))
                {
                    var simbolo = tablaSimbolos[nombreVar];
                    string valorActual = simbolo.Valor?.ToString() ?? "";

                    if (simbolo.TipoDato == "string" || valorActual.StartsWith("\""))
                    {
                        esOperacionCadena = true;
                    }

                    if (valorActual == "null" || string.IsNullOrEmpty(valorActual))
                    {
                        valorActual = esOperacionCadena ? "\"\"" : "0";
                    }

                    expresion = expresion.Replace(nombreVar, valorActual);
                }
            }

            if (esOperacionCadena)
            {
                string[] partes = expresion.Split('+');
                string resultadoCadena = "";

                foreach (string parte in partes)
                {
                    string terminoLimpio = parte.Trim().Trim('"');
                    resultadoCadena += terminoLimpio;
                }

                return $"\"{resultadoCadena}\"";
            }

            try
            {
                DataTable dt = new DataTable();
                var resultado = dt.Compute(expresion, "");
                return resultado.ToString();
            }
            catch
            {
                return expresion;
            }
        }

        private void ActualizarDataGrid()
        {
            dtgTablaSimbolos.Rows.Clear();

        
            if (dtgTablaSimbolos.Columns.Count < 6)
            {
                dtgTablaSimbolos.Columns.Clear();
                dtgTablaSimbolos.Columns.Add("colId", "ID");
                dtgTablaSimbolos.Columns.Add("colLexema", "Lexema");
                dtgTablaSimbolos.Columns.Add("colTipo", "Tipo Dato");
                dtgTablaSimbolos.Columns.Add("colScope", "Scope");
                dtgTablaSimbolos.Columns.Add("colDireccion", "Dirección (Bytes)"); 
                dtgTablaSimbolos.Columns.Add("colValor", "Valor");
            }

            foreach (var item in tablaSimbolos.Values)
            {
                string respuesta=item.EstaInicializada? "Sí" : "No";

                dtgTablaSimbolos.Rows.Add(
                    item.Id,
                    item.Lexema,
                    item.TipoDato ?? "Desconocido",
                    item.Scope ?? "Global",
                    item.Direccion + " bytes", 
                    respuesta,
                    item.Valor ?? "null"
                );
            }
        }

        private void ImprimirErrorSemantico(int linea, string mensaje)
        {
            int inicio = rTxtErrores.TextLength;
            string textoError = $"• LÍNEA {linea}: {mensaje}\n";
            rTxtErrores.AppendText(textoError);
            rTxtErrores.Select(inicio, textoError.Length);
            rTxtErrores.SelectionColor = Color.DarkOrange;
            rTxtErrores.DeselectAll();
            rTxtErrores.SelectionColor = Color.Black;
        }
    }
}