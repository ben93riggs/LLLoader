namespace LLLoader
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.processListbox = new System.Windows.Forms.ListBox();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.loadButton = new MaterialSkin.Controls.MaterialRaisedButton();
            this.unloadButton = new MaterialSkin.Controls.MaterialRaisedButton();
            this.materialLabel1 = new MaterialSkin.Controls.MaterialLabel();
            this.dllPathTextbox = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.materialLabel2 = new MaterialSkin.Controls.MaterialLabel();
            this.processTextbox = new MaterialSkin.Controls.MaterialSingleLineTextField();
            this.materialDivider1 = new MaterialSkin.Controls.MaterialDivider();
            this.logBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // processListbox
            // 
            this.processListbox.FormattingEnabled = true;
            this.processListbox.Location = new System.Drawing.Point(14, 170);
            this.processListbox.Name = "processListbox";
            this.processListbox.Size = new System.Drawing.Size(273, 186);
            this.processListbox.Sorted = true;
            this.processListbox.TabIndex = 6;
            this.processListbox.SelectedIndexChanged += new System.EventHandler(this.processListbox_SelectedIndexChanged);
            // 
            // timer2
            // 
            this.timer2.Interval = 1000;
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // loadButton
            // 
            this.loadButton.Depth = 0;
            this.loadButton.Location = new System.Drawing.Point(14, 362);
            this.loadButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.loadButton.Name = "loadButton";
            this.loadButton.Primary = true;
            this.loadButton.Size = new System.Drawing.Size(75, 23);
            this.loadButton.TabIndex = 7;
            this.loadButton.Text = "Load";
            this.loadButton.UseVisualStyleBackColor = true;
            this.loadButton.Click += new System.EventHandler(this.loadButton_Click);
            // 
            // unloadButton
            // 
            this.unloadButton.Depth = 0;
            this.unloadButton.Location = new System.Drawing.Point(212, 362);
            this.unloadButton.MouseState = MaterialSkin.MouseState.HOVER;
            this.unloadButton.Name = "unloadButton";
            this.unloadButton.Primary = true;
            this.unloadButton.Size = new System.Drawing.Size(75, 23);
            this.unloadButton.TabIndex = 8;
            this.unloadButton.Text = "Unload";
            this.unloadButton.UseVisualStyleBackColor = true;
            this.unloadButton.Visible = false;
            this.unloadButton.Click += new System.EventHandler(this.unloadButton_Click);
            // 
            // materialLabel1
            // 
            this.materialLabel1.AutoSize = true;
            this.materialLabel1.BackColor = System.Drawing.Color.White;
            this.materialLabel1.Depth = 0;
            this.materialLabel1.Font = new System.Drawing.Font("Roboto", 11F);
            this.materialLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialLabel1.Location = new System.Drawing.Point(115, 74);
            this.materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel1.Name = "materialLabel1";
            this.materialLabel1.Size = new System.Drawing.Size(61, 19);
            this.materialLabel1.TabIndex = 9;
            this.materialLabel1.Text = "Dll Path";
            // 
            // dllPathTextbox
            // 
            this.dllPathTextbox.Depth = 0;
            this.dllPathTextbox.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.dllPathTextbox.Hint = "Input path or double click to select...";
            this.dllPathTextbox.Location = new System.Drawing.Point(14, 96);
            this.dllPathTextbox.MouseState = MaterialSkin.MouseState.HOVER;
            this.dllPathTextbox.Name = "dllPathTextbox";
            this.dllPathTextbox.PasswordChar = '\0';
            this.dllPathTextbox.SelectedText = "";
            this.dllPathTextbox.SelectionLength = 0;
            this.dllPathTextbox.SelectionStart = 0;
            this.dllPathTextbox.Size = new System.Drawing.Size(273, 23);
            this.dllPathTextbox.TabIndex = 10;
            this.dllPathTextbox.UseSystemPasswordChar = false;
            this.dllPathTextbox.DoubleClick += new System.EventHandler(this.dllPathTextbox_DoubleClick);
            // 
            // materialLabel2
            // 
            this.materialLabel2.AutoSize = true;
            this.materialLabel2.BackColor = System.Drawing.Color.White;
            this.materialLabel2.Depth = 0;
            this.materialLabel2.Font = new System.Drawing.Font("Roboto", 11F);
            this.materialLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialLabel2.Location = new System.Drawing.Point(95, 122);
            this.materialLabel2.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel2.Name = "materialLabel2";
            this.materialLabel2.Size = new System.Drawing.Size(108, 19);
            this.materialLabel2.TabIndex = 11;
            this.materialLabel2.Text = "Process Name";
            // 
            // processTextbox
            // 
            this.processTextbox.Depth = 0;
            this.processTextbox.Hint = "";
            this.processTextbox.Location = new System.Drawing.Point(14, 144);
            this.processTextbox.MouseState = MaterialSkin.MouseState.HOVER;
            this.processTextbox.Name = "processTextbox";
            this.processTextbox.PasswordChar = '\0';
            this.processTextbox.SelectedText = "";
            this.processTextbox.SelectionLength = 0;
            this.processTextbox.SelectionStart = 0;
            this.processTextbox.Size = new System.Drawing.Size(273, 23);
            this.processTextbox.TabIndex = 12;
            this.processTextbox.Text = "csgo";
            this.processTextbox.UseSystemPasswordChar = false;
            // 
            // materialDivider1
            // 
            this.materialDivider1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(31)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialDivider1.Depth = 0;
            this.materialDivider1.Location = new System.Drawing.Point(0, 391);
            this.materialDivider1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialDivider1.Name = "materialDivider1";
            this.materialDivider1.Size = new System.Drawing.Size(287, 128);
            this.materialDivider1.TabIndex = 13;
            this.materialDivider1.Text = "materialDivider1";
            // 
            // logBox
            // 
            this.logBox.BackColor = System.Drawing.Color.Black;
            this.logBox.Font = new System.Drawing.Font("Consolas", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.logBox.ForeColor = System.Drawing.Color.Lime;
            this.logBox.Location = new System.Drawing.Point(0, 391);
            this.logBox.Multiline = true;
            this.logBox.Name = "logBox";
            this.logBox.Size = new System.Drawing.Size(301, 128);
            this.logBox.TabIndex = 15;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(301, 519);
            this.Controls.Add(this.logBox);
            this.Controls.Add(this.materialDivider1);
            this.Controls.Add(this.processTextbox);
            this.Controls.Add(this.materialLabel2);
            this.Controls.Add(this.dllPathTextbox);
            this.Controls.Add(this.materialLabel1);
            this.Controls.Add(this.unloadButton);
            this.Controls.Add(this.loadButton);
            this.Controls.Add(this.processListbox);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.Text = "LLLoader";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListBox processListbox;
        private System.Windows.Forms.Timer timer2;
        private MaterialSkin.Controls.MaterialRaisedButton loadButton;
        private MaterialSkin.Controls.MaterialRaisedButton unloadButton;
        private MaterialSkin.Controls.MaterialLabel materialLabel1;
        private MaterialSkin.Controls.MaterialSingleLineTextField dllPathTextbox;
        private MaterialSkin.Controls.MaterialLabel materialLabel2;
        private MaterialSkin.Controls.MaterialSingleLineTextField processTextbox;
        private MaterialSkin.Controls.MaterialDivider materialDivider1;
        private System.Windows.Forms.TextBox logBox;
    }
}

