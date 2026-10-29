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

        // Variables y controles para zoom dinámico en el editor de código
        private float _tamanoFuenteCodigo = 10.5f;
        private const float TAMANO_MIN_FUENTE = 7.0f;
        private const float TAMANO_MAX_FUENTE = 36.0f;
        private const float TAMANO_DEFAULT_FUENTE = 10.5f;

        private Panel panelZoomControls;
        private Label lblZoomLevel;
        private Button btnZoomIn;
        private Button btnZoomOut;
        private Button btnZoomReset;

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
            ConfigurarZoom();
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

        #region Configuración de Zoom del Editor de Código

        private void ConfigurarZoom()
        {
            if (rTxtCodigoFuente != null)
            {
                _tamanoFuenteCodigo = rTxtCodigoFuente.Font.Size;
                rTxtCodigoFuente.MouseWheel += RTxtCodigoFuente_MouseWheel;
            }
            if (rTxtNumeros != null)
            {
                rTxtNumeros.MouseWheel += RTxtCodigoFuente_MouseWheel;
            }

            CrearControlesVisualesZoom();
        }

        private void CrearControlesVisualesZoom()
        {
            if (panelCodigoHeader == null) return;

            panelZoomControls = new Panel
            {
                Dock = DockStyle.Right,
                Width = 205,
                Height = 34,
                BackColor = Color.Transparent
            };

            Label lblZoomTitulo = new Label
            {
                Text = "Zoom:",
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular),
                AutoSize = false,
                Width = 44,
                Height = 26,
                Top = 4,
                Left = 0,
                TextAlign = ContentAlignment.MiddleRight
            };

            btnZoomOut = new Button
            {
                Text = "−",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Width = 28,
                Height = 24,
                Top = 5,
                Left = 48,
                Cursor = Cursors.Hand
            };
            btnZoomOut.FlatAppearance.BorderSize = 0;
            btnZoomOut.Click += (s, e) => CambiarZoomCodigo(-1.0f);

            lblZoomLevel = new Label
            {
                Text = "100%",
                Font = new Font("Segoe UI Semibold", 8.5F),
                ForeColor = Color.FromArgb(226, 232, 240),
                BackColor = Color.FromArgb(30, 41, 59),
                BorderStyle = BorderStyle.FixedSingle,
                AutoSize = false,
                Width = 50,
                Height = 24,
                Top = 5,
                Left = 80,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblZoomLevel.Click += (s, e) => RestablecerZoomCodigo();

            btnZoomIn = new Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Width = 28,
                Height = 24,
                Top = 5,
                Left = 134,
                Cursor = Cursors.Hand
            };
            btnZoomIn.FlatAppearance.BorderSize = 0;
            btnZoomIn.Click += (s, e) => CambiarZoomCodigo(1.0f);

            btnZoomReset = new Button
            {
                Text = "↺",
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(148, 163, 184),
                BackColor = Color.FromArgb(51, 65, 85),
                FlatStyle = FlatStyle.Flat,
                Width = 24,
                Height = 24,
                Top = 5,
                Left = 166,
                Cursor = Cursors.Hand
            };
            btnZoomReset.FlatAppearance.BorderSize = 0;
            btnZoomReset.Click += (s, e) => RestablecerZoomCodigo();

            ToolTip tip = new ToolTip();
            tip.SetToolTip(btnZoomOut, "Disminuir tamaño de letra (Ctrl - o Ctrl + Rueda abajo)");
            tip.SetToolTip(lblZoomLevel, "Restablecer al 100% (Ctrl 0)");
            tip.SetToolTip(btnZoomIn, "Aumentar tamaño de letra (Ctrl + o Ctrl + Rueda arriba)");
            tip.SetToolTip(btnZoomReset, "Restablecer tamaño predeterminado (Ctrl 0)");

            panelZoomControls.Controls.Add(lblZoomTitulo);
            panelZoomControls.Controls.Add(btnZoomOut);
            panelZoomControls.Controls.Add(lblZoomLevel);
            panelZoomControls.Controls.Add(btnZoomIn);
            panelZoomControls.Controls.Add(btnZoomReset);

            if (lblCodigoHeaderTip != null)
            {
                lblCodigoHeaderTip.Text = "✦ Ctrl+V: pegar | F5: Compilar | Zoom: Ctrl+Rueda o Ctrl+/-";
            }

            panelCodigoHeader.Controls.Add(panelZoomControls);
            panelZoomControls.BringToFront();
        }

        public void CambiarZoomCodigo(float delta)
        {
            float nuevoTamano = _tamanoFuenteCodigo + delta;
            if (nuevoTamano < TAMANO_MIN_FUENTE) nuevoTamano = TAMANO_MIN_FUENTE;
            if (nuevoTamano > TAMANO_MAX_FUENTE) nuevoTamano = TAMANO_MAX_FUENTE;

            AplicarTamanoFuenteCodigo(nuevoTamano);
        }

        public void RestablecerZoomCodigo()
        {
            AplicarTamanoFuenteCodigo(TAMANO_DEFAULT_FUENTE);
        }

        public void AplicarTamanoFuenteCodigo(float nuevoTamano)
        {
            _tamanoFuenteCodigo = (float)Math.Round(nuevoTamano, 1);
            Font nuevaFuente = new Font("Consolas", _tamanoFuenteCodigo, FontStyle.Regular);

            rTxtCodigoFuente.Font = nuevaFuente;
            rTxtNumeros.Font = nuevaFuente;

            Size size = TextRenderer.MeasureText("9999", nuevaFuente);
            int anchoDeseado = Math.Max(52, size.Width + 14);
            rTxtNumeros.Width = anchoDeseado;

            if (this.IsHandleCreated)
            {
                ActualizarNumerosLinea(null, null);
            }

            if (lblZoomLevel != null)
            {
                int porcentaje = (int)Math.Round((_tamanoFuenteCodigo / TAMANO_DEFAULT_FUENTE) * 100);
                lblZoomLevel.Text = $"{porcentaje}%";
            }
        }

        private void RTxtCodigoFuente_MouseWheel(object sender, MouseEventArgs e)
        {
            if ((Control.ModifierKeys & Keys.Control) == Keys.Control)
            {
                if (e.Delta > 0)
                {
                    CambiarZoomCodigo(1.0f);
                }
                else if (e.Delta < 0)
                {
                    CambiarZoomCodigo(-1.0f);
                }
                ((HandledMouseEventArgs)e).Handled = true;
            }
        }

        #endregion

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
                rTxtCodigoFuente.SelectionFont = new Font("Consolas", _tamanoFuenteCodigo);
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
            if (keyData == (Keys.Control | Keys.Oemplus) || keyData == (Keys.Control | Keys.Add) || keyData == (Keys.Control | Keys.Shift | Keys.Oemplus))
            {
                CambiarZoomCodigo(1.0f);
                return true;
            }
            if (keyData == (Keys.Control | Keys.OemMinus) || keyData == (Keys.Control | Keys.Subtract))
            {
                CambiarZoomCodigo(-1.0f);
                return true;
            }
            if (keyData == (Keys.Control | Keys.D0) || keyData == (Keys.Control | Keys.NumPad0))
            {
                RestablecerZoomCodigo();
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
                bool esDeclaracion = actual.Token == "PR19" || actual.Token == "PR28" || actual.Token == "PR02" || actual.Token == "PRO2" ||
                                     actual.Token == "PR11" || actual.Token == "PR13" || actual.Token == "PR03" || actual.Token == "PRO3" ||
                                     lexemaMin == "int" || lexemaMin == "float" || lexemaMin == "double" ||
                                     lexemaMin == "string" || lexemaMin == "char" || lexemaMin == "bool" || lexemaMin == "boolean";

                bool esVariableEnDeclaracion = false;
                if (i > 0)
                {
                    var anterior = listaTokensCompletos[i - 1];
                    string antMin = anterior.Lexema.ToLower();
                    if (anterior.Token == "PR19" || anterior.Token == "PR28" || anterior.Token == "PR02" || anterior.Token == "PRO2" ||
                        anterior.Token == "PR11" || anterior.Token == "PR13" || anterior.Token == "PR03" || anterior.Token == "PRO3" ||
                        antMin == "int" || antMin == "float" || antMin == "double" || antMin == "string" || antMin == "char" || antMin == "bool" || antMin == "boolean")
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

                        // Detección de error por variables con el mismo nombre (redeclaración)
                        bool yaDeclaradaMismoAmbito = ambitos.Peek().Contains(idenLexema);
                        bool yaDeclaradaExterior = false;
                        foreach (var scope in ambitos)
                        {
                            if (scope.Contains(idenLexema))
                            {
                                yaDeclaradaExterior = true;
                                break;
                            }
                        }

                        if (yaDeclaradaMismoAmbito)
                        {
                            ImprimirErrorSemantico(siguiente.Linea, $"Error Semántico: La variable '{idenLexema}' ya fue declarada previamente en este mismo ámbito.");
                            erroresSemanticos++;
                        }
                        else if (yaDeclaradaExterior)
                        {
                            ImprimirErrorSemantico(siguiente.Linea, $"Error Semántico: La variable '{idenLexema}' ya fue declarada previamente en un ámbito exterior (no se permite duplicar nombres de variables).");
                            erroresSemanticos++;
                        }
                        else
                        {
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
                }

                if (lexemaMin == "switch")
                {
                    HashSet<string> casosRegistrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    int nivelLlavesSwitch = 0;
                    bool dentroDeSwitch = false;

                    // 1. Obtener el tipo de dato de la expresión evaluada en el switch (ej. switch ($a))
                    string tipoExpresionSwitch = "";
                    int idxParenOpen = i + 1;
                    while (idxParenOpen < listaTokensCompletos.Count && listaTokensCompletos[idxParenOpen].Lexema != "(") idxParenOpen++;

                    if (idxParenOpen < listaTokensCompletos.Count)
                    {
                        List<string> tokensSwitchExpr = new List<string>();
                        int pCount = 0;
                        for (int p = idxParenOpen; p < listaTokensCompletos.Count; p++)
                        {
                            if (listaTokensCompletos[p].Lexema == "(") pCount++;
                            else if (listaTokensCompletos[p].Lexema == ")")
                            {
                                pCount--;
                                if (pCount == 0) break;
                            }
                            else if (pCount > 0)
                            {
                                tokensSwitchExpr.Add(listaTokensCompletos[p].Lexema);
                            }
                        }

                        if (tokensSwitchExpr.Count > 0)
                        {
                            string exprTokenUnico = tokensSwitchExpr[0];
                            if (exprTokenUnico.StartsWith("$") || tablaSimbolos.ContainsKey(exprTokenUnico))
                            {
                                tipoExpresionSwitch = ObtenerTipoVariable(exprTokenUnico);
                            }
                            else
                            {
                                tipoExpresionSwitch = ObtenerTipoOperando(exprTokenUnico, "");
                            }
                        }
                    }

                    // 2. Recorrer el bloque interno del switch para validar tipos y duplicados
                    for (int j = i; j < listaTokensCompletos.Count; j++)
                    {
                        var tokenActualSwitch = listaTokensCompletos[j];

                        if (tokenActualSwitch.Lexema == "{")
                        {
                            nivelLlavesSwitch++;
                            dentroDeSwitch = true;
                        }
                        else if (tokenActualSwitch.Lexema == "}")
                        {
                            nivelLlavesSwitch--;
                            if (nivelLlavesSwitch == 0 && dentroDeSwitch)
                            {
                                break; // Fin del bloque switch actual
                            }
                        }
                        else if (tokenActualSwitch.Lexema.ToLower() == "case" && j + 1 < listaTokensCompletos.Count)
                        {
                            var tokenValorCase = listaTokensCompletos[j + 1];
                            string valorCase = tokenValorCase.Lexema;
                            string tokenTipoCase = tokenValorCase.Token;

                            // Determinar el tipo del valor del case actual
                            string tipoCaseDetectado = "";
                            if (tokenTipoCase == "CONENTERO" || int.TryParse(valorCase, out _))
                            {
                                tipoCaseDetectado = "int";
                                if (tokenTipoCase == "CONENTERO")
                                {
                                    if (!ValidarRangoNumerico(valorCase, "int", out string msgOverflowCase))
                                    {
                                        ImprimirErrorSemantico(tokenActualSwitch.Linea, msgOverflowCase);
                                        erroresSemanticos++;
                                    }
                                }
                            }
                            else if (tokenTipoCase == "CONDEC" || double.TryParse(valorCase, out _))
                                tipoCaseDetectado = "float";
                            else if (tokenTipoCase == "CAD" || valorCase.StartsWith("\""))
                                tipoCaseDetectado = "string";
                            else if (valorCase.StartsWith("$"))
                                tipoCaseDetectado = ObtenerTipoVariable(valorCase);

                            // Validar compatibilidad de tipos entre el switch y el case
                            if (!string.IsNullOrEmpty(tipoExpresionSwitch) && !string.IsNullOrEmpty(tipoCaseDetectado))
                            {
                                // Normalizar tipos equivalentes (ej: pr19 o int)
                                string tipoSwitchNorm = NormalizarTipo(tipoExpresionSwitch, "");
                                string tipoCaseNorm = NormalizarTipo(tipoCaseDetectado, "");

                                if (tipoSwitchNorm == "int" && (tipoCaseNorm == "string" || tipoCaseNorm == "float"))
                                {
                                    ImprimirErrorSemantico(tokenActualSwitch.Linea, $"Error Semántico: No se puede convertir un valor de tipo '{tipoCaseNorm}' al tipo '{tipoSwitchNorm}' en el case '{valorCase}'.");
                                    erroresSemanticos++;
                                }
                                else if (tipoSwitchNorm == "string" && tipoCaseNorm != "string")
                                {
                                    ImprimirErrorSemantico(tokenActualSwitch.Linea, $"Error Semántico: El case '{valorCase}' no coincide con el tipo 'string' de la expresión del switch.");
                                    erroresSemanticos++;
                                }
                            }

                            // Validar casos duplicados
                            if (casosRegistrados.Contains(valorCase))
                            {
                                ImprimirErrorSemantico(tokenActualSwitch.Linea, $"Error Semántico: El valor de caso '{valorCase}' está duplicado en la instrucción switch.");
                                erroresSemanticos++;
                            }
                            else
                            {
                                casosRegistrados.Add(valorCase);
                            }
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

                            // Validación de desbordamiento (Overflow) en el resultado de expresiones compuestas evaluadas
                            try
                            {
                                string exprParaEvaluar = string.Join(" ", expTokens.Select(x => x.Lexema));
                                exprParaEvaluar = Regex.Replace(exprParaEvaluar, @"\$[a-zA-Z0-9_]+", m =>
                                {
                                    if (tablaSimbolos.ContainsKey(m.Value) && tablaSimbolos[m.Value].Valor != null)
                                    {
                                        string v = tablaSimbolos[m.Value].Valor.ToString();
                                        if (double.TryParse(v, out _)) return v;
                                    }
                                    return m.Value;
                                });

                                if (Regex.IsMatch(exprParaEvaluar, @"^[0-9\.\s\+\-\*\/\(\)]+$"))
                                {
                                    DataTable dtCalc = new DataTable();
                                    var calcResult = dtCalc.Compute(exprParaEvaluar, "");
                                    if (calcResult != null && double.TryParse(calcResult.ToString(), out double resNum))
                                    {
                                        if (resNum < int.MinValue || resNum > int.MaxValue)
                                        {
                                            ImprimirErrorSemantico(actual.Linea, $"Error de Desbordamiento (Overflow): El resultado de la expresión ({resNum}) excede los límites permitidos para un entero de 32 bits [{int.MinValue} a {int.MaxValue}].");
                                            erroresSemanticos++;
                                        }
                                    }
                                }
                            }
                            catch { }

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
            if (token == "PR02" || token == "PRO2" || lexema == "boolean" || lexema == "bool") return "boolean";
            if (token == "PR11" || token == "PR13" || lexema == "float" || lexema == "double") return "float";
            if (token == "PR03" || token == "PRO3" || lexema == "char") return "char";
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

            if (tipoDato.Equals("int", StringComparison.OrdinalIgnoreCase) || tipoDato.Equals("pr19", StringComparison.OrdinalIgnoreCase))
            {
                if (!long.TryParse(tokenValor, out long valorLong) || valorLong < int.MinValue || valorLong > int.MaxValue)
                {
                    mensajeError = $"Error de Desbordamiento (Overflow): El valor entero '{tokenValor}' excede los límites permitidos para un entero de 32 bits [{int.MinValue} a {int.MaxValue}].";
                    return false;
                }
            }
            else if (tipoDato.Equals("float", StringComparison.OrdinalIgnoreCase) || tipoDato.Equals("pr11", StringComparison.OrdinalIgnoreCase) || tipoDato.Equals("pr13", StringComparison.OrdinalIgnoreCase))
            {
                if (!float.TryParse(tokenValor, out float valorFloat) || float.IsInfinity(valorFloat))
                {
                    mensajeError = $"Error de Desbordamiento (Overflow): El valor numérico '{tokenValor}' excede los límites permitidos para un flotante de 32 bits.";
                    return false;
                }
            }
            else if (tipoDato.Equals("double", StringComparison.OrdinalIgnoreCase))
            {
                if (!double.TryParse(tokenValor, out double valorDouble) || double.IsInfinity(valorDouble))
                {
                    mensajeError = $"Error de Desbordamiento (Overflow): El valor numérico '{tokenValor}' excede los límites permitidos para 'double'.";
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

        public void GenerarCodigoIntermedio()
        {
            codigoIntermedio.Clear();
            rtCodigoIntermedio.Clear();

            InstruccionP.ReiniciarContadorEtiquetas();
            int index = 0;
            List<InstruccionP> instruccionesGlobales = ProcesarBloqueTokens(ref index, listaTokensCompletos);
            codigoIntermedio.AddRange(instruccionesGlobales);

            foreach (var instruccion in codigoIntermedio)
            {
                rtCodigoIntermedio.Text += instruccion.ToString() + Environment.NewLine;
            }
        }

        private List<InstruccionP> ProcesarBloqueTokens(ref int index, List<(string Token, string Lexema, int Linea)> tokens)
        {
            List<InstruccionP> bloque = new List<InstruccionP>();

            while (index < tokens.Count)
            {
                var actual = tokens[index];

                if (actual.Lexema == "}" || actual.Lexema.ToLower() == "end" || actual.Lexema == "\n")
                {
                    if (actual.Lexema == "}")
                    {
                        index++;
                        return bloque; 
                    }
                    index++;
                    continue;
                }

                string lexemaMin = actual.Lexema.ToLower();

                // 1. DO-WHILE
                if (lexemaMin == "do")
                {
                    index++; 
                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++; 
                        List<InstruccionP> cuerpoDo = ProcesarBloqueTokens(ref index, tokens);

                        while (index < tokens.Count && tokens[index].Lexema.ToLower() != "while") index++;
                        if (index < tokens.Count && tokens[index].Lexema.ToLower() == "while")
                        {
                            index++; 
                            List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                            var instsDoWhile = InstruccionP.GenerarCodigoDoWhile(condTokens, cuerpoDo);
                            bloque.AddRange(instsDoWhile);
                        }
                    }
                }
                // 2. WHILE
                else if (lexemaMin == "while")
                {
                    index++;
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
                // 3. IF / IF-ELSE (Con detección robusta de ELSE y soporte para else{ pegado)
                else if (lexemaMin == "if")
                {
                    index++; 
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    List<InstruccionP> cuerpoIf = new List<InstruccionP>();
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        cuerpoIf = ProcesarBloqueTokens(ref index, tokens);
                    }

                    int lookAhead = index;
                    while (lookAhead < tokens.Count)
                    {
                        string lex = tokens[lookAhead].Lexema?.ToLower() ?? "";
                        if (lex == "" || lex == "\n" || lex == "\r" || lex == " ")
                        {
                            lookAhead++;
                            continue;
                        }
                        break;
                    }

                    bool tieneElse = false;
                    int elseIndex = -1;
                    if (lookAhead < tokens.Count)
                    {
                        string lex = tokens[lookAhead].Lexema?.ToLower() ?? "";
                        if (lex == "else" || lex.StartsWith("else"))
                        {
                            tieneElse = true;
                            elseIndex = lookAhead;
                        }
                    }

                    if (tieneElse)
                    {
                        index = elseIndex + 1;
                        bool llaveElseIncluida = tokens[elseIndex].Lexema.ToLower().Contains("{");

                        if (!llaveElseIncluida)
                        {
                            while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                        }

                        List<InstruccionP> cuerpoElse = new List<InstruccionP>();
                        if (index < tokens.Count && (tokens[index].Lexema == "{" || llaveElseIncluida))
                        {
                            if (!llaveElseIncluida) index++;
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
                // 4. SWITCH
                else if (lexemaMin == "switch")
                {
                    index++; 
                    List<string> exprTokens = ExtraerTokensParentesis(ref index, tokens);

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++; 

                        List<SwitchCaseNode> listaCasos = new List<SwitchCaseNode>();
                        SwitchCaseNode casoActual = null;

                        while (index < tokens.Count && tokens[index].Lexema != "}")
                        {
                            string lex = tokens[index].Lexema.ToLower();

                            if (lex == "case")
                            {
                                if (casoActual != null) listaCasos.Add(casoActual);

                                index++; 
                                string valCaso = tokens[index].Lexema;
                                index++;
                                if (tokens[index].Lexema == ":") index++;

                                casoActual = new SwitchCaseNode { ValorCaso = valCaso, Instrucciones = new List<InstruccionP>(), TieneBreak = false };
                            }
                            else if (lex == "default")
                            {
                                if (casoActual != null) listaCasos.Add(casoActual);

                                index++;
                                if (tokens[index].Lexema == ":") index++;

                                casoActual = new SwitchCaseNode { ValorCaso = null, Instrucciones = new List<InstruccionP>(), TieneBreak = false };
                            }
                            else if (lex == "break")
                            {
                                index++;
                                if (tokens[index].Lexema == ";") index++;
                                if (casoActual != null) casoActual.TieneBreak = true;
                            }
                            else if (lex == "print")
                            {
                                index++;
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
                            else if (tokens[index].Lexema == "=" || tokens[index].Token == "OPASIG" || (index + 1 < tokens.Count && tokens[index + 1].Lexema == "="))
                            {
                                int idxAsig = (tokens[index].Lexema == "=" || tokens[index].Token == "OPASIG") ? index : index + 1;
                                string varName = tokens[idxAsig - 1].Lexema;

                                List<string> asigTokens = new List<string>();
                                index = idxAsig + 1;
                                while (index < tokens.Count && tokens[index].Lexema != ";" && tokens[index].Lexema != "}")
                                {
                                    asigTokens.Add(tokens[index].Lexema);
                                    index++;
                                }
                                if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                                if (asigTokens.Count > 0 && casoActual != null)
                                {
                                    var instsAsig = InstruccionP.GenerarCodigoDinamico(varName, asigTokens);
                                    casoActual.Instrucciones.AddRange(instsAsig);
                                }
                            }
                            else
                            {
                                index++;
                            }
                        }
                        if (casoActual != null) listaCasos.Add(casoActual);
                        if (index < tokens.Count && tokens[index].Lexema == "}") index++;

                        var instsSwitch = InstruccionP.GenerarCodigoSwitch(exprTokens, listaCasos);
                        bloque.AddRange(instsSwitch);
                    }
                }
                // 5. FOR
                else if (lexemaMin == "for")
                {
                    index++;
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
                // 6. PRINT
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

                    bloque.Add(new InstruccionP("PRN", string.Join(" ", printTokens)));
                }
                // 7. INPUT
                else if (lexemaMin == "input")
                {
                    index++;
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
                // 8. ASIGNACIÓN O DECLARACIÓN CON ASIGNACIÓN
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
                index++;
                int parenCount = 1;
                while (index < tokens.Count)
                {
                    if (tokens[index].Lexema == "(") parenCount++;
                    else if (tokens[index].Lexema == ")")
                    {
                        parenCount--;
                        if (parenCount == 0)
                        {
                            index++;
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

        private void InicializarGridsIntermedios()
        {
            dtgCuadruplos.Columns.Clear();
            dtgCuadruplos.Columns.Add("Operador", "Operador");
            dtgCuadruplos.Columns.Add("Arg1", "Arg 1");
            dtgCuadruplos.Columns.Add("Arg2", "Arg 2");
            dtgCuadruplos.Columns.Add("Resultado", "Resultado");
            dtgCuadruplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dtgTriplos.Columns.Clear();
            dtgTriplos.Columns.Add("Indice", "#");
            dtgTriplos.Columns.Add("Operador", "Operador");
            dtgTriplos.Columns.Add("Arg1", "Arg 1");
            dtgTriplos.Columns.Add("Arg2", "Arg 2");
            dtgTriplos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }

        public void GenerarYMostrarRepresentacionesIntermedias()
        {
            InicializarGridsIntermedios();

            List<Cuadruplo> listaCuadruplos = new List<Cuadruplo>();
            List<Triplo> listaTriplos = new List<Triplo>();
            List<string> listaTAC = new List<string>();

            List<string> tokensEjemploExpr = new List<string>(); 

            int tempCount = 1;

            foreach (var token in listaTokensCompletos)
            {
                if (token.Lexema == "=")
                {
                    listaCuadruplos.Add(new Cuadruplo("+", "$SU", "$VA", $"t{tempCount}"));
                    listaCuadruplos.Add(new Cuadruplo("=", $"t{tempCount}", "-", "$SU"));

                    listaTriplos.Add(new Triplo("+", "$SU", "$VA"));
                    listaTriplos.Add(new Triplo("=", "$SU", "(0)"));

                    listaTAC.Add($"t{tempCount} = $SU + $VA");
                    listaTAC.Add($"$SU = t{tempCount}");

                    tempCount++;
                }
            }

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

            tokensEjemploExpr = new List<string> { "$SU", "+", "$VA" };

            rtNotacionInfija.Clear();
            rtNotacionInfija.Text = string.Join(" ", tokensEjemploExpr);

            List<string> postfija = TraductorNotaciones.InfijaAPostfija(tokensEjemploExpr);
            rtNotacionPostfija.Clear();
            rtNotacionPostfija.Text = string.Join(" ", postfija);

            List<string> prefija = TraductorNotaciones.InfijaAPrefija(tokensEjemploExpr);
            rtNotacionPrefija.Clear();
            rtNotacionPrefija.Text = string.Join(" ", prefija);

            rtCodigoTAC.Clear();
            rtCodigoTAC.Text = string.Join(Environment.NewLine, listaTAC);
        }

        public void GenerarTodoDinamico()
        {
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

            List<string> lineasInfijas = new List<string>();
            List<string> lineasPostfijas = new List<string>();
            List<string> lineasPrefijas = new List<string>();

            int index = 0;

            ProcesarBloqueDinamico(
                ref index,
                listaTokensCompletos,
                cuadruplosGlobales,
                triplosGlobales,
                tacGlobales,
                lineasInfijas,
                lineasPostfijas,
                lineasPrefijas
            );


            foreach (var c in cuadruplosGlobales)
            {
                dtgCuadruplos.Rows.Add(c.Operador, c.Arg1, c.Arg2, c.Resultado);
            }

            int idx = 0;
            foreach (var t in triplosGlobales)
            {
                dtgTriplos.Rows.Add($"({idx})", t.Operador, t.Arg1, t.Arg2);
                idx++;
            }

            rtCodigoTAC.Text = string.Join(Environment.NewLine, tacGlobales);

            if (rtNotacionInfija != null) rtNotacionInfija.Text = string.Join(Environment.NewLine, lineasInfijas);
            if (rtNotacionPostfija != null) rtNotacionPostfija.Text = string.Join(Environment.NewLine, lineasPostfijas);
            if (rtNotacionPrefija != null) rtNotacionPrefija.Text = string.Join(Environment.NewLine, lineasPrefijas);
        }

        private void ProcesarBloqueDinamico(
            ref int index,
            List<(string Token, string Lexema, int Linea)> tokens,
            List<Cuadruplo> cuadruplos,
            List<Triplo> triplos,
            List<string> tac,
            List<string> infija,
            List<string> postfija,
            List<string> prefija)
        {
            while (index < tokens.Count)
            {
                var actual = tokens[index];
                string lexemaMin = actual.Lexema.ToLower();

                if (actual.Lexema == "}" || lexemaMin == "end" || actual.Lexema == "\n" || actual.Lexema == ";")
                {
                    if (actual.Lexema == "}")
                    {
                        index++;
                        return;
                    }
                    index++;
                    continue;
                }

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

                        cuadruplos.Add(new Cuadruplo("=", resCompilacion.ResultadoCuadruplo, "-", varName));
                        triplos.Add(new Triplo("=", varName, resCompilacion.ResultadoTriplo));
                        tac.Add($"{varName} = {resCompilacion.ResultadoCuadruplo}");

                        List<string> tokensAsignacionCompleta = new List<string> { varName, "=" };
                        tokensAsignacionCompleta.AddRange(expTokens);
                        RegistrarNotacionesEx(tokensAsignacionCompleta, infija, postfija, prefija);
                    }
                    continue;
                }
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
                    continue;
                }
                else if (lexemaMin == "if")
                {
                    index++;
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);
                    RegistrarNotacionesEx(condTokens, infija, postfija, prefija);

                    var resCond = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                    string condRes = resCond.ResultadoCuadruplo;

                    string etiquetaFalsa = InstruccionP.ObtenerNuevaEtiqueta(); 
                    string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();  

                    tac.Add($"ifFalse {condRes} goto {etiquetaFalsa}");
                    cuadruplos.Add(new Cuadruplo("ifFalse", condRes, "-", etiquetaFalsa));
                    triplos.Add(new Triplo("ifFalse", condRes, etiquetaFalsa));

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac, infija, postfija, prefija);
                    }

                    int lookAhead = index;
                    while (lookAhead < tokens.Count)
                    {
                        string tLex = tokens[lookAhead].Lexema?.Trim().ToLower() ?? "";
                        string tTok = tokens[lookAhead].Token?.ToUpper() ?? "";

                        if (tLex == "" || tLex == "\n" || tLex == "\r" || tLex == "\r\n" ||
                            tTok.Contains("ESPACIO") || tTok.Contains("COMENTARIO") || tTok.Contains("NL") || tTok.Contains("WHITESPACE"))
                        {
                            lookAhead++;
                            continue;
                        }
                        break;
                    }

                    bool tieneElse = false;
                    bool llaveYaIncluida = false;

                    if (lookAhead < tokens.Count)
                    {
                        string lexL = tokens[lookAhead].Lexema?.Trim().ToLower() ?? "";
                        if (lexL == "else")
                        {
                            tieneElse = true;
                            index = lookAhead + 1;
                        }
                        else if (lexL.StartsWith("else") || lexL == "else{")
                        {
                            tieneElse = true;
                            llaveYaIncluida = true;
                            index = lookAhead + 1;
                        }
                    }

                    if (tieneElse)
                    {
                        tac.Add($"goto {etiquetaFin}");
                        cuadruplos.Add(new Cuadruplo("JUMP", etiquetaFin, "-", "-"));
                        triplos.Add(new Triplo("JUMP", etiquetaFin, "-"));

                        tac.Add($"{etiquetaFalsa}:");
                        cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFalsa, "-", "-"));
                        triplos.Add(new Triplo("LABEL", etiquetaFalsa, "-"));

                        if (!llaveYaIncluida)
                        {
                            while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                        }

                        if (index < tokens.Count && (tokens[index].Lexema == "{" || llaveYaIncluida))
                        {
                            if (!llaveYaIncluida) index++;
                            ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac, infija, postfija, prefija);
                        }

                        tac.Add($"{etiquetaFin}:");
                        cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFin, "-", "-"));
                        triplos.Add(new Triplo("LABEL", etiquetaFin, "-"));
                    }
                    else
                    {
                        tac.Add($"{etiquetaFalsa}:");
                        cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFalsa, "-", "-"));
                        triplos.Add(new Triplo("LABEL", etiquetaFalsa, "-"));
                    }
                    continue;
                }
                else if (lexemaMin == "while")
                {
                    index++;
                    List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);
                    RegistrarNotacionesEx(condTokens, infija, postfija, prefija);

                    string etiquetaInicio = InstruccionP.ObtenerNuevaEtiqueta();
                    string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();

                    tac.Add($"{etiquetaInicio}:");
                    cuadruplos.Add(new Cuadruplo("LABEL", etiquetaInicio, "-", "-"));
                    triplos.Add(new Triplo("LABEL", etiquetaInicio, "-"));

                    var resCond = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                    string condRes = resCond.ResultadoCuadruplo;

                    tac.Add($"ifFalse {condRes} goto {etiquetaFin}");
                    cuadruplos.Add(new Cuadruplo("ifFalse", condRes, "-", etiquetaFin));
                    triplos.Add(new Triplo("ifFalse", condRes, etiquetaFin));

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac, infija, postfija, prefija);
                    }

                    tac.Add($"goto {etiquetaInicio}");
                    cuadruplos.Add(new Cuadruplo("JUMP", etiquetaInicio, "-", "-"));
                    triplos.Add(new Triplo("JUMP", etiquetaInicio, "-"));

                    tac.Add($"{etiquetaFin}:");
                    cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFin, "-", "-"));
                    triplos.Add(new Triplo("LABEL", etiquetaFin, "-"));
                    continue;
                }
                else if (lexemaMin == "do")
                {
                    index++;
                    string etiquetaInicio = InstruccionP.ObtenerNuevaEtiqueta();

                    tac.Add($"{etiquetaInicio}:");
                    cuadruplos.Add(new Cuadruplo("LABEL", etiquetaInicio, "-", "-"));
                    triplos.Add(new Triplo("LABEL", etiquetaInicio, "-"));

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac, infija, postfija, prefija);
                    }

                    while (index < tokens.Count && tokens[index].Lexema.ToLower() != "while") index++;
                    if (index < tokens.Count && tokens[index].Lexema.ToLower() == "while")
                    {
                        index++;
                        List<string> condTokens = ExtraerTokensParentesis(ref index, tokens);
                        RegistrarNotacionesEx(condTokens, infija, postfija, prefija);

                        var resCond = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                        string condRes = resCond.ResultadoCuadruplo;

                        tac.Add($"ifTrue {condRes} goto {etiquetaInicio}");
                        cuadruplos.Add(new Cuadruplo("ifTrue", condRes, "-", etiquetaInicio));
                        triplos.Add(new Triplo("ifTrue", condRes, etiquetaInicio));
                    }
                    if (index < tokens.Count && tokens[index].Lexema == ";") index++;
                    continue;
                }
                else if (lexemaMin == "for")
                {
                    index++;
                    List<string> forTokens = ExtraerTokensParentesis(ref index, tokens);

                    List<string> initTokens = new List<string>();
                    List<string> condTokens = new List<string>();
                    List<string> incTokens = new List<string>();

                    int parte = 0;
                    foreach (var t in forTokens)
                    {
                        if (t == ";") { parte++; continue; }
                        if (parte == 0) initTokens.Add(t);
                        else if (parte == 1) condTokens.Add(t);
                        else if (parte == 2) incTokens.Add(t);
                    }

                    if (initTokens.Count > 0)
                    {
                        var resInit = CompiladorExpresiones.Compilar(initTokens.Skip(2).ToList(), cuadruplos, triplos, tac);
                        string varI = initTokens[1];
                        cuadruplos.Add(new Cuadruplo("=", resInit.ResultadoCuadruplo, "-", varI));
                        triplos.Add(new Triplo("=", varI, resInit.ResultadoTriplo));
                        tac.Add($"{varI} = {resInit.ResultadoCuadruplo}");
                    }

                    string etiquetaInicio = InstruccionP.ObtenerNuevaEtiqueta();
                    string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();

                    tac.Add($"{etiquetaInicio}:");
                    cuadruplos.Add(new Cuadruplo("LABEL", etiquetaInicio, "-", "-"));
                    triplos.Add(new Triplo("LABEL", etiquetaInicio, "-"));

                    if (condTokens.Count > 0)
                    {
                        var resCond = CompiladorExpresiones.Compilar(condTokens, cuadruplos, triplos, tac);
                        RegistrarNotacionesEx(condTokens, infija, postfija, prefija);

                        tac.Add($"ifFalse {resCond.ResultadoCuadruplo} goto {etiquetaFin}");
                        cuadruplos.Add(new Cuadruplo("ifFalse", resCond.ResultadoCuadruplo, "-", etiquetaFin));
                        triplos.Add(new Triplo("ifFalse", resCond.ResultadoCuadruplo, etiquetaFin));
                    }

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;
                        ProcesarBloqueDinamico(ref index, tokens, cuadruplos, triplos, tac, infija, postfija, prefija);
                    }

                    if (incTokens.Count > 0)
                    {
                        int idxAsigInc = incTokens.IndexOf("=");
                        if (idxAsigInc != -1 && idxAsigInc > 0)
                        {
                            string varNameInc = incTokens[idxAsigInc - 1];
                            List<string> exprInc = incTokens.Skip(idxAsigInc + 1).ToList();

                            var resInc = CompiladorExpresiones.Compilar(exprInc, cuadruplos, triplos, tac);
                            cuadruplos.Add(new Cuadruplo("=", resInc.ResultadoCuadruplo, "-", varNameInc));
                            triplos.Add(new Triplo("=", varNameInc, resInc.ResultadoTriplo));
                            tac.Add($"{varNameInc} = {resInc.ResultadoCuadruplo}");

                            List<string> tokensIncCompleta = new List<string> { varNameInc, "=" };
                            tokensIncCompleta.AddRange(exprInc);
                            RegistrarNotacionesEx(tokensIncCompleta, infija, postfija, prefija);
                        }
                        else
                        {
                            CompiladorExpresiones.Compilar(incTokens, cuadruplos, triplos, tac);
                            RegistrarNotacionesEx(incTokens, infija, postfija, prefija);
                        }
                    }

                    tac.Add($"goto {etiquetaInicio}");
                    cuadruplos.Add(new Cuadruplo("JUMP", etiquetaInicio, "-", "-"));
                    triplos.Add(new Triplo("JUMP", etiquetaInicio, "-"));

                    tac.Add($"{etiquetaFin}:");
                    cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFin, "-", "-"));
                    triplos.Add(new Triplo("LABEL", etiquetaFin, "-"));
                    continue;
                }
                else if (lexemaMin == "switch")
                {
                    index++;
                    List<string> exprTokens = ExtraerTokensParentesis(ref index, tokens);
                    RegistrarNotacionesEx(exprTokens, infija, postfija, prefija);

                    var resExpr = CompiladorExpresiones.Compilar(exprTokens, cuadruplos, triplos, tac);
                    string exprRes = resExpr.ResultadoCuadruplo;

                    while (index < tokens.Count && tokens[index].Lexema != "{") index++;
                    if (index < tokens.Count && tokens[index].Lexema == "{")
                    {
                        index++;

                        string etiquetaFin = InstruccionP.ObtenerNuevaEtiqueta();
                        string etiquetaDefault = null;

                        List<(string ValorCaso, string EtiquetaCuerpo, bool TieneBreak, List<string> ContenidoPrint)> casos = new List<(string, string, bool, List<string>)>();

                        string valActual = null;
                        List<string> printsActuales = new List<string>();
                        bool breakActual = false;
                        bool esDefault = false;

                        while (index < tokens.Count && tokens[index].Lexema != "}")
                        {
                            string lex = tokens[index].Lexema.ToLower();

                            if (lex == "case")
                            {
                                if (valActual != null || esDefault)
                                {
                                    string etq = esDefault ? etiquetaDefault : InstruccionP.ObtenerNuevaEtiqueta();
                                    casos.Add((valActual, etq, breakActual, printsActuales));
                                    printsActuales = new List<string>();
                                    breakActual = false;
                                }
                                esDefault = false;
                                index++;

                                List<string> tokensVal = new List<string>();
                                while (index < tokens.Count && tokens[index].Lexema != ":" && tokens[index].Lexema != "{")
                                {
                                    tokensVal.Add(tokens[index].Lexema);
                                    index++;
                                }
                                valActual = string.Join(" ", tokensVal);
                                RegistrarNotacionesEx(tokensVal, infija, postfija, prefija);

                                if (index < tokens.Count && tokens[index].Lexema == ":") index++;
                            }
                            else if (lex == "default")
                            {
                                if (valActual != null || esDefault)
                                {
                                    string etq = esDefault ? etiquetaDefault : InstruccionP.ObtenerNuevaEtiqueta();
                                    casos.Add((valActual, etq, breakActual, printsActuales));
                                    printsActuales = new List<string>();
                                    breakActual = false;
                                }
                                valActual = null;
                                esDefault = true;
                                etiquetaDefault = InstruccionP.ObtenerNuevaEtiqueta();
                                index++;
                                if (index < tokens.Count && tokens[index].Lexema == ":") index++;
                            }
                            else if (lex == "break")
                            {
                                breakActual = true;
                                index++;
                                if (index < tokens.Count && tokens[index].Lexema == ";") index++;
                            }
                            else if (lex == "print")
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
                                printsActuales.Add(contenido);
                            }
                            else if (tokens[index].Lexema == "=" || tokens[index].Token == "OPASIG" || (index + 1 < tokens.Count && tokens[index + 1].Lexema == "="))
                            {
                                int idxAsig = (tokens[index].Lexema == "=" || tokens[index].Token == "OPASIG") ? index : index + 1;
                                string varName = tokens[idxAsig - 1].Lexema;

                                List<string> expToks = new List<string>();
                                index = idxAsig + 1;
                                while (index < tokens.Count && tokens[index].Lexema != ";" && tokens[index].Lexema != ",")
                                {
                                    expToks.Add(tokens[index].Lexema);
                                    index++;
                                }
                                if (index < tokens.Count && tokens[index].Lexema == ";") index++;

                                if (expToks.Count > 0)
                                {
                                    var resComp = CompiladorExpresiones.Compilar(expToks, cuadruplos, triplos, tac);
                                    cuadruplos.Add(new Cuadruplo("=", resComp.ResultadoCuadruplo, "-", varName));
                                    triplos.Add(new Triplo("=", varName, resComp.ResultadoTriplo));
                                    tac.Add($"{varName} = {resComp.ResultadoCuadruplo}");
                                }
                            }
                            else
                            {
                                index++;
                            }
                        }

                        if (valActual != null || esDefault)
                        {
                            string etqFinal = esDefault ? etiquetaDefault : InstruccionP.ObtenerNuevaEtiqueta();
                            casos.Add((valActual, etqFinal, breakActual, printsActuales));
                        }

                        if (index < tokens.Count && tokens[index].Lexema == "}") index++;

                        foreach (var c in casos)
                        {
                            if (c.ValorCaso != null)
                            {
                                var resCmp = CompiladorExpresiones.Compilar(new List<string> { exprRes, "==", c.ValorCaso }, cuadruplos, triplos, tac);
                                tac.Add($"ifTrue {resCmp.ResultadoCuadruplo} goto {c.EtiquetaCuerpo}");
                                cuadruplos.Add(new Cuadruplo("ifTrue", resCmp.ResultadoCuadruplo, "-", c.EtiquetaCuerpo));
                                triplos.Add(new Triplo("ifTrue", resCmp.ResultadoCuadruplo, c.EtiquetaCuerpo));
                            }
                        }

                        string saltoDestino = etiquetaDefault ?? etiquetaFin;
                        tac.Add($"goto {saltoDestino}");
                        cuadruplos.Add(new Cuadruplo("JUMP", saltoDestino, "-", "-"));
                        triplos.Add(new Triplo("JUMP", saltoDestino, "-"));

                        foreach (var c in casos)
                        {
                            tac.Add($"{c.EtiquetaCuerpo}:");
                            cuadruplos.Add(new Cuadruplo("LABEL", c.EtiquetaCuerpo, "-", "-"));
                            triplos.Add(new Triplo("LABEL", c.EtiquetaCuerpo, "-"));

                            foreach (var p in c.ContenidoPrint)
                            {
                                cuadruplos.Add(new Cuadruplo("PRINT", p, "-", "-"));
                                triplos.Add(new Triplo("PRINT", p, "-"));
                                tac.Add($"print {p}");
                            }

                            if (c.TieneBreak)
                            {
                                tac.Add($"goto {etiquetaFin}");
                                cuadruplos.Add(new Cuadruplo("JUMP", etiquetaFin, "-", "-"));
                                triplos.Add(new Triplo("JUMP", etiquetaFin, "-"));
                            }
                        }

                        tac.Add($"{etiquetaFin}:");
                        cuadruplos.Add(new Cuadruplo("LABEL", etiquetaFin, "-", "-"));
                        triplos.Add(new Triplo("LABEL", etiquetaFin, "-"));
                    }
                    continue;
                }
                else
                {
                    index++;
                }
            }
        }

        private void RegistrarNotacionesEx(List<string> expTokens, List<string> infija, List<string> postfija, List<string> prefija)
        {
            if (expTokens == null || expTokens.Count == 0) return;
            var post = TraductorNotaciones.InfijaAPostfija(expTokens);
            var pre = TraductorNotaciones.InfijaAPrefija(expTokens);

            infija.Add(string.Join(" ", expTokens));
            postfija.Add(string.Join(" ", post));
            prefija.Add(string.Join(" ", pre));
        }



        private void lblStatusLeft_Click(object sender, EventArgs e)
        {

        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }   
}