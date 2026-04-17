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
        //Conexion de SQL Server a la base de datos donde se encuentra la matriz de transicion
        string connectionString = "Server=DACZ-1225; Database=ZAP; Integrated Security=True; TrustServerCertificate=True;";
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void btnAnalizarCodigo_Click(object sender, EventArgs e)
        {
            string strCadenaEntrada = rTxtCodigoFuente.Text.Trim();
            DataTable matriz = ObtenerMatriz();

            MessageBox.Show(ValidarCadena(matriz, strCadenaEntrada));

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
                string query = "SELECT * FROM ['Hoja 1$']";
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
