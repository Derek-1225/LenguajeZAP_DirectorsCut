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
using System.Reflection;

namespace AnalizadorLexico_LenguajeZAP
{
    public partial class Form1 : Form
    {
        //Listas para almacenar los tokens extraídos y los tokens completos con su lexema y línea
        private List<string> listaTokensExtraidos = new List<string>();
        private List<(string Token, string Lexema, int Linea)> listaTokensCompletos = new List<(string, string, int)>();
        private Dictionary<string, string> tablaTiposVariables = new Dictionary<string, string>();
        List<(string Lexema, string Token)> expTokens = new List<(string, string)>();
        public List<InstruccionP> codigoIntermedio = new List<InstruccionP>();

        //Clase que ayudara a representar los elementos de la tabla de símbolos
        public class Simbolo
        {
            public int Id { get; set; }
            public string Lexema { get; set; }
            public string TipoDato { get; set; }     
            public string Scope { get; set; }        
            public int Tamano { get; set; }
            public int Direccion { get; set; }       
            public bool EstaInicializada { get; set; } 
            public object Valor { get; set; }        
        }

        //Aqui se crea la tabla de simbolos y se inicializa
        private Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        private int contadorID = 1;

        private int offsetGlobal = 0;
        private int offsetLocal = 0;

        //Metodo Auxiliar para mostrar el numero de linea en editor de codigo
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
            rTxtNumerosTokens.BackColor = Color.FromArgb(241, 245, 249);
            rTxtNumerosTokens.ForeColor = Color.FromArgb(100, 116, 139);
            rTxtNumerosTokens.ScrollBars = RichTextBoxScrollBars.None;

            rTxtNumeros.BackColor = Color.FromArgb(241, 245, 249);
            rTxtNumeros.ForeColor = Color.FromArgb(100, 116, 139);

            ActualizarNumerosLinea(null, null);
        }
        //Metodo Auxiliar para actualizar el numero de linea en editor de codigo
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
            if (lblStatusRight != null)
            {
                lblStatusRight.Text = $"Líneas: {totalLineas}  |  Caracteres: {rTxtCodigoFuente.TextLength}  |  Lenguaje ZAP  |  UTF-8";
            }
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

