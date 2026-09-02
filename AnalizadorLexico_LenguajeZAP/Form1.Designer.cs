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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabCodigo = new System.Windows.Forms.TabPage();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rTxtNumeros = new System.Windows.Forms.RichTextBox();
            this.rTxtCodigoFuente = new System.Windows.Forms.RichTextBox();
            this.btnAnalizarCodigo = new System.Windows.Forms.Button();
            this.btnGuardarArchivo = new System.Windows.Forms.Button();
            this.btnEditarPrograma = new System.Windows.Forms.Button();
            this.btnCargarPrograma = new System.Windows.Forms.Button();
            this.tabToken = new System.Windows.Forms.TabPage();
            this.panel2 = new System.Windows.Forms.Panel();
            this.rTxtNumerosTokens = new System.Windows.Forms.RichTextBox();
            this.rTxtTokens = new System.Windows.Forms.RichTextBox();
            this.btnGuardarArchivoTokens = new System.Windows.Forms.Button();
            this.tabError = new System.Windows.Forms.TabPage();
            this.rTxtErrores = new System.Windows.Forms.RichTextBox();
            this.tabTablaSimbolos = new System.Windows.Forms.TabPage();
            this.dtgTablaSimbolos = new System.Windows.Forms.DataGridView();
            this.Identificador = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Nombre = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.TipoDato = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.Valor = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabCodigo.SuspendLayout();
            this.panel1.SuspendLayout();
            this.tabToken.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabError.SuspendLayout();
            this.tabTablaSimbolos.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dtgTablaSimbolos)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabCodigo);
            this.tabControl1.Controls.Add(this.tabToken);
            this.tabControl1.Controls.Add(this.tabError);
            this.tabControl1.Controls.Add(this.tabTablaSimbolos);
            this.tabControl1.Location = new System.Drawing.Point(9, 10);
            this.tabControl1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(962, 591);
            this.tabControl1.TabIndex = 0;
            // 
            // tabCodigo
            // 
            this.tabCodigo.Controls.Add(this.panel1);
            this.tabCodigo.Controls.Add(this.btnAnalizarCodigo);
            this.tabCodigo.Controls.Add(this.btnGuardarArchivo);
            this.tabCodigo.Controls.Add(this.btnEditarPrograma);
            this.tabCodigo.Controls.Add(this.btnCargarPrograma);
            this.tabCodigo.Location = new System.Drawing.Point(4, 22);
            this.tabCodigo.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabCodigo.Name = "tabCodigo";
            this.tabCodigo.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabCodigo.Size = new System.Drawing.Size(954, 565);
            this.tabCodigo.TabIndex = 0;
            this.tabCodigo.Text = "Código";
            this.tabCodigo.UseVisualStyleBackColor = true;
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.rTxtNumeros);
            this.panel1.Controls.Add(this.rTxtCodigoFuente);
            this.panel1.Location = new System.Drawing.Point(2, 5);
            this.panel1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(950, 508);
            this.panel1.TabIndex = 5;
            // 
            // rTxtNumeros
            // 
            this.rTxtNumeros.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rTxtNumeros.Enabled = false;
            this.rTxtNumeros.ForeColor = System.Drawing.Color.Blue;
            this.rTxtNumeros.Location = new System.Drawing.Point(-2, -3);
            this.rTxtNumeros.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rTxtNumeros.Name = "rTxtNumeros";
            this.rTxtNumeros.ReadOnly = true;
            this.rTxtNumeros.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rTxtNumeros.Size = new System.Drawing.Size(57, 512);
            this.rTxtNumeros.TabIndex = 2;
            this.rTxtNumeros.Text = "";
            // 
            // rTxtCodigoFuente
            // 
            this.rTxtCodigoFuente.AcceptsTab = true;
            this.rTxtCodigoFuente.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rTxtCodigoFuente.Location = new System.Drawing.Point(54, -3);
            this.rTxtCodigoFuente.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rTxtCodigoFuente.Name = "rTxtCodigoFuente";
            this.rTxtCodigoFuente.Size = new System.Drawing.Size(894, 512);
            this.rTxtCodigoFuente.TabIndex = 1;
            this.rTxtCodigoFuente.Text = "";
            // 
            // btnAnalizarCodigo
            // 
            this.btnAnalizarCodigo.Location = new System.Drawing.Point(765, 518);
            this.btnAnalizarCodigo.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnAnalizarCodigo.Name = "btnAnalizarCodigo";
            this.btnAnalizarCodigo.Size = new System.Drawing.Size(186, 37);
            this.btnAnalizarCodigo.TabIndex = 4;
            this.btnAnalizarCodigo.Text = "Analizar Código";
            this.btnAnalizarCodigo.UseVisualStyleBackColor = true;
            this.btnAnalizarCodigo.Click += new System.EventHandler(this.btnAnalizarCodigo_Click);
            // 
            // btnGuardarArchivo
            // 
            this.btnGuardarArchivo.Location = new System.Drawing.Point(570, 518);
            this.btnGuardarArchivo.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnGuardarArchivo.Name = "btnGuardarArchivo";
            this.btnGuardarArchivo.Size = new System.Drawing.Size(186, 37);
            this.btnGuardarArchivo.TabIndex = 3;
            this.btnGuardarArchivo.Text = "Guardar Archivo";
            this.btnGuardarArchivo.UseVisualStyleBackColor = true;
            this.btnGuardarArchivo.Click += new System.EventHandler(this.btnGuardarArchivo_Click);
            // 
            // btnEditarPrograma
            // 
            this.btnEditarPrograma.Location = new System.Drawing.Point(375, 518);
            this.btnEditarPrograma.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnEditarPrograma.Name = "btnEditarPrograma";
            this.btnEditarPrograma.Size = new System.Drawing.Size(186, 37);
            this.btnEditarPrograma.TabIndex = 2;
            this.btnEditarPrograma.Text = "Editar Programa";
            this.btnEditarPrograma.UseVisualStyleBackColor = true;
            this.btnEditarPrograma.Click += new System.EventHandler(this.btnEditarPrograma_Click);
            // 
            // btnCargarPrograma
            // 
            this.btnCargarPrograma.Location = new System.Drawing.Point(180, 518);
            this.btnCargarPrograma.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnCargarPrograma.Name = "btnCargarPrograma";
            this.btnCargarPrograma.Size = new System.Drawing.Size(186, 37);
            this.btnCargarPrograma.TabIndex = 1;
            this.btnCargarPrograma.Text = "Cargar Programa";
            this.btnCargarPrograma.UseVisualStyleBackColor = true;
            this.btnCargarPrograma.Click += new System.EventHandler(this.btnCargarPrograma_Click);
            // 
            // tabToken
            // 
            this.tabToken.Controls.Add(this.panel2);
            this.tabToken.Controls.Add(this.btnGuardarArchivoTokens);
            this.tabToken.Location = new System.Drawing.Point(4, 22);
            this.tabToken.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabToken.Name = "tabToken";
            this.tabToken.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabToken.Size = new System.Drawing.Size(954, 565);
            this.tabToken.TabIndex = 1;
            this.tabToken.Text = "Lista de Tokens";
            this.tabToken.UseVisualStyleBackColor = true;
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.rTxtNumerosTokens);
            this.panel2.Controls.Add(this.rTxtTokens);
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(950, 508);
            this.panel2.TabIndex = 5;
            // 
            // rTxtNumerosTokens
            // 
            this.rTxtNumerosTokens.BackColor = System.Drawing.SystemColors.ControlLight;
            this.rTxtNumerosTokens.ForeColor = System.Drawing.Color.Blue;
            this.rTxtNumerosTokens.Location = new System.Drawing.Point(0, 0);
            this.rTxtNumerosTokens.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rTxtNumerosTokens.Name = "rTxtNumerosTokens";
            this.rTxtNumerosTokens.ReadOnly = true;
            this.rTxtNumerosTokens.ScrollBars = System.Windows.Forms.RichTextBoxScrollBars.None;
            this.rTxtNumerosTokens.Size = new System.Drawing.Size(57, 512);
            this.rTxtNumerosTokens.TabIndex = 3;
            this.rTxtNumerosTokens.Text = "";
            // 
            // rTxtTokens
            // 
            this.rTxtTokens.Font = new System.Drawing.Font("Consolas", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.rTxtTokens.Location = new System.Drawing.Point(58, 0);
            this.rTxtTokens.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rTxtTokens.Name = "rTxtTokens";
            this.rTxtTokens.ReadOnly = true;
            this.rTxtTokens.Size = new System.Drawing.Size(894, 512);
            this.rTxtTokens.TabIndex = 1;
            this.rTxtTokens.Text = "";
            // 
            // btnGuardarArchivoTokens
            // 
            this.btnGuardarArchivoTokens.Location = new System.Drawing.Point(763, 516);
            this.btnGuardarArchivoTokens.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.btnGuardarArchivoTokens.Name = "btnGuardarArchivoTokens";
            this.btnGuardarArchivoTokens.Size = new System.Drawing.Size(186, 37);
            this.btnGuardarArchivoTokens.TabIndex = 4;
            this.btnGuardarArchivoTokens.Text = "Guardar Archivo";
            this.btnGuardarArchivoTokens.UseVisualStyleBackColor = true;
            this.btnGuardarArchivoTokens.Click += new System.EventHandler(this.btnGuardarArchivoTokens_Click);
            // 
            // tabError
            // 
            this.tabError.Controls.Add(this.rTxtErrores);
            this.tabError.Location = new System.Drawing.Point(4, 22);
            this.tabError.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabError.Name = "tabError";
            this.tabError.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabError.Size = new System.Drawing.Size(954, 565);
            this.tabError.TabIndex = 2;
            this.tabError.Text = "Lista de Errores";
            this.tabError.UseVisualStyleBackColor = true;
            // 
            // rTxtErrores
            // 
            this.rTxtErrores.Location = new System.Drawing.Point(0, 0);
            this.rTxtErrores.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.rTxtErrores.Name = "rTxtErrores";
            this.rTxtErrores.ReadOnly = true;
            this.rTxtErrores.Size = new System.Drawing.Size(953, 506);
            this.rTxtErrores.TabIndex = 0;
            this.rTxtErrores.Text = "";
            // 
            // tabTablaSimbolos
            // 
            this.tabTablaSimbolos.Controls.Add(this.dtgTablaSimbolos);
            this.tabTablaSimbolos.Location = new System.Drawing.Point(4, 22);
            this.tabTablaSimbolos.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabTablaSimbolos.Name = "tabTablaSimbolos";
            this.tabTablaSimbolos.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.tabTablaSimbolos.Size = new System.Drawing.Size(954, 565);
            this.tabTablaSimbolos.TabIndex = 3;
            this.tabTablaSimbolos.Text = "Tabla de Símbolos";
            this.tabTablaSimbolos.UseVisualStyleBackColor = true;
            // 
            // dtgTablaSimbolos
            // 
            this.dtgTablaSimbolos.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dtgTablaSimbolos.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
            this.dtgTablaSimbolos.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dtgTablaSimbolos.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Identificador,
            this.Nombre,
            this.TipoDato,
            this.Valor});
            this.dtgTablaSimbolos.Location = new System.Drawing.Point(4, 5);
            this.dtgTablaSimbolos.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.dtgTablaSimbolos.Name = "dtgTablaSimbolos";
            this.dtgTablaSimbolos.ReadOnly = true;
            this.dtgTablaSimbolos.RowHeadersWidth = 51;
            this.dtgTablaSimbolos.RowTemplate.Height = 24;
            this.dtgTablaSimbolos.Size = new System.Drawing.Size(947, 540);
            this.dtgTablaSimbolos.TabIndex = 0;
            // 
            // Identificador
            // 
            this.Identificador.HeaderText = "Identificador";
            this.Identificador.MinimumWidth = 6;
            this.Identificador.Name = "Identificador";
            this.Identificador.ReadOnly = true;
            // 
            // Nombre
            // 
            this.Nombre.HeaderText = "Nombre";
            this.Nombre.MinimumWidth = 6;
            this.Nombre.Name = "Nombre";
            this.Nombre.ReadOnly = true;
            // 
            // TipoDato
            // 
            this.TipoDato.HeaderText = "Tipo de Dato";
            this.TipoDato.MinimumWidth = 6;
            this.TipoDato.Name = "TipoDato";
            this.TipoDato.ReadOnly = true;
            // 
            // Valor
            // 
            this.Valor.HeaderText = "Valor";
            this.Valor.MinimumWidth = 6;
            this.Valor.Name = "Valor";
            this.Valor.ReadOnly = true;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(976, 35);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(163, 182);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBox1.TabIndex = 1;
            this.pictureBox1.TabStop = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(976, 219);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(102, 18);
            this.label1.TabIndex = 2;
            this.label1.Text = "Versión: Beta";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Arial", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(192)))));
            this.label2.Location = new System.Drawing.Point(976, 240);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(274, 60);
            this.label2.TabIndex = 3;
            this.label2.Text = "Integrantes del Equipo\r\nEmmanuel Blanco Samaniego 23100139\r\nDerek Alexander Camar" +
    "ena Zequeida 23100141\r\nCarlos Eduardo Contreras Hernández 23100151\r\n";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1256, 619);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Analizador Léxico ZAP";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.tabControl1.ResumeLayout(false);
            this.tabCodigo.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.tabToken.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.tabError.ResumeLayout(false);
            this.tabTablaSimbolos.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dtgTablaSimbolos)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

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
        private System.Windows.Forms.TabPage tabTablaSimbolos;
        private System.Windows.Forms.DataGridView dtgTablaSimbolos;
        private System.Windows.Forms.DataGridViewTextBoxColumn Identificador;
        private System.Windows.Forms.DataGridViewTextBoxColumn Nombre;
        private System.Windows.Forms.DataGridViewTextBoxColumn TipoDato;
        private System.Windows.Forms.DataGridViewTextBoxColumn Valor;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
    }
}

