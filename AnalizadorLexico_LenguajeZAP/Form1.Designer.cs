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
            this.btnCargarPrograma = new System.Windows.Forms.Button();
            this.rTxtCodigoFuente = new System.Windows.Forms.RichTextBox();
            this.tabToken = new System.Windows.Forms.TabPage();
            this.richTextBox2 = new System.Windows.Forms.RichTextBox();
            this.tabError = new System.Windows.Forms.TabPage();
            this.richTextBox3 = new System.Windows.Forms.RichTextBox();
            this.btnEditarPrograma = new System.Windows.Forms.Button();
            this.btnGuardarArchivo = new System.Windows.Forms.Button();
            this.btnAnalizarCodigo = new System.Windows.Forms.Button();
            this.btnGuardarArchivoTokens = new System.Windows.Forms.Button();
            this.tabControl1.SuspendLayout();
            this.tabCodigo.SuspendLayout();
            this.tabToken.SuspendLayout();
            this.tabError.SuspendLayout();
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
            this.tabCodigo.Controls.Add(this.btnAnalizarCodigo);
            this.tabCodigo.Controls.Add(this.btnGuardarArchivo);
            this.tabCodigo.Controls.Add(this.btnEditarPrograma);
            this.tabCodigo.Controls.Add(this.btnCargarPrograma);
            this.tabCodigo.Controls.Add(this.rTxtCodigoFuente);
            this.tabCodigo.Location = new System.Drawing.Point(4, 25);
            this.tabCodigo.Name = "tabCodigo";
            this.tabCodigo.Padding = new System.Windows.Forms.Padding(3);
            this.tabCodigo.Size = new System.Drawing.Size(1275, 698);
            this.tabCodigo.TabIndex = 0;
            this.tabCodigo.Text = "Codigo";
            this.tabCodigo.UseVisualStyleBackColor = true;
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
            // rTxtCodigoFuente
            // 
            this.rTxtCodigoFuente.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rTxtCodigoFuente.Location = new System.Drawing.Point(0, 0);
            this.rTxtCodigoFuente.Name = "rTxtCodigoFuente";
            this.rTxtCodigoFuente.Size = new System.Drawing.Size(1269, 622);
            this.rTxtCodigoFuente.TabIndex = 0;
            this.rTxtCodigoFuente.Text = "";
            // 
            // tabToken
            // 
            this.tabToken.Controls.Add(this.btnGuardarArchivoTokens);
            this.tabToken.Controls.Add(this.richTextBox2);
            this.tabToken.Location = new System.Drawing.Point(4, 25);
            this.tabToken.Name = "tabToken";
            this.tabToken.Padding = new System.Windows.Forms.Padding(3);
            this.tabToken.Size = new System.Drawing.Size(1275, 698);
            this.tabToken.TabIndex = 1;
            this.tabToken.Text = "Lista de Tokens";
            this.tabToken.UseVisualStyleBackColor = true;
            // 
            // richTextBox2
            // 
            this.richTextBox2.Location = new System.Drawing.Point(0, 0);
            this.richTextBox2.Name = "richTextBox2";
            this.richTextBox2.ReadOnly = true;
            this.richTextBox2.Size = new System.Drawing.Size(1265, 621);
            this.richTextBox2.TabIndex = 0;
            this.richTextBox2.Text = "";
            // 
            // tabError
            // 
            this.tabError.Controls.Add(this.richTextBox3);
            this.tabError.Location = new System.Drawing.Point(4, 25);
            this.tabError.Name = "tabError";
            this.tabError.Padding = new System.Windows.Forms.Padding(3);
            this.tabError.Size = new System.Drawing.Size(1275, 698);
            this.tabError.TabIndex = 2;
            this.tabError.Text = "Lista de Errores";
            this.tabError.UseVisualStyleBackColor = true;
            // 
            // richTextBox3
            // 
            this.richTextBox3.Location = new System.Drawing.Point(0, 0);
            this.richTextBox3.Name = "richTextBox3";
            this.richTextBox3.ReadOnly = true;
            this.richTextBox3.Size = new System.Drawing.Size(1269, 622);
            this.richTextBox3.TabIndex = 0;
            this.richTextBox3.Text = "";
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
            // btnGuardarArchivo
            // 
            this.btnGuardarArchivo.Location = new System.Drawing.Point(760, 637);
            this.btnGuardarArchivo.Name = "btnGuardarArchivo";
            this.btnGuardarArchivo.Size = new System.Drawing.Size(248, 46);
            this.btnGuardarArchivo.TabIndex = 3;
            this.btnGuardarArchivo.Text = "Guardar Archivo";
            this.btnGuardarArchivo.UseVisualStyleBackColor = true;
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
            // btnGuardarArchivoTokens
            // 
            this.btnGuardarArchivoTokens.Location = new System.Drawing.Point(1017, 635);
            this.btnGuardarArchivoTokens.Name = "btnGuardarArchivoTokens";
            this.btnGuardarArchivoTokens.Size = new System.Drawing.Size(248, 46);
            this.btnGuardarArchivoTokens.TabIndex = 4;
            this.btnGuardarArchivoTokens.Text = "Guardar Archivo";
            this.btnGuardarArchivoTokens.UseVisualStyleBackColor = true;
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
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabCodigo;
        private System.Windows.Forms.RichTextBox rTxtCodigoFuente;
        private System.Windows.Forms.TabPage tabToken;
        private System.Windows.Forms.TabPage tabError;
        private System.Windows.Forms.RichTextBox richTextBox2;
        private System.Windows.Forms.Button btnCargarPrograma;
        private System.Windows.Forms.RichTextBox richTextBox3;
        private System.Windows.Forms.Button btnAnalizarCodigo;
        private System.Windows.Forms.Button btnGuardarArchivo;
        private System.Windows.Forms.Button btnEditarPrograma;
        private System.Windows.Forms.Button btnGuardarArchivoTokens;
    }
}