        //Metodo Auxiliar para aplicar color a los errores
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
            ConfigurarEstilosVisuales();
            InicializarColumnasGrids();
        }

        // Método para definir las columnas de ambos DataGridView
        private void InicializarColumnasGrids()
        {
            // 1. Configurar columnas para Cuádruplos (dtgCuadruplos)
            dtgCuadruplos.Columns.Clear();
            dtgCuadruplos.Columns.Add("Operador", "Operador");
            dtgCuadruplos.Columns.Add("Arg1", "Arg 1");
            dtgCuadruplos.Columns.Add("Arg2", "Arg 2");
            dtgCuadruplos.Columns.Add("Resultado", "Resultado");
            dtgCuadruplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // 2. Configurar columnas para Triplos (dtgTriplos)
            dtgTriplos.Columns.Clear();
            dtgTriplos.Columns.Add("Indice", "#");
            dtgTriplos.Columns.Add("Operador", "Operador");
            dtgTriplos.Columns.Add("Arg1", "Arg 1");
            dtgTriplos.Columns.Add("Arg2", "Arg 2");
            dtgTriplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        private void ConfigurarEstilosVisuales()
        {
            ConfigurarEstiloDataGridView();
        }

        private void ConfigurarEstiloDataGridView()
        {
            if (dtgTablaSimbolos.Columns.Count < 8)
            {
                dtgTablaSimbolos.Columns.Clear();
                dtgTablaSimbolos.Columns.Add("colId", "ID");
                dtgTablaSimbolos.Columns.Add("colLexema", "Lexema");
                dtgTablaSimbolos.Columns.Add("colTipo", "Tipo Dato");
                dtgTablaSimbolos.Columns.Add("colScope", "Scope");
                dtgTablaSimbolos.Columns.Add("colTamano", "Tamaño (Bytes)");
                dtgTablaSimbolos.Columns.Add("colDireccion", "Dirección");
                dtgTablaSimbolos.Columns.Add("colInit", "Inicializada");
                dtgTablaSimbolos.Columns.Add("colValor", "Valor");
            }

            dtgTablaSimbolos.EnableHeadersVisualStyles = false;
            dtgTablaSimbolos.BackgroundColor = Color.FromArgb(248, 250, 252);
            dtgTablaSimbolos.BorderStyle = BorderStyle.None;
            dtgTablaSimbolos.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dtgTablaSimbolos.GridColor = Color.FromArgb(226, 232, 240);
            
            dtgTablaSimbolos.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dtgTablaSimbolos.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(15, 23, 42);
            dtgTablaSimbolos.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dtgTablaSimbolos.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9.5F);
            dtgTablaSimbolos.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dtgTablaSimbolos.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 10, 0);
            dtgTablaSimbolos.ColumnHeadersHeight = 38;

            dtgTablaSimbolos.DefaultCellStyle.BackColor = Color.White;
            dtgTablaSimbolos.DefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dtgTablaSimbolos.DefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dtgTablaSimbolos.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);
            dtgTablaSimbolos.DefaultCellStyle.Font = new Font("Segoe UI", 9F);
            dtgTablaSimbolos.DefaultCellStyle.Padding = new Padding(10, 4, 10, 4);

            dtgTablaSimbolos.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dtgTablaSimbolos.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(15, 23, 42);
            dtgTablaSimbolos.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(219, 234, 254);
            dtgTablaSimbolos.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 64, 175);
            
            dtgTablaSimbolos.RowHeadersVisible = false;
            dtgTablaSimbolos.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dtgTablaSimbolos.RowTemplate.Height = 32;
        }

        private void panelTopBar_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(30, 41, 59), 1))
            {
                e.Graphics.DrawLine(pen, 0, panelTopBar.Height - 1, panelTopBar.Width, panelTopBar.Height - 1);
            }
        }

        private void panelBottomBar_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(30, 41, 59), 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, panelBottomBar.Width, 0);
            }
        }

        private void panelSidebar_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(Color.FromArgb(30, 41, 59), 1))
            {
                e.Graphics.DrawLine(pen, 0, 0, 0, panelSidebar.Height);
            }
        }


        //Evento del boton para analizar el codigo fuente
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

            if (contadorGlobal > 0)
            {
                lblStatusLeft.Text = $"● {contadorGlobal} error(es) léxico(s) detectado(s)";
                lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
            }
            else
            {
                //Si no hay errores léxicos, se procede al análisis sintáctico
                bool resultadoSintactico = AnalizadorSintactico();
                //Si el analizador sintáctico es exitoso, se procede al análisis semántico
                if (resultadoSintactico)
                {
                    bool resultadoSemantico = AnalizadorSemantico();
                    if (resultadoSemantico)
                    {


                        GenerarTodoDinamico();
                        lblStatusLeft.Text = "● Compilación exitosa: Estructura válida y Código P generado";
                        lblStatusLeft.ForeColor = Color.FromArgb(52, 211, 153);
                    }
                    else
                    {
                        ActualizarDataGrid();
                        rtCodigoIntermedio.Clear();
                        lblStatusLeft.Text = "● Errores semánticos detectados";
                        lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                }
                else
                {
                    lblStatusLeft.Text = "● Errores sintácticos detectados";
                    lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
                }
            }
        }
        //Metodo auxiliar para permitir pegar texto en el eitor de codigo sin que se altere la fuente y el tamaño de letra
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

        //Metodo auxiliar que permite pegar codigo con ctrl+v y compilar con F5
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.V))
            {
                PegarTextoPlano();
                return true;
            }
            if (keyData == Keys.F5)
            {
                btnAnalizarCodigo_Click(btnAnalizarCodigo, EventArgs.Empty);
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        //Metodo con el que se obtiene la matriz de transiciones desde la base de datos
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
        
        //Metodo que valida una cadena de entrada utilizando la matriz de transiciones
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

        //Metodo que cambia de estado en la matriz
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

        //Metodo que maneja errores lexico
        private string ObtenerTokenOError(DataTable matriz, int estado)
        {
            DataRow[] filas = matriz.Select("F1 = " + estado);
            if (filas.Length > 0 && filas[0]["ACEPTA"] != DBNull.Value)
            {
                return filas[0]["ACEPTA"].ToString();
            }
            return "CADENA_NO_VALIDA";
        }

        //Metodo auxiliar para obtener la columna correspondiente en la matriz de transiciones según el caracter de entrada
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

        //Metodo auxiliar que procesa una línea de código fuente para extraer los tokens y manejar errores léxicos
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

        //Metodo que verifica si hay errores
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

        //Metodo con el que se obtiene el token de un elemento del codigo
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

        //Evento con el que se guardan los archivos del programa fuente en formato .zap
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

        //Metodo auxiliar que permite cargar archivos .zap desde el explorador de archivos
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

        //Metodo auxiliar que guarda los archivos de tokens
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

        //Metodo auxiliar que permite editar el codigo fuente en el editor de codigo
        private void btnEditarPrograma_Click(object sender, EventArgs e)
        {
            rTxtCodigoFuente.ReadOnly = false;
            rTxtCodigoFuente.BackColor = SystemColors.Window;
        }


        //Metodo que ejecuta el analisis sintactico
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

            //Invocamos ZapParsingResult para ejecutar el analizador sintáctico (PushdownParser) - Pila sintactica
            ZapParsingResult resultado = PushdownParser.ExecuteParser(listaTokensExtraidos);
            rTxtErrores.Clear();

            if (resultado.Success)
            {
                rTxtErrores.SelectionColor = Color.Green;
                rTxtErrores.AppendText("¡Análisis sintáctico completado con éxito! Estructura válida.\n");
                //MessageBox.Show("El código de tokens cumple perfectamente con la gramática ZAP.", "Análisis Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                //MessageBox.Show("Se encontraron fallas sintácticas en el orden de los tokens.", "Error de Sintaxis", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
        
        //Metodo auxiliar que obtiene el tamaño en bytes segun el tipo de dato
        private int ObtenerTamanoBytes(string tipoDato)
        {
            if (string.IsNullOrEmpty(tipoDato)) return 0;

            switch (tipoDato.ToLower())
            {
                case "int":
                case "pr19":
                    return 4;

                case "float":
                    return 4;

                case "double":
                    return 8;

                case "char":
                    return 1;

                case "boolean":
                case "bool":
                case "pr02":
                    return 1;

                case "string":
                    return 256;

                default:
                    return 0;
            }
        }

        //Metodo que ejecuta el analisis semantico
        public bool AnalizadorSemantico()
        {
            int erroresSemanticos = 0;
            int nivelBloque = 0;
            offsetGlobal = 0;
            offsetLocal = 0;

            Stack<HashSet<string>> ambitos = new Stack<HashSet<string>>();
            ambitos.Push(new HashSet<string>());

            string codigo = rTxtCodigoFuente.Text;
            string codigoLimpio = Regex.Replace(codigo, @"//.*", "");

            EvaluarValoresVariables();

            for (int i = 0; i < listaTokensCompletos.Count; i++)
            {
                var actual = listaTokensCompletos[i];

                if (actual.Lexema == "{")
                {
                    nivelBloque++;
                    ambitos.Push(new HashSet<string>());
                }
                else if (actual.Lexema == "}")
                {
                    if (nivelBloque > 0) nivelBloque--;
                    if (ambitos.Count > 1) ambitos.Pop();
                }

                string lexemaMin = actual.Lexema.ToLower();
                bool esDeclaracion = actual.Token == "PR19" || actual.Token == "PR28" || actual.Token == "PR02" || actual.Token == "PR11" ||
                                     lexemaMin == "int" || lexemaMin == "float" || lexemaMin == "string" ||
                                     lexemaMin == "char" || lexemaMin == "bool";

                bool esVariableEnDeclaracion = false;
                if (i > 0)
                {
                    var anterior = listaTokensCompletos[i - 1];
                    string antMin = anterior.Lexema.ToLower();
                    if (anterior.Token == "PR19" || anterior.Token == "PR28" || anterior.Token == "PR02" || anterior.Token == "PR11" ||
                        antMin == "int" || antMin == "float" || antMin == "string" || antMin == "char" || antMin == "bool")
                    {
                        esVariableEnDeclaracion = true;
                    }
                }

                if (esDeclaracion && i + 1 < listaTokensCompletos.Count)
                {
                    var siguiente = listaTokensCompletos[i + 1];

                    if (siguiente.Token.StartsWith("IDEN") || siguiente.Lexema.StartsWith("$"))
                    {
                        string idenLexema = siguiente.Lexema;
                        string tipoNormalizado = NormalizarTipo(actual.Lexema, actual.Token);

                        tablaTiposVariables[siguiente.Token] = tipoNormalizado;
                        ambitos.Peek().Add(idenLexema);

                        if (tablaSimbolos.ContainsKey(idenLexema))
                        {
                            Simbolo sim = tablaSimbolos[idenLexema];
                            sim.TipoDato = tipoNormalizado;
                            sim.Scope = (nivelBloque == 0) ? "Global" : "Local";

                            int bytes = ObtenerTamanoBytes(tipoNormalizado);
                            sim.Tamano = bytes;
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

                            sim.EstaInicializada = true; 
                        }
                    }
                }

                if ((actual.Token.StartsWith("IDEN") || actual.Lexema.StartsWith("$")) && !esDeclaracion && !esVariableEnDeclaracion)
                {
                    bool estaEnAlcance = false;
                    foreach (var scope in ambitos)
                    {
                        if (scope.Contains(actual.Lexema))
                        {
                            estaEnAlcance = true;
                            break;
                        }
                    }

                    if (!estaEnAlcance)
                    {
                        ImprimirErrorSemantico(actual.Linea, $"Error de Alcance: La variable '{actual.Lexema}' no existe en el contexto o bloque actual.");
                        erroresSemanticos++;
                    }
                }

                if (actual.Lexema == "+" || actual.Token == "OP+" || actual.Token == "OPSUM")
                {
                    if (i > 0 && i + 1 < listaTokensCompletos.Count)
                    {
                        var left = listaTokensCompletos[i - 1];
                        var right = listaTokensCompletos[i + 1];

                        string tipoLeft = ObtenerTipoOperando(left.Lexema, left.Token);
                        string tipoRight = ObtenerTipoOperando(right.Lexema, right.Token);

                        // Bloquear mezclas directas entre cadenas y números/booleanos
                        if ((tipoLeft == "string" && tipoRight != "string") || (tipoLeft != "string" && tipoRight == "string"))
                        {
                            ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: No se puede sumar/concatenar una cadena ('{left.Lexema}') con un tipo numérico/booleano ('{right.Lexema}').");
                            erroresSemanticos++;
                        }
                    }
                }


                if ((actual.Lexema == "=" || actual.Token == "OPASIG") && i > 0)
                {
                    var varAsignada = listaTokensCompletos[i - 1];

                    if (tablaSimbolos.ContainsKey(varAsignada.Lexema))
                    {
                        Simbolo sim = tablaSimbolos[varAsignada.Lexema];
                        sim.EstaInicializada = true;

                        string tipoVariable = sim.TipoDato;

                        List<(string Lexema, string Token)> expTokens = new List<(string, string)>();
                        for (int j = i + 1; j < listaTokensCompletos.Count; j++)
                        {
                            if (listaTokensCompletos[j].Lexema == ";" || listaTokensCompletos[j].Lexema == ",") break;
                            expTokens.Add((listaTokensCompletos[j].Lexema, listaTokensCompletos[j].Token));
                        }


                        // =========================================================================
                        // 1. VALIDACIÓN PARA ENTEROS (int / PR19)
                        // =========================================================================
                        if (tipoVariable == "int" || tipoVariable == "PR19")
                        {
                            bool intInvalido = false;
                            string causaInvalida = "";

                            foreach (var t in expTokens)
                            {
                                // A) Rechazar decimales literales
                                if (t.Token == "CONDEC" || t.Lexema.Contains("."))
                                {
                                    intInvalido = true;
                                    causaInvalida = "valores decimales";
                                }
                                // B) Rechazar cadenas de texto literales
                                else if (t.Token == "CAD" || t.Token.StartsWith("CAD"))
                                {
                                    intInvalido = true;
                                    causaInvalida = "cadenas de texto";
                                }
                                // C) Validar variables ($): sólo acepta variables registradas como 'int'
                                else if (t.Token == "ID" || t.Token.StartsWith("ID") || t.Lexema.StartsWith("$"))
                                {
                                    string tipoReg = ObtenerTipoVariable(t.Lexema);
                                    if (tipoReg != "int" && tipoReg != "pr19")
                                    {
                                        intInvalido = true;
                                        string tipoMostrar = string.IsNullOrEmpty(tipoReg) ? "desconocido" : tipoReg;
                                        causaInvalida = $"la variable '{t.Lexema}' de tipo '{tipoMostrar}'";
                                    }
                                }
                                // D) Control de Desbordamiento Numérico en enteros constantes
                                else if (t.Token == "CONENTERO")
                                {
                                    if (!ValidarRangoNumerico(t.Lexema, "int", out string mensajeError))
                                    {
                                        ImprimirErrorSemantico(actual.Linea, mensajeError);
                                        erroresSemanticos++;
                                    }
                                }
                            }

                            if (intInvalido)
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: La variable '{varAsignada.Lexema}' (int) no admite asignaciones ni operaciones con {causaInvalida}.");
                                erroresSemanticos++;
                            }
                        }

                        // =========================================================================
                        // 2. VALIDACIÓN PARA FLOTANTES (float / double / PR11, PR13)
                        // =========================================================================
                        else if (tipoVariable == "float" || tipoVariable == "double" || tipoVariable == "PR11" || tipoVariable == "PR13")
                        {
                            bool floatInvalido = false;
                            string causaInvalida = "";

                            foreach (var t in expTokens)
                            {
                                // A) Rechazar cadenas de texto
                                if (t.Token == "CAD" || t.Token.StartsWith("CAD"))
                                {
                                    floatInvalido = true;
                                    causaInvalida = "cadenas de texto";
                                }
                                // B) Validar variables ($): acepta sólo variables de tipo flotante o entero (si permites int -> float)
                                else if (t.Token == "ID" || t.Token.StartsWith("ID") || t.Lexema.StartsWith("$"))
                                {
                                    string tipoReg = ObtenerTipoVariable(t.Lexema);
                                    if (tipoReg != "float" && tipoReg != "double" && tipoReg != "pr11" && tipoReg != "pr13" && tipoReg != "int" && tipoReg != "pr19")
                                    {
                                        floatInvalido = true;
                                        string tipoMostrar = string.IsNullOrEmpty(tipoReg) ? "desconocido" : tipoReg;
                                        causaInvalida = $"la variable '{t.Lexema}' de tipo '{tipoMostrar}'";
                                    }
                                }
                                // C) Control de Desbordamiento Numérico en decimales o enteros constantes
                                else if (t.Token == "CONENTERO" || t.Token == "CONDEC")
                                {
                                    if (!ValidarRangoNumerico(t.Lexema, tipoVariable, out string mensajeError))
                                    {
                                        ImprimirErrorSemantico(actual.Linea, mensajeError);
                                        erroresSemanticos++;
                                    }
                                }
                            }

                            if (floatInvalido)
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: La variable '{varAsignada.Lexema}' ({tipoVariable}) no admite asignaciones ni operaciones con {causaInvalida}.");
                                erroresSemanticos++;
                            }
                        }

                        // =========================================================================
                        // 3. VALIDACIÓN PARA BOOLEANOS (bool / boolean / PR02)
                        // =========================================================================
                        else if (tipoVariable == "boolean" || tipoVariable == "bool" || tipoVariable == "PR02")
                        {
                            bool boolInvalido = false;
                            string causaInvalida = "";

                            foreach (var t in expTokens)
                            {
                                // A) Exclusividad de constantes numéricas: sólo 0 y 1
                                if (t.Token == "CONENTERO")
                                {
                                    if (t.Lexema != "0" && t.Lexema != "1")
                                    {
                                        boolInvalido = true;
                                        causaInvalida = $"el entero '{t.Lexema}' (un booleano solo admite 0 y 1)";
                                    }
                                }
                                // B) Rechazar decimales
                                else if (t.Token == "CONDEC" || t.Lexema.Contains("."))
                                {
                                    boolInvalido = true;
                                    causaInvalida = "valores decimales";
                                }
                                // C) Rechazar cadenas de texto
                                else if (t.Token == "CAD" || t.Token.StartsWith("CAD"))
                                {
                                    boolInvalido = true;
                                    causaInvalida = $"la cadena \"{t.Lexema}\"";
                                }
                                // D) Validar variables ($): sólo acepta variables registradas como 'bool'
                                else if (t.Token == "ID" || t.Token.StartsWith("ID") || t.Lexema.StartsWith("$"))
                                {
                                    string tipoReg = ObtenerTipoVariable(t.Lexema);
                                    if (tipoReg != "bool" && tipoReg != "boolean" && tipoReg != "pr02")
                                    {
                                        boolInvalido = true;
                                        string tipoMostrar = string.IsNullOrEmpty(tipoReg) ? "desconocido" : tipoReg;
                                        causaInvalida = $"la variable '{t.Lexema}' de tipo '{tipoMostrar}'";
                                    }
                                }
                            }

                            if (boolInvalido)
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error Semántico: La variable '{varAsignada.Lexema}' (bool) no admite {causaInvalida}.");
                                erroresSemanticos++;
                            }
                        }

                        // =========================================================================
                        // 4. VALIDACIÓN PARA CADENAS (string / PR28)
                        // =========================================================================
                        else if (tipoVariable == "string" || tipoVariable == "PR28")
                        {
                            bool stringInvalido = false;
                            string causaInvalida = "";

                            foreach (var t in expTokens)
                            {
                                // A) Rechazar literales numéricos (enteros o decimales)
                                if (t.Token == "CONENTERO" || t.Token == "CONDEC")
                                {
                                    stringInvalido = true;
                                    causaInvalida = $"el valor numérico '{t.Lexema}'";
                                }
                                // B) Validar variables ($): sólo acepta variables registradas como 'string'
                                else if (t.Token == "ID" || t.Token.StartsWith("ID") || t.Lexema.StartsWith("$"))
                                {
                                    string tipoReg = ObtenerTipoVariable(t.Lexema);
                                    if (tipoReg != "string" && tipoReg != "pr28")
                                    {
                                        stringInvalido = true;
                                        string tipoMostrar = string.IsNullOrEmpty(tipoReg) ? "desconocido" : tipoReg;
                                        causaInvalida = $"la variable '{t.Lexema}' de tipo '{tipoMostrar}'";
                                    }
                                }
                            }

                            if (stringInvalido)
                            {
                                ImprimirErrorSemantico(actual.Linea, $"Error de Tipo: La variable '{varAsignada.Lexema}' (string) no admite operaciones ni asignaciones con {causaInvalida}.");
                                erroresSemanticos++;
                            }
                        }
                    }
                }
            }   


            if (erroresSemanticos > 0)
            {
                //MessageBox.Show($"Se detectaron {erroresSemanticos} errores semánticos. Revisa el reporte de errores.", "Errores Semánticos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                ActualizarDataGrid();
                return false;
            }
            else
            {
                rTxtErrores.SelectionColor = Color.Green;
                rTxtErrores.AppendText("¡Análisis semántico completado con éxito! Tipos, Scope y Direcciones de Memoria correctos.\n");
                ActualizarDataGrid();
                return true;
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

        public void EvaluarValoresVariables()
        {
            string codigo = rTxtCodigoFuente.Text;
            string codigoLimpio = Regex.Replace(codigo, @"//.*", "");
            // Modificamos las RegEx para ignorar el '==' de las condiciones y detenerse tanto en ';' como en ','
            string patronDeclaracionConValor = @"\b(int|double|float|string|char|bool|boolean)\b\s+(\$[a-zA-Z0-9_]+)\s*=(?!=)\s*(.*?)\s*(?:;|\s,\s)";
            string patronDeclaracionSinValor = @"\b(int|double|float|string|char|bool|boolean)\b\s+(\$[a-zA-Z0-9_]+)\s*(?:;|\s,\s)";
            string patronAsignacion = @"(\$[a-zA-Z0-9_]+)\s*=(?!=)\s*(.*?)\s*(?:;|\s,\s)";

            MatchCollection declaracionesConValor = Regex.Matches(codigoLimpio, patronDeclaracionConValor);
            foreach (Match m in declaracionesConValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;
                string valor = m.Groups[3].Value.Trim();

                if (tablaSimbolos.ContainsKey(iden))
                {
                    string tipoNormal = NormalizarTipo(tipo, "");
                    tablaSimbolos[iden].TipoDato = tipoNormal;
                    tablaSimbolos[iden].Valor = EvaluarExpresion(valor, tipoNormal); 
                }
            }

            MatchCollection declaracionesSinValor = Regex.Matches(codigoLimpio, patronDeclaracionSinValor);
            foreach (Match m in declaracionesSinValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;

                if (tablaSimbolos.ContainsKey(iden))
                {
                    string tipoNormal = NormalizarTipo(tipo, "");
                    tablaSimbolos[iden].TipoDato = tipoNormal;
                    if (tablaSimbolos[iden].Valor == null || tablaSimbolos[iden].Valor.ToString() == "null")
                    {
                        switch (tipoNormal)
                        {
                            case "int":
                            case "PR19": // Si usas tokens para los tipos
                                tablaSimbolos[iden].Valor = "0";
                                break;
                            case "float":
                            case "double":
                                tablaSimbolos[iden].Valor = "0.0";
                                break;
                            case "string":
                                tablaSimbolos[iden].Valor = "\"\"";
                                break;
                            case "char":
                                tablaSimbolos[iden].Valor = "''";
                                break;
                            case "boolean":
                            case "bool":
                            case "PR02":
                                tablaSimbolos[iden].Valor = "0";
                                break;
                            default:
                                tablaSimbolos[iden].Valor = "0";
                                break;
                        }
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
                    string tipoVar = tablaSimbolos[iden].TipoDato;
                    string valorResuelto = EvaluarExpresion(expresionOriginal, tipoVar);
                    tablaSimbolos[iden].Valor = valorResuelto;
                }
            }
        }

        private string EvaluarExpresion(string expresion, string tipoDato = "")
        {
            if (string.IsNullOrWhiteSpace(expresion)) return "";

            bool esOperacionCadena = expresion.Contains("\"") || tipoDato == "string" || tipoDato == "PR28";

            expresion = Regex.Replace(expresion, @"\$[a-zA-Z0-9_]+", match =>
            {
                string nombreVar = match.Value;
                if (tablaSimbolos.ContainsKey(nombreVar))
                {
                    string val = tablaSimbolos[nombreVar].Valor?.ToString() ?? "";
                    if (string.IsNullOrEmpty(val) || val == "null")
                        return (tipoDato == "string") ? "\"\"" : "0";
                    return val;
                }
                return match.Value;
            });

            if (esOperacionCadena)
            {
                try
                {
                    if (expresion.Contains("+"))
                    {
                        string[] partes = expresion.Split('+');
                        StringBuilder sb = new StringBuilder();

                        foreach (var p in partes)
                        {
                            string parteLimpia = p.Trim();
                            if (parteLimpia.StartsWith("\"") && parteLimpia.EndsWith("\"") && parteLimpia.Length >= 2)
                            {
                                sb.Append(parteLimpia.Substring(1, parteLimpia.Length - 2));
                            }
                            else
                            {
                                sb.Append(parteLimpia.Replace("\"", ""));
                            }
                        }
                        return "\"" + sb.ToString() + "\"";
                    }
                    return expresion.Trim();
                }
                catch
                {
                    return expresion;
                }
            }

            if(tipoDato=="float" || tipoDato=="PR11")
            {
                try
                {
                    DataTable dtFloat = new DataTable();
                    var resFloat = dtFloat.Compute(expresion, "");
                    return Convert.ToDouble(resFloat).ToString("0.00");
                }
                catch { return expresion; }
            }


            if (tipoDato == "bool" || tipoDato == "PR02")
            {
                try
                {
                    string exprBool = Regex.Replace(expresion, @"!(?!=)", " NOT ");
                    exprBool = exprBool.Replace("+", " OR ").Replace("*", " AND ");
                    exprBool = exprBool.Replace("1", " true ").Replace("0", " false ");

                    DataTable dtBool = new DataTable();
                    var res = dtBool.Compute(exprBool, "");
                    bool bRes = Convert.ToBoolean(res);
                    return bRes ? "1" : "0";
                }
                catch { return "0"; }
            }

            try
            {
                DataTable dt = new DataTable();
                var resultado = dt.Compute(expresion, "");
                string resultStr = resultado.ToString();

                if (tipoDato == "int" || tipoDato == "PR19")
                {
                    if (double.TryParse(resultStr, out double dVal))
                    {
                        return ((int)Math.Truncate(dVal)).ToString();
                    }
                }
                return resultStr;
            }
            catch { return expresion; }
        }



        private void ActualizarDataGrid()
        {
            dtgTablaSimbolos.Rows.Clear();


            if (dtgTablaSimbolos.Columns.Count < 8)
            {
                dtgTablaSimbolos.Columns.Clear();
                dtgTablaSimbolos.Columns.Add("colId", "ID");
                dtgTablaSimbolos.Columns.Add("colLexema", "Lexema");
                dtgTablaSimbolos.Columns.Add("colTipo", "Tipo Dato");
                dtgTablaSimbolos.Columns.Add("colScope", "Scope");
                dtgTablaSimbolos.Columns.Add("colTamano", "Tamaño (Bytes)");
                dtgTablaSimbolos.Columns.Add("colDireccion", "Dirección");
                dtgTablaSimbolos.Columns.Add("colInit", "Inicializada");
                dtgTablaSimbolos.Columns.Add("colValor", "Valor");
                ConfigurarEstiloDataGridView();
            }

            foreach (var item in tablaSimbolos.Values)
            {
                string respuesta = item.EstaInicializada ? "Sí" : "No";

                dtgTablaSimbolos.Rows.Add(
                item.Id,
                item.Lexema,
                item.TipoDato ?? "Desconocido",
                item.Scope ?? "Global",
                item.Tamano,
                item.Direccion.ToString(),
                respuesta,
                item.Valor ?? "null"
                  );
            }
        }

        private string ObtenerTipoVariable(string nombreVariable)
        {
            if (tablaSimbolos.ContainsKey(nombreVariable))
            {
                return tablaSimbolos[nombreVariable].TipoDato.ToLower();
            }

            return "";
        }

        private string ObtenerTipoOperando(string lexema, string token)
        {
            if (string.IsNullOrEmpty(lexema)) return "";

            // 1. Literal de Cadena
            if (token == "CAD" || lexema.StartsWith("\""))
                return "string";

            // 2. Literales Numéricos
            if (token == "CONENTERO" || token == "CONDEC" || double.TryParse(lexema, out _))
                return "number";

            // 3. Variables registradas en la Tabla de Símbolos
            if (tablaSimbolos.ContainsKey(lexema))
            {
                string t = tablaSimbolos[lexema].TipoDato?.ToLower() ?? "";
                if (t == "string" || t == "pr28" || t == "cadena") return "string";
                if (t == "int" || t == "float" || t == "double" || t == "pr19") return "number";
                if (t == "bool" || t == "boolean" || t == "pr02") return "bool";
            }

            return "";
        }

        // Método auxiliar para validar constantes numéricas (evitar desbordamiento)
        private static bool ValidarRangoNumerico(string tokenValor, string tipoDato, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (tipoDato.Equals("int", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(tokenValor, out long valorLong) || valorLong < int.MinValue || valorLong > int.MaxValue)
                {
                    
                    return false;
                }
            }
            else if (tipoDato.Equals("float", StringComparison.OrdinalIgnoreCase) || tipoDato.Equals("double", StringComparison.OrdinalIgnoreCase))
            {
                if (!double.TryParse(tokenValor, out double valorDouble) || double.IsInfinity(valorDouble))
                {
                    return false;
                }
            }

            return true;
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

        private void rTxtCodigoFuente_TextChanged(object sender, EventArgs e)
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

            if (contadorGlobal > 0)
            {
                lblStatusLeft.Text = $"● {contadorGlobal} error(es) léxico(s) detectado(s)";
                lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
            }
            else
            {
                //Si no hay errores léxicos, se procede al análisis sintáctico
                bool resultadoSintactico = AnalizadorSintactico();
                //Si el analizador sintáctico es exitoso, se procede al análisis semántico
                if (resultadoSintactico)
                {
                    bool resultadoSemantico = AnalizadorSemantico();
                    if (resultadoSemantico)
                    {
                        GenerarCodigoIntermedio();
                        lblStatusLeft.Text = "● Compilación exitosa: Estructura válida y Código P generado";
                        lblStatusLeft.ForeColor = Color.FromArgb(52, 211, 153);
                    }
                    else
                    {
                        ActualizarDataGrid();
                        rtCodigoIntermedio.Clear();
                        lblStatusLeft.Text = "● Errores semánticos detectados";
                        lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
                    }
                }
                else
                {
                    lblStatusLeft.Text = "● Errores sintácticos detectados";
                    lblStatusLeft.ForeColor = Color.FromArgb(248, 113, 113);
                }
            }
        }

        public void GenerarCodigoIntermedio()
        {
            codigoIntermedio.Clear();
            rtCodigoIntermedio.Clear();

            InstruccionP.ReiniciarContadorEtiquetas();
            int index = 0;
            List<InstruccionP> instruccionesGlobales = ProcesarBloqueTokens(ref index, listaTokensCompletos);
            codigoIntermedio.AddRange(instruccionesGlobales);

            // Mostrar el código intermedio resultante en el control visual
            foreach (var instruccion in codigoIntermedio)
            {
                rtCodigoIntermedio.Text += instruccion.ToString() + Environment.NewLine;
            }
        }

        // Analizador de bloques y estructuras de control recursivo/iterativo
        // Analizador de bloques y estructuras de control recursivo/iterativo robusto
        private List<InstruccionP> ProcesarBloqueTokens(ref int index, List<(string Token, string Lexema, int Linea)> tokens)
        {
            List<InstruccionP> bloque = new List<InstruccionP>();

            while (index < tokens.Count)
            {
                var actual = tokens[index];

                // Omitir llaves de cierre, marcas de fin o saltos de línea sueltos
                if (actual.Lexema == "}" || actual.Lexema.ToLower() == "end" || actual.Lexema == "\n")
                {
                    index++;
                    continue;
                }

                string lexemaMin = actual.Lexema.ToLower();

                // 1. DO-WHILE
                if (lexemaMin == "do")
                {
                    index++; // pasar 'do'
                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++; // pasar '{'
                        List<InstruccionP> cuerpoDo = ProcesarBloqueTokens(ref index, tokens);

                        while (index < tokens.Count && tokens[index].Lexema.ToLower() != "while") index++;
                        if (index < tokens.Count && tokens[index].Lexema.ToLower() == "while")
                        {
                            index++; // pasar 'while'
                            List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                            var instsDoWhile = InstruccionP.GenerarCodigoDoWhile(condTokens, cuerpoDo);
                            bloque.AddRange(instsDoWhile);
                        }
                    }
                }
                // 2. WHILE
                else if (lexemaMin == "while")
                {
                    index++; // pasar 'while'
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    List<InstruccionP> cuerpoWhile = new List<InstruccionP>();
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        cuerpoWhile = ProcesarBloqueTokens(ref index, tokens);
                    }

                    var instsWhile = InstruccionP.GenerarCodigoWhile(condTokens, cuerpoWhile);
                    bloque.AddRange(instsWhile);
                }
                // 3. IF / IF-ELSE (Mejorado para tolerar espacios, saltos de línea y anidamientos)
                else if (lexemaMin == "if")
                {
                    index++; // pasar 'if'
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    List<InstruccionP> cuerpoIf = new List<InstruccionP>();
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        cuerpoIf = ProcesarBloqueTokens(ref index, tokens);
                    }

                    // Buscar la palabra 'else' de forma tolerante (saltando espacios/saltos de línea)
                    bool tieneElse = false;
                    int elseIndex = -1;
                    for (int k = index; k < tokens.Count; k++)
                    {
                        string lex = tokens[k].Lexema.ToLower();
                        if (lex == "else")
                        {
                            tieneElse = true;
                            elseIndex = k;
                            break;
                        }
                        // Si encontramos un token que rompa la continuidad estructural antes de un else, paramos
                        if (lex != "}" && lex != "\n" && lex != " " && !string.IsNullOrEmpty(lex) && lex != ";")
                        {
                            break;
                        }
                    }

                    if (tieneElse)
                    {
                        index = elseIndex + 1; // pasar 'else'
                        while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                        List<InstruccionP> cuerpoElse = new List<InstruccionP>();
                        if (index < tokens.Count && tokens[index].Lexema == "{")
                        {
                            index++;
                            cuerpoElse = ProcesarBloqueTokens(ref index, tokens);
                        }

                        var instsIfElse = InstruccionP.GenerarCodigoIfElse(condTokens, cuerpoIf, cuerpoElse);
                        bloque.AddRange(instsIfElse);
                    }
                    else
                    {
                        var instsIf = InstruccionP.GenerarCodigoIf(condTokens, cuerpoIf);
                        bloque.AddRange(instsIf);
                    }
                }
                //Switch
                else if (lexemaMin == "switch")
                {
                    index++; // pasar 'switch'
                    List<string> exprTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++; // pasar '{'
                
                        List<SwitchCaseNode> listaCasos = new List<SwitchCaseNode>();
                        SwitchCaseNode casoActual = null;

                        while (index < tokens.Count && tokens[index].Lexema != "}")
                        {
                            string lex = tokens[index].Lexema.ToLower();

                            if (lex == "case")
                            {
                                if (casoActual != null) listaCasos.Add(casoActual);
                        
                                index++; // pasar 'case'
                                string valCaso = tokens[index].Lexema; // Valor del caso
                                index++;
                                if (tokens[index].Lexema == ":") index++;

                                casoActual = new SwitchCaseNode { ValorCaso = valCaso, Instrucciones = new List<InstruccionP>(), TieneBreak = false };
                            }
                            else if (lex == "default")
                            {
                                if (casoActual != null) listaCasos.Add(casoActual);
                        
                                index++; // pasar 'default'
                                if (tokens[index].Lexema == ":") index++;

                                casoActual = new SwitchCaseNode { ValorCaso = null, Instrucciones = new List<InstruccionP>(), TieneBreak = false };
                            }
                            else if (lex == "break")
                            {
                                index++; // pasar 'break'
                                if (tokens[index].Lexema == ";") index++;
                                if (casoActual != null) casoActual.TieneBreak = true;
                            }
                            else if (lex == "print")
                            {
                                index++; // pasar 'print'
                                List<string> printTokens = new List<string>();
                                while (index < tokens.Count && tokens[index].Lexema != ";")
                                {
                                    printTokens.Add(tokens[index].Lexema);
                                    index++;
                                }
                                if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                                if (casoActual != null)
                                {
                                    casoActual.Instrucciones.Add(new InstruccionP("PRN", string.Join(" ", printTokens)));
                                }
                            }
                            else
                            {
                                index++; // Omitir tokens vacíos o no reconocidos temporalmente
                            }
                        }
                        if (casoActual != null) listaCasos.Add(casoActual);
                        if (index < tokens.Count && tokens[index].Lexema == "}") index++; // pasar '}'

                        var instsSwitch = InstruccionP.GenerarCodigoSwitch(exprTokens, listaCasos);
                        bloque.AddRange(instsSwitch);
                    }
                }

                // 4. FOR
                else if (lexemaMin == "for")
                {
                    index++; // pasar 'for'
                    List<string> forParams = ExtraerTokensParentesis(ref index, tokens);

                    List<string> initTokens = new List<string>();
                    List<string> condTokens = new List<string>();
                    List<string> incTokens = new List<string>();

                    int parte = 0;
                    foreach (var t in forParams)
                    {
                        if (t == ";") { parte++; continue; }
                        if (parte == 0) initTokens.Add(t);
                        else if (parte == 1) condTokens.Add(t);
                        else if (parte == 2) incTokens.Add(t);
                    }

                    List<InstruccionP> initInsts = new List<InstruccionP>();
                    int idxAsig = initTokens.IndexOf("=");
                    if (idxAsig != -1 && idxAsig > 0)
                    {
                        string varName = initTokens[idxAsig - 1];
                        List<string> exprInit = initTokens.Skip(idxAsig + 1).ToList();
                        initInsts = InstruccionP.GenerarCodigoDinamico(varName, exprInit);
                    }

                    List<InstruccionP> incInsts = new List<InstruccionP>();
                    int idxAsigInc = incTokens.IndexOf("=");
                    if (idxAsigInc != -1 && idxAsigInc > 0)
                    {
                        string varNameInc = incTokens[idxAsigInc - 1];
                        List<string> exprInc = incTokens.Skip(idxAsigInc + 1).ToList();
                        incInsts = InstruccionP.GenerarCodigoDinamico(varNameInc, exprInc);
                    }

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    List<InstruccionP> cuerpoFor = new List<InstruccionP>();
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        cuerpoFor = ProcesarBloqueTokens(ref index, tokens);
                    }

                    var instsFor = InstruccionP.GenerarCodigoFor(initInsts, condTokens, incInsts, cuerpoFor);
                    bloque.AddRange(instsFor);
                }
                // 5. PRINT (Para que no se ignore dentro de los bloques o estructuras)
                else if (lexemaMin == "print")
                {
                    index++; // pasar 'print'
                    List<string> printTokens = new List<string>();
                    while (index < tokens.Count && tokens[index].Lexema != ";")
                    {
                        printTokens.Add(tokens[index].Lexema);
                        index++;
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                    bloque.Add(new InstruccionP("PRN", string.Join(" ", printTokens)));
                }
                // 6. INPUT (Para que no se ignore)
                else if (lexemaMin == "input")
                {
                    index++; // pasar 'input'
                    string varTarget = "";
                    while (index < tokens.Count && tokens[index].Lexema != ";")
                    {
                        if (tokens[index].Lexema.StartsWith("$")) varTarget = tokens[index].Lexema;
                        index++;
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                    if (!string.IsNullOrEmpty(varTarget))
                    {
                        bloque.Add(new InstruccionP("INP", varTarget));
                    }
                }
                // 7. ASIGNACIÓN O DECLARACIÓN CON ASIGNACIÓN
                else if (actual.Lexema == "=" || actual.Token == "OPASIG" || (index + 1 < tokens.Count && tokens[index + 1].Lexema == "="))
                {
                    int idxAsig = (actual.Lexema == "=" || actual.Token == "OPASIG") ? index : index + 1;
                    string varName = tokens[idxAsig - 1].Lexema;

                    List<string> expTokens = new List<string>();
                    index = idxAsig + 1;
                    while (index < tokens.Count && tokens[index].Lexema != ";" && tokens[index].Lexema != ",")
                    {
                        expTokens.Add(tokens[index].Lexema);
                        index++;
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                    if (expTokens.Count > 0)
                    {
                        var instsAsig = InstruccionP.GenerarCodigoDinamico(varName, expTokens);
                        bloque.AddRange(instsAsig);
                    }
                    continue;
                }
                else
                {
                    index++;
                }
            }

            return bloque;
        }

        // Método auxiliar para extraer los tokens que se encuentran dentro de los paréntesis (condiciones o parámetros)
        private List<string> ExtraerTokensParentesis(ref int index, List<(string Token, string Lexema, int Linea)> tokens)
        {
            List<string> resultado = new List<string>();
            while (index < tokens.Count && tokens[index].Lexema != "(") index++;

            if (index < tokens.Count && tokens[index].Lexema == "(")
            {
                index++; // pasar '('
                int parenCount = 1;
                while (index < tokens.Count)
                {
                    if (tokens[index].Lexema == "(") parenCount++;
                    else if (tokens[index].Lexema == ")")
                    {
                        parenCount--;
                        if (parenCount == 0)
                        {
                            index++; // pasar ')'
                            break;
                        }
                    }
                    if (parenCount > 0)
                    {
                        resultado.Add(tokens[index].Lexema);
                    }
                    index++;
                }
            }
            return resultado;
        }


        // =====================================================================
        // 1. INICIALIZAR LAS TABLAS DE LOS DATAGRIDVIEW
        // =====================================================================
        private void InicializarGridsIntermedios()
        {
            // Configurar DataGridView de Cuádruplos (ej. dtgCuadruplos)
            dtgCuadruplos.Columns.Clear();
            dtgCuadruplos.Columns.Add("Operador", "Operador");
            dtgCuadruplos.Columns.Add("Arg1", "Arg 1");
            dtgCuadruplos.Columns.Add("Arg2", "Arg 2");
            dtgCuadruplos.Columns.Add("Resultado", "Resultado");
            dtgCuadruplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Configurar DataGridView de Triplos (ej. dtgTriplos)
            dtgTriplos.Columns.Clear();
            dtgTriplos.Columns.Add("Indice", "#");
            dtgTriplos.Columns.Add("Operador", "Operador");
            dtgTriplos.Columns.Add("Arg1", "Arg 1");
            dtgTriplos.Columns.Add("Arg2", "Arg 2");
            dtgTriplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        // =====================================================================
        // 2. MÉTODO PRINCIPAL PARA GENERAR Y MOSTRAR TODAS LAS REPRESENTACIONES
        // =====================================================================
        public void GenerarYMostrarRepresentacionesIntermedias()
        {
            // Asegurarnos de que los grids estén listos
            InicializarGridsIntermedios();

            // Listas temporales para almacenar los resultados
            List<Cuadruplo> listaCuadruplos = new List<Cuadruplo>();
            List<Triplo> listaTriplos = new List<Triplo>();
            List<string> listaTAC = new List<string>();

            List<string> tokensEjemploExpr = new List<string>(); // Aquí puedes extraer los tokens de la expresión actual

            // --- EJEMPLO DE RECORRIDO Y LLENADO ---
            // (Puedes integrar esto dentro de tu método ProcesarBloqueTokens o recorrer listaTokensCompletos)
            int tempCount = 1;

            foreach (var token in listaTokensCompletos)
            {
                // Ejemplo ilustrativo de cómo estructurar una asignación o operación detectada:
                // Supongamos que detectas una operación aritmética tipo: $SU = $SU + $VA
                if (token.Lexema == "=")
                {
                    // Ejemplo de Cuádruplo: (=, $SU + $VA, -, $SU) o desglosado con temporales
                    listaCuadruplos.Add(new Cuadruplo("+", "$SU", "$VA", $"t{tempCount}"));
                    listaCuadruplos.Add(new Cuadruplo("=", $"t{tempCount}", "-", "$SU"));

                    // Ejemplo de Triplo: 
                    listaTriplos.Add(new Triplo("+", "$SU", "$VA"));
                    listaTriplos.Add(new Triplo("=", "$SU", "(0)")); // Hace referencia al triplo 0

                    // Ejemplo de Código de Tres Direcciones (TAC)
                    listaTAC.Add($"t{tempCount} = $SU + $VA");
                    listaTAC.Add($"$SU = t{tempCount}");

                    tempCount++;
                }
            }

            // =====================================================================
            // A. LLENAR DATAGRIDVIEW (Triplos y Cuádruplos)
            // =====================================================================
            dtgCuadruplos.Rows.Clear();
            foreach (var c in listaCuadruplos)
            {
                dtgCuadruplos.Rows.Add(c.Operador, c.Arg1, c.Arg2, c.Resultado);
            }

            dtgTriplos.Rows.Clear();
            int idx = 0;
            foreach (var t in listaTriplos)
            {
                dtgTriplos.Rows.Add($"({idx})", t.Operador, t.Arg1, t.Arg2);
                idx++;
            }

            // =====================================================================
            // B. LLENAR RICHTEXBOX (Notaciones y TAC)
            // =====================================================================
            // Simulando una lista de tokens de expresión para las notaciones
            tokensEjemploExpr = new List<string> { "$SU", "+", "$VA" };

            // 1. Notación Infija
            rtNotacionInfija.Clear();
            rtNotacionInfija.Text = string.Join(" ", tokensEjemploExpr);

            // 2. Notación Postfija (RPN)
            List<string> postfija = TraductorNotaciones.InfijaAPostfija(tokensEjemploExpr);
            rtNotacionPostfija.Clear();
            rtNotacionPostfija.Text = string.Join(" ", postfija);

            // 3. Notación Prefija (Polaca)
            List<string> prefija = TraductorNotaciones.InfijaAPrefija(tokensEjemploExpr);
            rtNotacionPrefija.Clear();
            rtNotacionPrefija.Text = string.Join(" ", prefija);

            // 4. Código de Tres Direcciones (TAC)
            rtCodigoTAC.Clear();
            rtCodigoTAC.Text = string.Join(Environment.NewLine, listaTAC);
        }

        public void GenerarTodoDinamico()
        {
            // 1. Limpiar vistas y reiniciar contadores
            dtgCuadruplos.Rows.Clear();
            dtgTriplos.Rows.Clear();
            rtCodigoTAC.Clear();

            if (rtNotacionInfija != null) rtNotacionInfija.Clear();
            if (rtNotacionPostfija != null) rtNotacionPostfija.Clear();
            if (rtNotacionPrefija != null) rtNotacionPrefija.Clear();

            CompiladorExpresiones.ReiniciarTemporales();
            InstruccionP.ReiniciarContadorEtiquetas();

            List<Cuadruplo> cuadruplosGlobales = new List<Cuadruplo>();
            List<Triplo> triplosGlobales = new List<Triplo>();
            List<string> tacGlobales = new List<string>();

            // Listas para acumular las expresiones limpias para las notaciones
            List<string> lineasInfijas = new List<string>();
            List<string> lineasPostfijas = new List<string>();
            List<string> lineasPrefijas = new List<string>();

            int index = 0;
            List<string> tokensCompletos = listaTokensCompletos.Select(t => t.Lexema).ToList();

            // 2. Recorrido inteligente del código fuente
            while (index < listaTokensCompletos.Count)
            {
                var actual = listaTokensCompletos[index];
                string lexemaMin = actual.Lexema.ToLower();

                // Omitir llaves, marcas de fin o saltos de línea
                if (actual.Lexema == "}" || lexemaMin == "end" || actual.Lexema == "\n" || actual.Lexema == ";")
                {
                    index++;
                    continue;
                }

                // Detección de Asignaciones
                if (actual.Lexema == "=" || actual.Token == "OPASIG" || (index + 1 < listaTokensCompletos.Count && listaTokensCompletos[index + 1].Lexema == "="))
                {
                    int idxAsig = (actual.Lexema == "=" || actual.Token == "OPASIG") ? index : index + 1;
                    string varName = listaTokensCompletos[idxAsig - 1].Lexema;

                    List<string> expTokens = new List<string>();
                    index = idxAsig + 1;
                    while (index < listaTokensCompletos.Count && listaTokensCompletos[index].Lexema != ";" && listaTokensCompletos[index].Lexema != ",")
                    {
                        expTokens.Add(listaTokensCompletos[index].Lexema);
                        index++;
                    }

                    if (expTokens.Count > 0)
                    {
                        // Compilar la expresión obteniendo por separado el resultado para Cuádruplos y para Triplos
                        var resCompilacion = CompiladorExpresiones.Compilar(expTokens, cuadruplosGlobales, triplosGlobales, tacGlobales);

                        // 1. Agregar Cuádruplo de asignación
                        cuadruplosGlobales.Add(new Cuadruplo("=", resCompilacion.ResultadoCuadruplo, "-", varName));

                        // 2. Agregar TAC de asignación
                        tacGlobales.Add($"{varName} = {resCompilacion.ResultadoCuadruplo}");

                        // 3. Agregar Triplo de asignación de manera limpia
                        // Si fue una asignación simple (ej: 10), Arg2 será "10". Si fue compleja, apuntará al último índice (ej: "(4)").
                        triplosGlobales.Add(new Triplo("=", varName, resCompilacion.ResultadoTriplo));

                        // Registrar para Notaciones
                        List<string> tokensAsignacionCompleta = new List<string> { varName, "=" };
                        tokensAsignacionCompleta.AddRange(expTokens);
                        RegistrarNotaciones(tokensAsignacionCompleta, lineasInfijas, lineasPostfijas, lineasPrefijas);
                    }
                    continue;
                }
                // B. DETECTAR CONDICIONALES Y CICLOS (if, while, for) para extraer sus expresiones de condición
                else if (lexemaMin == "if" || lexemaMin == "while" || lexemaMin == "for")
                {
                    index++; // pasar la palabra clave
                    List<string> tokensParentesis = ExtraerTokensParentesis(ref index, listaTokensCompletos);

                    if (tokensParentesis.Count > 0)
                    {
                        // Si es un 'for', viene separado por comas o puntos y comas (ej: $i = 1 ; $i < 5 ; $i ++)
                        // Vamos a extraer y compilar cada sub-expresión relacional o aritmética válida
                        List<string> subExpresion = new List<string>();
                        foreach (var t in tokensParentesis)
                        {
                            if (t == ";" || t == ",")
                            {
                                if (subExpresion.Count > 0 && ContieneOperador(subExpresion))
                                {
                                    CompiladorExpresiones.Compilar(subExpresion, cuadruplosGlobales, triplosGlobales, tacGlobales);
                                    RegistrarNotaciones(subExpresion, lineasInfijas, lineasPostfijas, lineasPrefijas);
                                }
                                subExpresion.Clear();
                            }
                            else
                            {
                                subExpresion.Add(t);
                            }
                        }
                        // Procesar la última subexpresión si quedó pendiente
                        if (subExpresion.Count > 0 && ContieneOperador(subExpresion))
                        {
                            CompiladorExpresiones.Compilar(subExpresion, cuadruplosGlobales, triplosGlobales, tacGlobales);
                            RegistrarNotaciones(subExpresion, lineasInfijas, lineasPostfijas, lineasPrefijas);
                        }
                    }
                }
                else
                {
                    index++;
                }
            }

            // =====================================================================
            // 3. LLENAR CONTROLES VISUALES EN LA INTERFAZ
            // =====================================================================

            // A. Poblar DataGridView de Cuádruplos
            foreach (var c in cuadruplosGlobales)
            {
                dtgCuadruplos.Rows.Add(c.Operador, c.Arg1, c.Arg2, c.Resultado);
            }

            // B. Poblar DataGridView de Triplos
            int idx = 0;
            foreach (var t in triplosGlobales)
            {
                dtgTriplos.Rows.Add($"({idx})", t.Operador, t.Arg1, t.Arg2);
                idx++;
            }

            // C. Poblar TAC en su RichTextBox
            rtCodigoTAC.Text = string.Join(Environment.NewLine, tacGlobales);

            // D. Poblar Notaciones divididas limpiamente por líneas en cada RichTextBox
            if (rtNotacionInfija != null) rtNotacionInfija.Text = string.Join(Environment.NewLine, lineasInfijas);
            if (rtNotacionPostfija != null) rtNotacionPostfija.Text = string.Join(Environment.NewLine, lineasPostfijas);
            if (rtNotacionPrefija != null) rtNotacionPrefija.Text = string.Join(Environment.NewLine, lineasPrefijas);
        }

        // Métodos auxiliares de apoyo:
        private void RegistrarNotaciones(List<string> expTokens, List<string> infija, List<string> postfija, List<string> prefija)
        {
            var post = TraductorNotaciones.InfijaAPostfija(expTokens);
            var pre = TraductorNotaciones.InfijaAPrefija(expTokens);

            infija.Add(string.Join(" ", expTokens));
            postfija.Add(string.Join(" ", post));
            prefija.Add(string.Join(" ", pre));
        }

        private bool ContieneOperador(List<string> tokens)
        {
            return tokens.Any(t => t == "+" || t == "-" || t == "*" || t == "/" || t == ">" || t == "<" || t == "==" || t == "!=" || t == ">=" || t == "<=");
        }

        // Método recursivo principal para procesar estructuras de control dinámicamente
        private void ProcesarBloqueDinamico(
            ref int index,
            List<(string Token, string Lexema, int Linea)> tokens,
            List<Cuadruplo> cuadruplos,
            List<Triplo> triplos,
            List<string> tac)
        {
            while (index < tokens.Count)
            {
                var actual = tokens[index];

                if (actual.Lexema == "}" || actual.Lexema.ToLower() == "end" || actual.Lexema == "\n")
                {
                    index++;
                    continue;
                }

                string lexemaMin = actual.Lexema.ToLower();

                // 1. ASIGNACIÓN O DECLARACIÓN CON ASIGNACIÓN
                if (actual.Lexema == "=" || actual.Token == "OPASIG" || (index + 1 < tokens.Count && tokens[index + 1].Lexema == "="))
                {
                    int idxAsig = (actual.Lexema == "=" || actual.Token == "OPASIG") ? index : index + 1;
                    string varName = tokens[idxAsig - 1].Lexema;

                    List<string> expTokens = new List<string>();
                    index = idxAsig + 1;
                    while (index < tokens.Count && tokens[index].Lexema != ";" && tokens[index].Lexema != ",")
                    {
                        expTokens.Add(tokens[index].Lexema);
                        index++;
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                    if (expTokens.Count > 0)
                    {
                        var resCompilacion = CompiladorExpresiones.Compilar(expTokens, cuadruplos, triplos, tac);

                        // Instrucción de asignación final (=, resultadoExpr, -, varName)
                        cuadruplos.Add(new Cuadruplo("=", resCompilacion.ResultadoCuadruplo, "-", varName));
                        triplos.Add(new Triplo("=", varName, resCompilacion.ResultadoTriplo));
                        tac.Add($"{varName} = {resCompilacion.ResultadoCuadruplo}");
                    }
                    continue;
                }
                // 2. IF / IF-ELSE
                else if (lexemaMin == "if")
                {
                    index++; // pasar 'if'
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    List<Cuadruplo> cuerpoIfCuad = new List<Cuadruplo>();
                    List<Triplo> cuerpoIfTrip = new List<Triplo>();
                    List<string> cuerpoIfTac = new List<string>();

                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuerpoIfCuad, cuerpoIfTrip, cuerpoIfTac);
                    }

                    string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();
                    var resCondIf = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                    string condRes = resCondIf.ResultadoCuadruplo;

                    // Salto si falso al fin o al else
                    tac.Add($"ifFalse {condRes} goto {etiquetaFin}");

                    // Agregar el cuerpo del if a las listas principales
                    cuadruplos.AddRange(cuerpoIfCuad);
                    triplos.AddRange(cuerpoIfTrip);
                    tac.AddRange(cuerpoIfTac);

                    tac.Add($"{etiquetaFin}:");
                }
                // 3. WHILE
                else if (lexemaMin == "while")
                {
                    index++;
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                    string etiquetaInicio = InstruccionP.ObtenerNuevaEtiqueta();
                    string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();

                    tac.Add($"{etiquetaInicio}:");
                    var resCondWhile = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                    string condRes = resCondWhile.ResultadoCuadruplo;
                    tac.Add($"ifFalse {condRes} goto {etiquetaFin}");

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac);
                    }

                    tac.Add($"goto {etiquetaInicio}");
                    tac.Add($"{etiquetaFin}:");
                }
                // 4. PRINT
                else if (lexemaMin == "print")
                {
                    index++;
                    List<string> printTokens = new List<string>();
                    while (index < tokens.Count && tokens[index].Lexema != ";")
                    {
                        printTokens.Add(tokens[index].Lexema);
                        index++;
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                    string contenido = string.Join(" ", printTokens);
                    cuadruplos.Add(new Cuadruplo("PRINT", contenido, "-", "-"));
                    triplos.Add(new Triplo("PRINT", contenido, "-"));
                    tac.Add($"print {contenido}");
                }
                else
                {
                    index++;
                }
            }
        }



        private void lblStatusLeft_Click(object sender, EventArgs e)
        {

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }   
}