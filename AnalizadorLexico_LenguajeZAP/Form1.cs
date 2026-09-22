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
        public void ConfigurarNumeracion()
        {
            rTxtCodigoFuente.TextChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.VScroll += ActualizarNumerosLinea;
            rTxtCodigoFuente.SelectionChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.Resize += ActualizarNumerosLinea;

            rTxtNumeros.Font = rTxtCodigoFuente.Font;
            rTxtNumeros.SelectionAlignment = HorizontalAlignment.Right;
            rTxtNumeros.ScrollBars = RichTextBoxScrollBars.None;

            // Eventos para el RichTextBox de Tokens
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

            // Si no hay tokens generados, mostramos el 1
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
            // Guardamos donde estaba el usuario para que no salte el cursor
            int posicionOriginal = rtb.SelectionStart;

            // El texto completo de los tokens
            string contenido = rtb.Text;

            // Buscamos cada token separado por espacios o saltos de línea
            string[] tokens = contenido.Split(new char[] { ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            int busquedaDesde = 0;
            foreach (string t in tokens)
            {
                if (t.StartsWith("ER"))
                {
                    // Buscamos la posición exacta de la palabra "ERxx"
                    int inicio = rtb.Find(t, busquedaDesde, RichTextBoxFinds.WholeWord);

                    if (inicio != -1)
                    {
                        rtb.Select(inicio, t.Length);
                        rtb.SelectionColor = Color.Red;
                        rtb.SelectionFont = new Font(rtb.Font, FontStyle.Bold); // Lo ponemos en negrita también

                        // Actualizamos para no buscar siempre desde el principio
                        busquedaDesde = inicio + t.Length;
                    }
                }
            }

            // Al terminar, regresamos el color a negro y el cursor a su lugar
            rtb.SelectionStart = posicionOriginal;
            rtb.SelectionLength = 0;
            rtb.SelectionColor = Color.Black;
        }

        //Conexion de SQL Server a la base de datos donde se encuentra la matriz de transicion
        string connectionString = "Server=DACZ-1225; Database=ZAP; Integrated Security=True; TrustServerCertificate=True;";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Configuramos la numeracion del editor de texto al cargar el ejecutable
            ConfigurarNumeracion();
        }

        private void btnAnalizarCodigo_Click(object sender, EventArgs e)
        {
            // Limpiamos todo antes de empezar
            dtgTablaSimbolos.Rows.Clear();
            tablaSimbolos.Clear();
            contadorID = 1;
            rTxtTokens.Clear();
            rTxtErrores.Clear();
            int contadorGlobal = 0;
            DataTable matriz = ObtenerMatriz();
            // Encabezado para la lista de errores
            rTxtErrores.SelectionFont = new Font(rTxtErrores.Font, FontStyle.Bold);
            rTxtErrores.AppendText("LÍNEA\tERROR" + Environment.NewLine);
            rTxtErrores.AppendText("----------------------------------------------------------" + Environment.NewLine);

            // Obtenemos las líneas del editor
            string[] lineas = rTxtCodigoFuente.Lines;

            for (int i = 0; i < lineas.Length; i++)
            {
                // Mandamos a llamar el metodo que generará el archivo de tokens y los errores que puedan presentarse
                string lineaDeTokens = ProcesarLineaParaTokens(matriz, lineas[i], i + 1, rTxtErrores, ref contadorGlobal);

                // Escribimos en el archivo de tokens manteniendo la estructura de renglones
                rTxtTokens.AppendText(lineaDeTokens + Environment.NewLine);
            }
            AplicarColorErrores(rTxtTokens);
            // Pie de reporte con el total
            rTxtErrores.AppendText(Environment.NewLine + "----------------------------------------------------------" + Environment.NewLine);
            rTxtErrores.SelectionFont = new Font(rTxtErrores.Font, FontStyle.Bold);
            rTxtErrores.AppendText("TOTAL DE ERRORES: " + contadorGlobal);
            if (contadorGlobal == 0)
            {
                bool resultadoSintactico;
                resultadoSintactico=AnalizadorSintactico();

                if (resultadoSintactico)
                {
                   AnalizadorSemantico();
                }

            }
        }        

        //Metodo para el editor de codigo fuente para evitar que se cambie la fuente de texto en caso de que se copie de un texto externo
        private void PegarTextoPlano()
        {
            if (Clipboard.ContainsText())
            {
                string texto = Clipboard.GetText();

                int inicio = rTxtCodigoFuente.SelectionStart;

                rTxtCodigoFuente.SelectedText = texto;

                // Aplicar fuente solo al texto pegado
                rTxtCodigoFuente.Select(inicio, texto.Length);
                rTxtCodigoFuente.SelectionFont = new Font("Consolas", 10.2f);

                // Restaurar cursor
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

        //Metodo para obtener la matriz de transicion desde la base de datos.
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

        //Metodo para recorrer la matriz de transicion.
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

            // Forzamos un último salto usando la columna "FDC" de tu base de datos
            estadoActual = MoverSiguienteEstado(matriz, estadoActual, "FDC", estadosError);

            //Se obtiene el token o error correspondiente a la cadena leida
            return ObtenerTokenOError(matriz, estadoActual);
        }

        //Metodo que nos ayuda a realizar los movimientos de estado en la matriz de transicion
        private int MoverSiguienteEstado(DataTable matriz, int estado, string columna, int[] errores)
        {
            // Buscamos la fila del estado actual
            DataRow[] filas = matriz.Select("F1 = " + estado);

            if (filas.Length > 0 && matriz.Columns.Contains(columna))
            {
                object valor = filas[0][columna];
                if (valor != DBNull.Value)
                {
                    return Convert.ToInt32(valor);
                }
            }
            return errores[0]; // Si no hay camino, mandamos al primer estado de error
        }


        //Metodo que nos devolvera el token o error de la cadena leida previamente
        private string ObtenerTokenOError(DataTable matriz, int estado)
        {
            DataRow[] filas = matriz.Select("F1 = " + estado);
            if (filas.Length > 0 && filas[0]["ACEPTA"] != DBNull.Value)
            {
                return filas[0]["ACEPTA"].ToString();
            }
            return "CADENA_NO_VALIDA";
        }

        // Metodo auxiliar para algunos caracteres en la base de datos SQL
        private string ObtenerNombreColumnaSQL(char c)
        {
            if (char.IsUpper(c))
            {
                return c.ToString() + "1";
            }
            
            switch (c)
            {
                case '.':
                    return "#1";
                case ',':
                    return ".";
                case '[':
                    return "(1";
                case ']':
                    return ")1";
                case '!':
                    return "_1";

                default:
                    return c.ToString();
            }
        }
        /*-----------------------------ARCHIVO DE TOKENS-----------------------------*/
        //Metodo que genera el archivo de tokens asi como detecta los errores que se puedan presentar
        private string ProcesarLineaParaTokens(DataTable matriz, string textoLinea, int numLinea, RichTextBox rtbErrores, ref int totalErrores)
        {
            if (string.IsNullOrWhiteSpace(textoLinea)) return "";

            StringBuilder sbLinea = new StringBuilder();
            string acumulador = "";
            // Añadimos un espacio al final de la línea para procesar el último lexema
            string lineaConEspacio = textoLinea + " ";

            for (int i = 0; i < lineaConEspacio.Length; i++)
            {
                char c = lineaConEspacio[i];
                if (c == ',') // Detectamos la coma
                {
                    // Si hay algo previo a validar
                    if (acumulador.Length > 0)
                    {
                        // 1. Probamos primero de forma limpia en la Base de Datos (ideal para palabras reservadas)
                        string resPrevio = ValidarCadena(matriz, acumulador);

                        // 2. Si da error o es un IDEN genérico, aplicamos el rescate manual
                        if (resPrevio.StartsWith("ER") || resPrevio == "CADENA_NO_VALIDA" || resPrevio == "IDEN")
                        {
                            if (acumulador.StartsWith("$"))
                            {
                                resPrevio = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                            }
                            else if (char.IsDigit(acumulador[0]))
                            {
                                resPrevio = "CONENTERO";
                            }
                        }

                        VerificarSiEsError(resPrevio, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resPrevio + " ");
                        acumulador = "";
                    }

                    // Procesamos la coma de forma segura
                    string resComa = ValidarCadena(matriz, ",");
                    if (resComa.StartsWith("ER") || resComa == "CADENA_NO_VALIDA" || string.IsNullOrEmpty(resComa))
                    {
                        resComa = "CS13";
                    }

                    sbLinea.Append(resComa + " ");
                    continue;
                }
                //Espacios en blanco
                else if (c == ' ' || c == '\t')
                {
                    if (acumulador.Length > 0)
                    {
                        string resultado = ValidarCadena(matriz, acumulador);

                        if (resultado == "IDEN")
                        {
                            // Utilizamos la tabla de simbolos para verificar si ya existe el identificador
                            resultado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                        }

                        // Verificamos si es un error antes de agregarlo al archivo de tokens
                        VerificarSiEsError(resultado, acumulador, numLinea, rtbErrores, ref totalErrores);

                        sbLinea.Append(resultado + " ");
                        acumulador = "";
                    }
                    sbLinea.Append(c);
                }
                //Cadenas
                else if (c == '"')
                {
                    string cadena = "";
                    i++; // Avanzar para saltar la comilla inicial
                    bool seCerraronComillas = false;

                    while (i < lineaConEspacio.Length)
                    {
                        if (lineaConEspacio[i] == '"')
                        {
                            seCerraronComillas = true;
                            break; // Encontró la comilla de cierre legítima
                        }
                        cadena += lineaConEspacio[i];
                        i++;
                    }

                    string resCadena;

                    if (seCerraronComillas)
                    {
                        // 1. Le preguntamos a la base de datos pasando las comillas reales
                        resCadena = ValidarCadena(matriz, "\"" + cadena + "\"");

                        // 2. Si la base de datos falla pero sabemos que estructuralmente está bien cerrada,
                        // puedes usar tu respaldo manual (cambia "CAD" por el código exacto que uses si no es ese)
                        if (resCadena.StartsWith("ER") || resCadena == "CADENA_NO_VALIDA" || string.IsNullOrEmpty(resCadena))
                        {
                            resCadena = "CAD";
                        }
                    }
                    else
                    {
                        // ERROR: La línea terminó y nunca se pusieron las comillas de cierre
                        resCadena = "ER02 \"Error: Cadena constante sin cerrar\"";
                    }

                    // Pasamos el resultado por tu verificador de errores
                    VerificarSiEsError(resCadena, "\"" + cadena + (seCerraronComillas ? "\"" : ""), numLinea, rtbErrores, ref totalErrores);

                    // Agregamos el token resultante al registro
                    sbLinea.Append(resCadena + " ");
                    continue;
                }
                else if ("+-".Contains(c.ToString()))
                {
                    bool esExponencial = acumulador.Length > 0 &&
                         acumulador.ToUpper().EndsWith("E") &&
                         char.IsDigit(acumulador[0]);

                    if (esExponencial)
                    {
                        acumulador += c;
                    }
                    else
                    {
                        if (acumulador.Length > 0)
                        {
                            // 1. Intentamos evaluar la palabra limpia
                            string resPrev = ValidarCadena(matriz, acumulador);

                            // 2. Si falla, probamos con el espacio simulado
                            if (resPrev.StartsWith("ER") || resPrev == "CADENA_NO_VALIDA" || resPrev == "IDEN")
                            {
                                string intentoConEspacio = ValidarCadena(matriz, acumulador + " ");
                                if (!intentoConEspacio.StartsWith("ER") && intentoConEspacio != "CADENA_NO_VALIDA")
                                {
                                    resPrev = intentoConEspacio;
                                }
                                else if (acumulador.StartsWith("$"))
                                {
                                    resPrev = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                                }
                                else if (char.IsDigit(acumulador[0]))
                                {
                                    resPrev = "CONENTERO";
                                }
                            }

                            if (resPrev == "IDEN")
                            {
                                resPrev = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                            }

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
                //Caracteres Especiales
                else if ("()[]{};*/!<>=:".Contains(c.ToString()))
                {
                    if (acumulador.Length > 0)
                    {
                        // 1. Intentamos evaluar la palabra limpia (Ideal para palabras reservadas como 'if')
                        string resAcumulado = ValidarCadena(matriz, acumulador);

                        // 2. Si da error o es un IDEN genérico, probamos simulando el espacio (Ideal para 'switch', '$i', '5')
                        if (resAcumulado.StartsWith("ER") || resAcumulado == "CADENA_NO_VALIDA" || resAcumulado == "IDEN")
                        {
                            string intentoConEspacio = ValidarCadena(matriz, acumulador + " ");

                            // Si con espacio la BD responde con una Palabra Reservada (ej: PRXX) o un token válido, lo usamos
                            if (!intentoConEspacio.StartsWith("ER") && intentoConEspacio != "CADENA_NO_VALIDA")
                            {

                                resAcumulado = intentoConEspacio;
                            }
                            // Si sigue dando problemas pero sabemos qué es por sus caracteres iniciales, lo rescatamos manualmente
                            else if (acumulador.StartsWith("$"))
                            {
                                resAcumulado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                            }
                            else if (char.IsDigit(acumulador[0]))
                            {
                                resAcumulado = "CONENTERO";
                            }
                        }

                        // Si después de todo el proceso se resolvió como IDEN puro de la matriz, lo indexamos
                        if (resAcumulado == "IDEN")
                        {
                            resAcumulado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                        }

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
                    if (lexemaEspecial == ":" && (resEspecial.StartsWith("ER") || resEspecial == "CADENA_NO_VALIDA"))
                    {
                        resEspecial = "CS16"; // Usa aquí el código de token que le corresponda a los dos puntos (ej: CS16 o similar)
                    }
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


        /*-----------------------------FIN ARCHIVO DE TOKENS-----------------------------*/

        /*-----------------------------MANEJO DE ERRORES-----------------------------*/

        private void VerificarSiEsError(string resultado, string lexema, int linea, RichTextBox rtbErrores, ref int total)
        {

            bool esError = resultado.StartsWith("ER") ||
                           resultado.Equals("CADENA_NO_VALIDA");

            if (resultado == "CONENTERO" || resultado == "IDEN" || resultado.StartsWith("PR"))
            {
                esError = false;
            }

            if (esError)
            {
                total++;
                string entrada = string.Format("{0}\t{1} (Lexema: '{2}'){3}",
                                               linea, resultado, lexema, Environment.NewLine);

                int inicio = rtbErrores.TextLength;
                rtbErrores.AppendText(entrada);
                rtbErrores.Select(inicio, entrada.Length);
                rtbErrores.SelectionColor = Color.Red;
                rtbErrores.DeselectAll();
                rtbErrores.SelectionColor = Color.Black;

            }
        }

        /*-----------------------------FIN DE MANEJO DE ERRORES-----------------------------*/

        /*-----------------------------TABLA DE SIMBOLOS-----------------------------*/
        public class Simbolo
        {
            public int Id { get; set; }
            public string Lexema { get; set; }
            public string TipoDato { get; set; }  // "entero", "flotante", etc.
            public string Categoria { get; set; } // "Variable", "Constante"
            public object Valor { get; set; }     // Valor asignado en ejecución/evaluación
        }
        Dictionary<string, Simbolo> tablaSimbolos = new Dictionary<string, Simbolo>();
        int contadorID = 1;

        private string ObtenerTokenIdentificador(string lexema, DataGridView dgvSimbolos)
        {
            // Si ya existe en la tabla, devolvemos su ID asignado
            if (tablaSimbolos.ContainsKey(lexema))
            {
                return "IDEN" + tablaSimbolos[lexema].Id;
            }

            int nuevoID = contadorID++;
            Simbolo nuevoSimbolo = new Simbolo
            {
                Id = nuevoID,
                Lexema = lexema,
            };

            tablaSimbolos.Add(lexema, nuevoSimbolo);

            // Reflejar en la interfaz gráfica (DataGridView)
            dgvSimbolos.Rows.Add(nuevoSimbolo.Id, nuevoSimbolo.Lexema, nuevoSimbolo.TipoDato, nuevoSimbolo.Categoria, nuevoSimbolo.Valor);

            return "IDEN" + nuevoID;
        }



        //Metodo para guardar el archivo de codigo fuente
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

        //Metodo para cargar el archivo de codigo fuente
        private void btnCargarPrograma_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Filtros para que el usuario solo vea archivos de texto o con la extensión de tu lenguaje
            openFileDialog.Filter = "Archivos ZAP (*.zap)|*.zap|Archivos de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*";
            openFileDialog.Title = "Seleccionar código fuente ZAP";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
               
                    string contenido = File.ReadAllText(openFileDialog.FileName);

                
                    rTxtCodigoFuente.Text = contenido;
                    rTxtCodigoFuente.ReadOnly=true;
                    rTxtCodigoFuente.BackColor = SystemColors.ControlLight;
                    MessageBox.Show("Archivo cargado correctamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al cargar el archivo: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        //Metodo para guardar el archivo de tokens
        private void btnGuardarArchivoTokens_Click(object sender, EventArgs e)
        {
            if (rTxtTokens.Text.Contains("ER0") || rTxtTokens.Text.Contains("ER1"))
            {
                MessageBox.Show("No se puede guardar el archivo de tokens porque contiene errores. Por favor, corrige los errores antes de guardar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
        //Metodo para editar el programa de codigo fuente
        private void btnEditarPrograma_Click(object sender, EventArgs e)
        {
            rTxtCodigoFuente.ReadOnly = false;
            rTxtCodigoFuente.BackColor = SystemColors.Window;
        }

        public bool AnalizadorSintactico()
        {
            listaTokensExtraidos.Clear();

            string contenidoActual = rTxtTokens.Text;

            string contenidoNormalizado = contenidoActual.Replace("\r\n", "\n").Replace("\r", "\n");
            contenidoNormalizado = contenidoNormalizado.Replace("\n", " \n ");

            string[] palabras = contenidoNormalizado.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string token in palabras)
            {
                if (token == "\n")
                {
                    listaTokensExtraidos.Add("\n");
                }
                else
                {
                    string tokenLimpio = token.Trim();
                    if (!string.IsNullOrEmpty(tokenLimpio))
                    {
                        listaTokensExtraidos.Add(tokenLimpio);
                    }
                }
            }
            ZapParsingResult resultado = PushdownParser.ExecuteParser(listaTokensExtraidos);
            rTxtErrores.Clear();
            if (resultado.Success)
            {
                rTxtErrores.SelectionColor = Color.Green;
                rTxtErrores.AppendText("¡Análisis sintáctico completado con éxito! Estructura válida.\n");
                MessageBox.Show("El código de tokens cumple perfectamente con la gramática ZAP.", "Análisis Correcto", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
        /*foreach (string paso in resultado.TraceSteps)
        {
            if (paso.StartsWith("[MATCH]"))
            {
                rTxtRecorrido.SelectionColor = Color.Blue;
            }
            else if (paso.Contains("-> ε"))
            {
                rTxtRecorrido.SelectionColor = Color.Gray;
            }
            else if (paso.StartsWith("[REGLA APLICADA]"))
            {
                rTxtRecorrido.SelectionColor = Color.DarkGreen;
            }
            else
            {
                rTxtRecorrido.SelectionColor = Color.Black;
            }

            rTxtRecorrido.AppendText(paso + Environment.NewLine);
        }*/
        //tabAnalizador.SelectedIndex = 1;

        public void AnalizadorSemantico()
        {
            string codigo = rTxtCodigoFuente.Text;

            string valorGlobal;
            string codigoLimpio = Regex.Replace(codigo, @"//.*", "");

            // 1. Patrón para declaraciones con asignación inicial (ej: int $A = 10; o double $B = 3.14;)
            string patronDeclaracionConValor = @"\b(int|double|float|string|char|bool)\b\s+(\$[a-zA-Z0-9_]+)\s*=\s*([^;]+);";

            // 2. Patrón para declaraciones sin inicializar (ej: int $A;)
            string patronDeclaracionSinValor = @"\b(int|double|float|string|char|bool)\b\s+(\$[a-zA-Z0-9_]+)\s*;";

            // 3. Patrón para reasignaciones posteriores (ej: $A = 20; o $SU = $SU + $VA;)
            string patronAsignacion = @"(\$[a-zA-Z0-9_]+)\s*=\s*([^;]+);";

            // --- A) Procesar Declaraciones con Inicialización (int $A = 10;) ---
            MatchCollection declaracionesConValor = Regex.Matches(codigoLimpio, patronDeclaracionConValor);
            foreach (Match m in declaracionesConValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;
                string valor = m.Groups[3].Value;

                if (tablaSimbolos.ContainsKey(iden))
                {
                    tablaSimbolos[iden].TipoDato = tipo;
                    tablaSimbolos[iden].Valor = valor;
                }
            }

            // --- B) Procesar Declaraciones Sin Valor Inicial (int $A;) ---
            MatchCollection declaracionesSinValor = Regex.Matches(codigoLimpio, patronDeclaracionSinValor);
            foreach (Match m in declaracionesSinValor)
            {
                string tipo = m.Groups[1].Value.ToLower();
                string iden = m.Groups[2].Value;

                if (tablaSimbolos.ContainsKey(iden))
                {
                    tablaSimbolos[iden].TipoDato = tipo;
                    if (tablaSimbolos[iden].Valor == null || tablaSimbolos[iden].Valor.ToString() == "Sin asignar")
                    {
                        tablaSimbolos[iden].Valor = "null";
                    }
                }
            }

            // --- C) Procesar Reasignaciones o Expresiones ($C = $C + 1;) ---
            MatchCollection asignaciones = Regex.Matches(codigoLimpio, patronAsignacion);
            foreach (Match m in asignaciones)
            {
                string iden = m.Groups[1].Value;
                string expresionOriginal = m.Groups[2].Value.Trim(); // Captura "$SU+$VA"

                if (tablaSimbolos.ContainsKey(iden))
                {
                    // EVALUAMOS LA EXPRESIÓN ANTES DE ASIGNARLA
                    string valorResuelto = EvaluarExpresion(expresionOriginal);

                    tablaSimbolos[iden].Valor = valorResuelto; // Guarda "3" en lugar de "$SU+$VA"
                }
            }
            ActualizarDataGrid();
        }

        private string EvaluarExpresion(string expresion)
        {
            if (string.IsNullOrWhiteSpace(expresion)) return "";

            // 1. Detectar si la expresión contiene comillas directamente (ej. "Hola" + "Mundo")
            bool esOperacionCadena = expresion.Contains("\"");

            // 2. Buscar y reemplazar todas las variables por sus valores actuales
            MatchCollection variables = Regex.Matches(expresion, @"\$[a-zA-Z0-9_]+");

            foreach (Match varMatch in variables)
            {
                string nombreVar = varMatch.Value;

                if (tablaSimbolos.ContainsKey(nombreVar))
                {
                    var simbolo = tablaSimbolos[nombreVar];
                    string valorActual = simbolo.Valor?.ToString() ?? "";

                    // Si la variable es de tipo string o su valor contiene comillas, es una operación de cadenas
                    if (simbolo.TipoDato == "string" || valorActual.StartsWith("\""))
                    {
                        esOperacionCadena = true;
                    }

                    // Si no tiene valor asignado aún
                    if (valorActual == "Sin asignar" || valorActual == "null")
                    {
                        valorActual = (simbolo.TipoDato == "string" || esOperacionCadena) ? "\"\"" : "0";
                    }

                    // Reemplazamos la variable en la expresión por su valor real
                    expresion = expresion.Replace(nombreVar, valorActual);
                }
            }

            // 3. SI ES UNA SUMA/CONCATENACIÓN DE CADENAS
            if (esOperacionCadena)
            {
                // Dividimos los términos por el operador '+'
                string[] partes = expresion.Split('+');
                string resultadoCadena = "";

                foreach (string parte in partes)
                {
                    // Limpiamos espacios laterales y removemos las comillas dobles extremas
                    string terminoLimpio = parte.Trim().Trim('"');
                    resultadoCadena += terminoLimpio;
                }

                // Retornamos el resultado delimitado entre comillas para la Tabla de Símbolos
                return $"\"{resultadoCadena}\"";
            }

            // 4. SI ES UNA OPERACIÓN NUMÉRICA (int/float/double)
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
            foreach (var item in tablaSimbolos.Values)
            {
                dtgTablaSimbolos.Rows.Add(item.Id, item.Lexema, item.TipoDato, item.Valor);
            }
        }
    }
}