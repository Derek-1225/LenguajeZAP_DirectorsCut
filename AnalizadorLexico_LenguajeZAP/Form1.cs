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
            string strCadenaEntrada = rTxtCodigoFuente.Text;
            DataTable matriz = ObtenerMatriz();

            GenerarEspejoTokens(matriz, strCadenaEntrada, rTxtTokens);

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
        public void GenerarEspejoTokens(DataTable matriz, string textoFuente, RichTextBox rtbTokens)
        {
            rtbTokens.Clear();

            // Dividimos el código fuente en líneas para mantener la estructura
            string[] lineas = textoFuente.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);

            foreach (string linea in lineas)
            {
                string lineaProcesada = ProcesarLineaParaTokens(matriz, linea);
                rtbTokens.AppendText(lineaProcesada + Environment.NewLine);
            }
        }

        private string ProcesarLineaParaTokens(DataTable matriz, string textoLinea)
        {
            if (string.IsNullOrWhiteSpace(textoLinea)) return "";

            StringBuilder sbLinea = new StringBuilder();
            string acumulador = "";
            // Añadimos un espacio al final de la línea para procesar el último lexema
            string lineaConEspacio = textoLinea + " ";

            for (int i = 0; i < lineaConEspacio.Length; i++)
            {
                char c = lineaConEspacio[i];

                // 1. Si es espacio o tabulador, lo mantenemos para conservar la sangría
                if (c == ' ' || c == '\t')
                {
                    if (acumulador.Length > 0)
                    {
                        sbLinea.Append(ValidarCadena(matriz, acumulador) + " ");
                        acumulador = "";
                    }
                    sbLinea.Append(c); // Mantiene el espacio original
                }
                // 2. Manejo de cadenas (comillas)
                else if (c == '"')
                {
                    string cadena = c.ToString(); i++;
                    while (i < lineaConEspacio.Length && lineaConEspacio[i] != '"')
                    {
                        cadena += lineaConEspacio[i]; i++;
                    }
                    if (i < lineaConEspacio.Length) cadena += '"';
                    sbLinea.Append(ValidarCadena(matriz, cadena) + " ");
                }
                // 3. Símbolos especiales (Delimitadores/Operadores)
                else if ("()[]{};,+-*/<>=".Contains(c.ToString()))
                {
                    if (acumulador.Length > 0)
                    {
                        sbLinea.Append(ValidarCadena(matriz, acumulador) + " ");
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
                    sbLinea.Append(ValidarCadena(matriz, lexemaEspecial) + " ");
                }
                else
                {
                    acumulador += c;
                }
            }

            return sbLinea.ToString();
        }

        //Metodo para genera el archivo de tokens
        public void GenerarArchivoTokens(DataTable matriz, string textoFuente, RichTextBox rtbTokens)
        {
            // Limpiamos el "archivo de tokens" antes de empezar
            rTxtTokens.Clear();
            string acumulador = "";
            int numeroLinea = 1;

            // Agregamos un espacio al final para procesar el último lexema
            textoFuente += " ";
            //Version
            for (int i = 0; i < textoFuente.Length; i++)
            {
                char c = textoFuente[i];

                // Si detectamos un salto de línea, aumentamos el contador
                if (c == '\n')
                {
                    if (acumulador.Length > 0)
                    {
                        ImprimirTokenEnArchivo(matriz, acumulador, rtbTokens, numeroLinea);
                        acumulador = "";
                    }
                    numeroLinea++;
                    continue;
                }

                if (char.IsWhiteSpace(c))
                {
                    if (acumulador.Length > 0)
                    {
                        ImprimirTokenEnArchivo(matriz, acumulador, rtbTokens, numeroLinea);
                        acumulador = "";
                    }
                }

                // 1. MANEJO DE CADENAS (Todo lo que esté entre " ")
                if (c == '"')
                {
                    // Si había algo antes de la comilla, lo procesamos
                    if (acumulador.Length > 0) { ImprimirTokenEnArchivo(matriz, acumulador, rtbTokens, numeroLinea); acumulador = ""; }

                    string cadenaCompleta = c.ToString(); // Inicia con "
                    i++;
                    // Leemos hasta encontrar la comilla de cierre o el fin del texto
                    while (i < textoFuente.Length && textoFuente[i] != '"')
                    {
                        cadenaCompleta += textoFuente[i];
                        i++;
                    }
                    if (i < textoFuente.Length) cadenaCompleta += '"'; // Cierra con "

                    ImprimirTokenEnArchivo(matriz, cadenaCompleta, rtbTokens,numeroLinea);
                    continue;
                }

                // 2. SEPARADORES (Espacios, Tabs, NewLines)
                if (char.IsWhiteSpace(c))
                {
                    if (acumulador.Length > 0)
                    {
                        ImprimirTokenEnArchivo(matriz, acumulador, rtbTokens,numeroLinea);
                        acumulador = "";
                    }
                }
                // 3. SÍMBOLOS ESPECIALES (Delimitadores y Operadores)
                // NOTA: He quitado el '$' de aquí para que se quede pegado a la palabra
                else if ("()[]{};,+-*/<>=".Contains(c.ToString()))
                {
                    if (acumulador.Length > 0) { ImprimirTokenEnArchivo(matriz, acumulador, rtbTokens,numeroLinea); acumulador = ""; }

                    string lexemaEspecial = c.ToString();
                    // Lógica de Lookahead (//, ++, --)
                    if (i + 1 < textoFuente.Length)
                    {
                        char siguiente = textoFuente[i + 1];
                        if ((c == '/' && siguiente == '/') || (c == '+' && siguiente == '+') || (c == '-' && siguiente == '-') || (c == '&' && siguiente == '&') ||
                            (c == '|' && siguiente == '|'))
                        {
                            lexemaEspecial += siguiente;
                            i++;
                        }
                    }
                    if (lexemaEspecial == "//")
                    {
                        // Opcional: Saltar todo el texto hasta encontrar un salto de línea
                        while (i + 1 < textoFuente.Length && textoFuente[i + 1] != '\n')
                        {
                            i++;
                        }
                        ImprimirTokenEnArchivo(matriz, "//", rtbTokens, numeroLinea); // O marcarlo como COMENTARIO
                        continue;
                    }
                    else
                    {
                        ImprimirTokenEnArchivo(matriz, lexemaEspecial, rtbTokens, numeroLinea);
                    }
                }
                // 4. ACUMULADOR (Letras, números y el símbolo $)
                else
                {
                    acumulador += c;
                }
            }
        }

        //Imprimir los tokens identificados.
        private void ImprimirTokenEnArchivo(DataTable matriz, string lexema, RichTextBox rtbTokens, int linea)
        {
            string tokenIdentificado = ValidarCadena(matriz, lexema);
            // Formato: [Línea 1] <Lexema, Token>
            rtbTokens.AppendText(string.Format("[Fila {0}] <{1}, {2}>{3}",
                                 linea, lexema, tokenIdentificado, Environment.NewLine));
        }

        // Extrae el mensaje de error (ER01, ER02...) desde la columna ACEPTA de los estados de error
        private string ObtenerMensajeError(DataTable matriz, int estadoError)
        {
            DataRow[] filaError = matriz.Select($"F1 = {estadoError}");
            if (filaError.Length > 0 && filaError[0]["ACEPTA"] != DBNull.Value)
            {
                return filaError[0]["ACEPTA"].ToString();
            }
            return "ERROR_DESCONOCIDO";
        }

    }
}
