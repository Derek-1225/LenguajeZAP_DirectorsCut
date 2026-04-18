using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Data.SqlClient; 

namespace AnalizadorLexico_LenguajeZAP
{
    public partial class Form1 : Form
    {
        public void ConfigurarNumeracion()
        {
            // Sincronizar eventos
            rTxtCodigoFuente.TextChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.VScroll += ActualizarNumerosLinea;
            rTxtCodigoFuente.SelectionChanged += ActualizarNumerosLinea;
            rTxtCodigoFuente.Resize += ActualizarNumerosLinea;

            // Configuración inicial de rtbNumeros
            rTxtNumeros.Font = rTxtCodigoFuente.Font;
            rTxtNumeros.SelectionAlignment = HorizontalAlignment.Right;
            rTxtNumeros.ScrollBars = RichTextBoxScrollBars.None;

            // Eventos para el RichTextBox de Tokens
            rTxtTokens.TextChanged += ActualizarNumerosTokens;
            rTxtTokens.VScroll += ActualizarNumerosTokens;
            rTxtTokens.Resize += ActualizarNumerosTokens;

            // Ajustes estéticos
            rTxtNumerosTokens.Font = rTxtTokens.Font;
            rTxtNumerosTokens.ReadOnly = true;
            rTxtNumerosTokens.SelectionAlignment = HorizontalAlignment.Right;
            rTxtNumerosTokens.BackColor = Color.LightGray; // Un color distinto para diferenciar
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
            // Calculamos líneas visibles del rtbTokens
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

        //Conexion de SQL Server a la base de datos donde se encuentra la matriz de transicion
        string connectionString = "Server=DACZ-1225; Database=ZAP; Integrated Security=True; TrustServerCertificate=True;";
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
                // LLAMADA AL MÉTODO QUE HACE EL ESPEJO Y BUSCA ERRORES AL MISMO TIEMPO
                // Pasamos i + 1 para que la primera línea sea la 1 y no la 0
                string lineaDeTokens = ProcesarLineaParaTokens(matriz, lineas[i], i + 1, rTxtErrores, ref contadorGlobal);

                // Escribimos en el archivo de tokens manteniendo la estructura de renglones
                rTxtTokens.AppendText(lineaDeTokens + Environment.NewLine);
            }

            // Pie de reporte con el total
            rTxtErrores.AppendText(Environment.NewLine + "----------------------------------------------------------" + Environment.NewLine);
            rTxtErrores.SelectionFont = new Font(rTxtErrores.Font, FontStyle.Bold);
            rTxtErrores.AppendText("TOTAL DE ERRORES: " + contadorGlobal);
        }        

        //Metodo para el editor de codigo fuente.
        private void PegarTextoPlano()
        {
            if (Clipboard.ContainsText())
            {
                string texto = Clipboard.GetText();

                int inicio = rTxtCodigoFuente.SelectionStart;

                rTxtCodigoFuente.SelectedText = texto;

                // Aplicar fuente solo al texto pegado
                rTxtCodigoFuente.Select(inicio, texto.Length);
                rTxtCodigoFuente.SelectionFont = new Font("Consolas", 11);

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

            // 1. Recorrido de los caracteres visibles (h, o, l, a...)
            foreach (char c in cadenaEntrada)
            {
                string nombreColumna = ObtenerNombreColumnaSQL(c);
                estadoActual = MoverSiguienteEstado(matriz, estadoActual, nombreColumna, estadosError);

                if (estadosError.Contains(estadoActual))
                {
                    return ObtenerTokenOError(matriz, estadoActual);
                }
            }

            // 2. REPRESENTACIÓN DEL FDC (Sin símbolo físico)
            // Forzamos un último salto usando la columna "FDC" de tu base de datos
            estadoActual = MoverSiguienteEstado(matriz, estadoActual, "FDC", estadosError);

            // 3. Verificación final
            return ObtenerTokenOError(matriz, estadoActual);
        }

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


        private string ObtenerTokenOError(DataTable matriz, int estado)
        {
            DataRow[] filas = matriz.Select("F1 = " + estado);
            if (filas.Length > 0 && filas[0]["ACEPTA"] != DBNull.Value)
            {
                return filas[0]["ACEPTA"].ToString();
            }
            return "CADENA_NO_VALIDA";
        }

        // Esta función traduce lo que el usuario escribe a como se llama la columna en SQL
        private string ObtenerNombreColumnaSQL(char c)
        {
            // 1. Manejo de Mayúsculas (SQL les añade un 1)
            if (char.IsUpper(c))
            {
                return c.ToString() + "1";
            }
            
            // 2. Manejo de Caracteres Especiales con nombres modificados por SQL
            // Basado en tus hallazgos: . es #1, , es . , [ es (1 , ] es )1
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
                // Si detectas que otros símbolos fallan, agrégalos aquí:
                // case '(': return "algunNombre"; 

                default:
                    // Para minúsculas y el resto de símbolos que SQL no renombró
                    return c.ToString();
            }
        }
        /*-----------------------------ARCHIVO DE TOKENS-----------------------------*/
        
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

