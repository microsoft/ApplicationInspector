
namespace WinSarifViewer
{
    partial class SarifViewer
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.Runs = new System.Windows.Forms.ComboBox();
            this.titleStatus = new System.Windows.Forms.Label();
            this.TagsCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.RulesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.SeveritiesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.TypesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(8, 7);
            this.button1.Margin = new System.Windows.Forms.Padding(2);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(78, 20);
            this.button1.TabIndex = 0;
            this.button1.Text = "Load Sarif";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(1198, 10);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 15);
            this.label1.TabIndex = 1;
            this.label1.Text = "Select Run:";
            // 
            // Runs
            // 
            this.Runs.FormattingEnabled = true;
            this.Runs.Location = new System.Drawing.Point(1271, 10);
            this.Runs.Margin = new System.Windows.Forms.Padding(2);
            this.Runs.Name = "Runs";
            this.Runs.Size = new System.Drawing.Size(129, 23);
            this.Runs.TabIndex = 2;
            // 
            // titleStatus
            // 
            this.titleStatus.AutoSize = true;
            this.titleStatus.Location = new System.Drawing.Point(116, 10);
            this.titleStatus.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.titleStatus.Name = "titleStatus";
            this.titleStatus.Size = new System.Drawing.Size(0, 15);
            this.titleStatus.TabIndex = 5;
            // 
            // TagsCheckedListBox
            // 
            this.TagsCheckedListBox.CheckOnClick = true;
            this.TagsCheckedListBox.FormattingEnabled = true;
            this.TagsCheckedListBox.Location = new System.Drawing.Point(1131, 110);
            this.TagsCheckedListBox.Name = "TagsCheckedListBox";
            this.TagsCheckedListBox.Size = new System.Drawing.Size(229, 22);
            this.TagsCheckedListBox.TabIndex = 30;
            this.TagsCheckedListBox.Enter += new System.EventHandler(this.TagsCheckedListBox_Enter);
            this.TagsCheckedListBox.Leave += new System.EventHandler(this.TagsCheckedListBox_Leave);
            // 
            // RulesCheckedListBox
            // 
            this.RulesCheckedListBox.FormattingEnabled = true;
            this.RulesCheckedListBox.Location = new System.Drawing.Point(85, 110);
            this.RulesCheckedListBox.Name = "RulesCheckedListBox";
            this.RulesCheckedListBox.Size = new System.Drawing.Size(294, 22);
            this.RulesCheckedListBox.TabIndex = 27;
            this.RulesCheckedListBox.Enter += new System.EventHandler(this.RulesCheckedListBox_Enter);
            this.RulesCheckedListBox.Leave += new System.EventHandler(this.RulesCheckedListBox_Leave);
            // 
            // SeveritiesCheckedListBox
            // 
            this.SeveritiesCheckedListBox.FormattingEnabled = true;
            this.SeveritiesCheckedListBox.Location = new System.Drawing.Point(786, 110);
            this.SeveritiesCheckedListBox.Name = "SeveritiesCheckedListBox";
            this.SeveritiesCheckedListBox.Size = new System.Drawing.Size(294, 22);
            this.SeveritiesCheckedListBox.TabIndex = 29;
            this.SeveritiesCheckedListBox.Enter += new System.EventHandler(this.SeveritiesCheckedListBox_Enter);
            this.SeveritiesCheckedListBox.Leave += new System.EventHandler(this.SeveritiesCheckedListBox_Leave);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label2.Location = new System.Drawing.Point(38, 110);
            this.label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 15);
            this.label2.TabIndex = 23;
            this.label2.Text = "Rules:";
            // 
            // TypesCheckedListBox
            // 
            this.TypesCheckedListBox.FormattingEnabled = true;
            this.TypesCheckedListBox.Location = new System.Drawing.Point(431, 110);
            this.TypesCheckedListBox.Name = "TypesCheckedListBox";
            this.TypesCheckedListBox.Size = new System.Drawing.Size(289, 22);
            this.TypesCheckedListBox.TabIndex = 28;
            this.TypesCheckedListBox.Enter += new System.EventHandler(this.TypesCheckedListBox_Enter);
            this.TypesCheckedListBox.Leave += new System.EventHandler(this.TypesCheckedListBox_Leave);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label3.Location = new System.Drawing.Point(388, 110);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(34, 15);
            this.label3.TabIndex = 24;
            this.label3.Text = "Type:";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label4.Location = new System.Drawing.Point(729, 110);
            this.label4.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(48, 15);
            this.label4.TabIndex = 25;
            this.label4.Text = "Severity";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label5.Location = new System.Drawing.Point(1089, 110);
            this.label5.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 15);
            this.label5.TabIndex = 26;
            this.label5.Text = "Tags:";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBox1.Cursor = System.Windows.Forms.Cursors.Default;
            this.textBox1.Enabled = false;
            this.textBox1.Location = new System.Drawing.Point(8, 70);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.Size = new System.Drawing.Size(1392, 116);
            this.textBox1.TabIndex = 31;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(1285, 147);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 32;
            this.button2.Text = "Filter";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // SarifViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1411, 573);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.TagsCheckedListBox);
            this.Controls.Add(this.RulesCheckedListBox);
            this.Controls.Add(this.SeveritiesCheckedListBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.TypesCheckedListBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.titleStatus);
            this.Controls.Add(this.Runs);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.textBox1);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.Name = "SarifViewer";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.Click += new System.EventHandler(this.SarifViewer_Click);
            this.Enter += new System.EventHandler(this.SarifViewer_Enter);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox Runs;
        private System.Windows.Forms.Label titleStatus;
        private System.Windows.Forms.CheckedListBox TagsCheckedListBox;
        private System.Windows.Forms.CheckedListBox RulesCheckedListBox;
        private System.Windows.Forms.CheckedListBox SeveritiesCheckedListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox TypesCheckedListBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button button2;
    }
}

