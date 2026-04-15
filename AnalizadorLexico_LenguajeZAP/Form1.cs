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
            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                try
                {
                    conexion.Open();
                    MessageBox.Show("¡Conexión exitosa!");
                    ObtenerCaracter(200);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error al conectar: " + ex.Message);
                }
            }
        }

        //Metodo de Prueba para verificar conexión y consulta a la base de datos 
        public string ObtenerCaracter(int id)
        {
            string query = "SELECT a,b,c,d,e,f FROM [dbo].['Hoja 1$'] WHERE F1 = @id";
            string resultado = "";

            using (SqlConnection conexion = new SqlConnection(connectionString))
            {
                SqlCommand comando = new SqlCommand(query, conexion);
                // Usar parámetros evita ataques de Inyección SQL
                comando.Parameters.AddWithValue("@id", id);

                try
                {
                    conexion.Open();
                    /* ExecuteScalar devuelve un 'object', por lo que hay que convertirlo a string
                    using(SqlDataReader reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            resultado = reader["a"].ToString(); // Ejemplo: obtener el valor de la columna 'a'
                            MessageBox.Show("Valor obtenido: " + resultado);
                        }
                        else
                        {
                            MessageBox.Show("No se encontró ningún registro con el ID proporcionado.");
                        }
                    }
                    object valor = comando.ExecuteScalar();

                    if (valor != null)
                    {
                        resultado = valor.ToString();
                        MessageBox.Show("Valor obtenido: " + resultado);
                    }*/
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            return resultado;
        }

    }
}