                // 1. Manejo de espacios/tabuladores
                if (c == ' ' || c == '\t')
                {
                    if (acumulador.Length > 0)
                    {
                        // CAPTURAMOS EL RESULTADO EN UNA VARIABLE
                        string resultado = ValidarCadena(matriz, acumulador);

                        if (resultado == "IDEN")
                        {
                            // Sustituimos "IDEN" por su valor específico (IDEN1, IDEN2, etc.)
                            resultado = ObtenerTokenIdentificador(acumulador, dtgTablaSimbolos);
                        }

                        // Verificamos si es un error antes de agregarlo al StringBuilder
                        VerificarSiEsError(resultado, acumulador, numLinea, rtbErrores, ref totalErrores);

                        sbLinea.Append(resultado + " ");
                        acumulador = "";
                    }
                    sbLinea.Append(c);
                }
                // 2. Manejo de cadenas (comillas)
                else if (c == '"')
                {
                    if (acumulador.Length > 0)
                    {
                        string resPre = ValidarCadena(matriz, acumulador);
                        VerificarSiEsError(resPre, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resPre + " ");
                        acumulador = "";
                    }

                    string cadena = c.ToString(); i++;
                    while (i < lineaConEspacio.Length && lineaConEspacio[i] != '"')
                    {
                        cadena += lineaConEspacio[i]; i++;
                    }
                    if (i < lineaConEspacio.Length) cadena += '"';

                    // CAPTURAMOS EL RESULTADO DE LA CADENA
                    string resCad = ValidarCadena(matriz, cadena);
                    VerificarSiEsError(resCad, cadena, numLinea, rtbErrores, ref totalErrores);

                    sbLinea.Append(resCad + " ");
                }
                // 3. Símbolos especiales (Delimitadores/Operadores)
                else if ("()[]{};,+-*/<>=".Contains(c.ToString()))
                {
                    if (acumulador.Length > 0)
                    {
                        string resAcumulado = ValidarCadena(matriz, acumulador);

                        // Enviamos a la lista de errores si es necesario
                        VerificarSiEsError(resAcumulado, acumulador, numLinea, rtbErrores, ref totalErrores);
                        sbLinea.Append(resAcumulado + " ");
                        acumulador = "";
                    }

                    string lexemaEspecial = c.ToString();
                    // Lookahead para //, ++, --
                    if (i + 1 < lineaConEspacio.Length)
                    {
                        char sig = lineaConEspacio[i + 1];
                        if ((c == '/' && sig == '/') || (c == '+' && sig == '+') || (c == '-' && sig == '-') || (c == '&' && sig == '&') ||
                            (c == '|' && sig == '|'))
                        {
                            lexemaEspecial += sig; i++;
                        }
                    }
                    string resEspecial = ValidarCadena(matriz, lexemaEspecial);

                    // Verificamos si el símbolo mismo es un error (ej: un & solo que no es &&)
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
            // 1. Definimos exactamente qué es un error.
            // Usamos StartsWith para que "ER01" sea error, pero "CONENTERO" no.
            bool esError = resultado.StartsWith("ER") ||
                           resultado.Equals("CADENA_NO_VALIDA") ||
                           resultado.Contains("Invalido"); // Solo si tus mensajes de SQL usan esta palabra

            // 2. Filtro de seguridad: Si el token es uno de tus tokens válidos, NO es error.
            // Esto evita que "CONENTERO" entre a la lista.
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
        // Diccionario para rastrear identificadores: <Nombre, NumeroID>
        Dictionary<string, int> tablaSimbolos = new Dictionary<string, int>();
        int contadorID = 1;

        private string ObtenerTokenIdentificador(string lexema, DataGridView dgvSimbolos)
        {
            // Si ya existe en la tabla, devolvemos su ID asignado
            if (tablaSimbolos.ContainsKey(lexema))
            {
                return "IDEN" + tablaSimbolos[lexema];
            }

            // Si es nuevo, lo registramos
            int nuevoID = contadorID++;
            tablaSimbolos.Add(lexema, nuevoID);

            // Lo agregamos visualmente al DataGridView
            // Columnas: # IDENTIFICADOR, NOMBRE, TIPO DE DATO, VALOR
            dgvSimbolos.Rows.Add(nuevoID, lexema, "", "");

            return "IDEN" + nuevoID;
        }


    }
}
