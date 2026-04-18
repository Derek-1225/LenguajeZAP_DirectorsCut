namespace AnalizadorLexico_LenguajeZAP
{
    partial class Form1
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabCodigo = new System.Windows.Forms.TabPage();
            this.btnAnalizarCodigo = new System.Windows.Forms.Button();
            this.btnGuardarArchivo = new System.Windows.Forms.Button();
            this.btnEditarPrograma = new System.Windows.Forms.Button();
            this.btnCargarPrograma = new System.Windows.Forms.Button();
            this.tabToken = new System.Windows.Forms.TabPage();
            this.btnGuardarArchivoTokens = new System.Windows.Forms.Button();
            this.tabError = new System.Windows.Forms.TabPage();
            this.rTxtErrores = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rTxtCodigoFuente = new System.Windows.Forms.RichTextBox();
            this.rTxtNumeros = new System.Windows.Forms.RichTextBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.rTxtTokens = new System.Windows.Forms.RichTextBox();
            this.rTxtNumerosTokens = new System.Windows.Forms.RichTextBox();
            this.tabControl1.SuspendLayout();
            this.tabCodigo.SuspendLayout();
            this.tabToken.SuspendLayout();
            this.tabError.SuspendLayout();
            this.panel1.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabCodigo);
            this.tabControl1.Controls.Add(this.tabToken);
            this.tabControl1.Controls.Add(this.tabError);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1283, 727);
            this.tabControl1.TabIndex = 0;
            // 
            // tabCodigo
            // 
            this.tabCodigo.Controls.Add(this.panel1);
            this.tabCodigo.Controls.Add(this.btnAnalizarCodigo);
            this.tabCodigo.Controls.Add(this.btnGuardarArchivo);
            this.tabCodigo.Controls.Add(this.btnEditarPrograma);
            this.tabCodigo.Controls.Add(this.btnCargarPrograma);
            this.tabCodigo.Location = new System.Drawing.Point(4, 25);
            this.tabCodigo.Name = "tabCodigo";
            this.tabCodigo.Padding = new System.Windows.Forms.Padding(3);
            this.tabCodigo.Size = new System.Drawing.Size(1275, 698);
            this.tabCodigo.TabIndex = 0;
            this.tabCodigo.Text = "Codigo";
            this.tabCodigo.UseVisualStyleBackColor = true;
            // 
            // btnAnalizarCodigo
            // 
            this.btnAnalizarCodigo.Location = new System.Drawing.Point(1020, 637);
            this.btnAnalizarCodigo.Name = "btnAnalizarCodigo";
            this.btnAnalizarCodigo.Size = new System.Drawing.Size(248, 46);
            this.btnAnalizarCodigo.TabIndex = 4;
            this.btnAnalizarCodigo.Text = "Analizar Codigo";
            this.btnAnalizarCodigo.UseVisualStyleBackColor = true;
            this.btnAnalizarCodigo.Click += new System.EventHandler(this.btnAnalizarCodigo_Click);
            // 
            // btnGuardarArchivo
            // 
            this.btnGuardarArchivo.Location = new System.Drawing.Point(760, 637);
            this.btnGuardarArchivo.Name = "btnGuardarArchivo";
            this.btnGuardarArchivo.Size = new System.Drawing.Size(248, 46);
            this.btnGuardarArchivo.TabIndex = 3;
            this.btnGuardarArchivo.Text = "Guardar Archivo";
            this.btnGuardarArchivo.UseVisualStyleBackColor = true;
            // 
            // btnEditarPrograma
            // 
            this.btnEditarPrograma.Location = new System.Drawing.Point(500, 637);
            this.btnEditarPrograma.Name = "btnEditarPrograma";
            this.btnEditarPrograma.Size = new System.Drawing.Size(248, 46);
            this.btnEditarPrograma.TabIndex = 2;
            this.btnEditarPrograma.Text = "Editar Programa";
            this.btnEditarPrograma.UseVisualStyleBackColor = true;
            // 
            // btnCargarPrograma
            // 
            this.btnCargarPrograma.Location = new System.Drawing.Point(240, 637);
            this.btnCargarPrograma.Name = "btnCargarPrograma";
            this.btnCargarPrograma.Size = new System.Drawing.Size(248, 46);
            this.btnCargarPrograma.TabIndex = 1;
            this.btnCargarPrograma.Text = "Cargar Programa";
            this.btnCargarPrograma.UseVisualStyleBackColor = true;
            // 
            // tabToken
            // 
            this.tabToken.Controls.Add(this.panel2);
            this.tabToken.Controls.Add(this.btnGuardarArchivoTokens);
            this.tabToken.Location = new System.Drawing.Point(4, 25);
            this.tabToken.Name = "tabToken";
            this.tabToken.Padding = new System.Windows.Forms.Padding(3);
            this.tabToken.Size = new System.Drawing.Size(1275, 698);
            this.tabToken.TabIndex = 1;
            this.tabToken.Text = "Lista de Tokens";
            this.tabToken.UseVisualStyleBackColor = true;
            // 
            // btnGuardarArchivoTokens
            // 
            this.btnGuardarArchivoTokens.Location = new System.Drawing.Point(1017, 635);
            this.btnGuardarArchivoTokens.Name = "btnGuardarArchivoTokens";
            this.btnGuardarArchivoTokens.Size = new System.Drawing.Size(248, 46);
            this.btnGuardarArchivoTokens.TabIndex = 4;
            this.btnGuardarArchivoTokens.Text = "Guardar Archivo";
            this.btnGuardarArchivoTokens.UseVisualStyleBackColor = true;
            // 
            // tabError
            // 
            this.tabError.Controls.Add(this.rTxtErrores);
            this.tabError.Location = new System.Drawing.Point(4, 25);
            this.tabError.Name = "tabError";
            this.tabError.Padding = new System.Windows.Forms.Padding(3);
            this.tabError.Size = new System.Drawing.Size(1275, 698);
            this.tabError.TabIndex = 2;
            this.tabError.Text = "Lista de Errores";
            this.tabError.UseVisualStyleBackColor = true;
            // 
            // rTxtErrores
            // 
            this.rTxtErrores.Location = new System.Drawing.Point(0, 0);
            this.rTxtErrores.Name = "rTxtErrores";
            this.rTxtErrores.ReadOnly = true;
            this.rTxtErrores.Size = new System.Drawing.Size(1269, 622);
            this.rTxtErrores.TabIndex = 0;
            this.rTxtErrores.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rTxtNumeros);
            this.panel1.Controls.Add(this.rTxtCodigoFuente);
            this.panel1.Location = new System.Drawing.Point(3, 6);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1266, 625);
            this.panel1.TabIndex = 5;
            // 
            // rTxtCodigoFuente
            // 
            this.rTxtCodigoFuente.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rTxtCodigoFuente.Location = new System.Drawing.Point(72, -4);
            this.rTxtCodigoFuente.Name = "rTxtCodigoFuente";
            this.rTxtCodigoFuente.Size = new System.Drawing.Size(1190, 629);
            this.rTxtCodigoFuente.TabIndex = 1;
            this.rTxtCodigoFuente.Text = "";
            // 
            // rTxtNumeros
            // 
            this.rTxtNumeros.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rTxtNumeros.ForeColor = System.Drawing.Color.Blue;
            this.rTxtNumeros.Location = new System.Drawing.Point(-3, -4);
            this.rTxtNumeros.Name = "rTxtNumeros";
            this.rTxtNumeros.ReadOnly = true;
            this.rTxtNumeros.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rTxtNumeros.Size = new System.Drawing.Size(75, 629);
            this.rTxtNumeros.TabIndex = 2;
            this.rTxtNumeros.Text = "";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.rTxtNumerosTokens);
            this.panel2.Controls.Add(this.rTxtTokens);
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1266, 625);
            this.panel2.TabIndex = 5;
            // 
            // rTxtTokens
            // 
            this.rTxtTokens.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rTxtTokens.Location = new System.Drawing.Point(78, 0);
            this.rTxtTokens.Name = "rTxtTokens";
            this.rTxtTokens.ReadOnly = true;
            this.rTxtTokens.Size = new System.Drawing.Size(1190, 629);
            this.rTxtTokens.TabIndex = 1;
            this.rTxtTokens.Text = "";
            // 
            // rTxtNumerosTokens
            // 
            this.rTxtNumerosTokens.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rTxtNumerosTokens.ForeColor = System.Drawing.Color.Blue;
            this.rTxtNumerosTokens.Location = new System.Drawing.Point(0, 0);
            this.rTxtNumerosTokens.Name = "rTxtNumerosTokens";
            this.rTxtNumerosTokens.ReadOnly = true;
            this.rTxtNumerosTokens.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rTxtNumerosTokens.Size = new System.Drawing.Size(75, 629);
            this.rTxtNumerosTokens.TabIndex = 3;
            this.rTxtNumerosTokens.Text = "";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1307, 762);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.Text = "Analizador Lenguaje ZAP";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabCodigo.ResumeLayout(false);
            this.tabToken.ResumeLayout(false);
            this.tabError.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabCodigo;
        private System.Windows.Forms.TabPage tabToken;
        private System.Windows.Forms.TabPage tabError;
        private System.Windows.Forms.Button btnCargarPrograma;
        private System.Windows.Forms.RichTextBox rTxtErrores;
        private System.Windows.Forms.Button btnAnalizarCodigo;
        private System.Windows.Forms.Button btnGuardarArchivo;
        private System.Windows.Forms.Button btnEditarPrograma;
        private System.Windows.Forms.Button btnGuardarArchivoTokens;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RichTextBox rTxtCodigoFuente;
        private System.Windows.Forms.RichTextBox rTxtNumeros;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.RichTextBox rTxtNumerosTokens;
        private System.Windows.Forms.RichTextBox rTxtTokens;
    }
}

