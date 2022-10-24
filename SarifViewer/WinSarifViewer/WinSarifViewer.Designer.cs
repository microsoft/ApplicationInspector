
namespace WinSarifViewer
{
    partial class WinSarifViewer
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
            this.webBrowser1 = new System.Windows.Forms.WebBrowser();
            this.Filter = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.RulesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.TypesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label2 = new System.Windows.Forms.Label();
            this.SeveritiesCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label3 = new System.Windows.Forms.Label();
            this.TagsCheckedListBox = new System.Windows.Forms.CheckedListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.Load = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.RunSelectComboBox = new System.Windows.Forms.ComboBox();
            this.StatusLabel = new System.Windows.Forms.Label();
            this.backgroundWorker1 = new System.ComponentModel.BackgroundWorker();
            this.loadingTimer = new System.Windows.Forms.Timer(this.components);
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripProgressBar1 = new System.Windows.Forms.ToolStripProgressBar();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // webBrowser1
            // 
            this.webBrowser1.Location = new System.Drawing.Point(12, 126);
            this.webBrowser1.MinimumSize = new System.Drawing.Size(20, 20);
            this.webBrowser1.Name = "webBrowser1";
            this.webBrowser1.Size = new System.Drawing.Size(1261, 400);
            this.webBrowser1.TabIndex = 0;
            this.webBrowser1.DocumentCompleted += new System.Windows.Forms.WebBrowserDocumentCompletedEventHandler(this.webBrowser1_DocumentCompleted);
            // 
            // Filter
            // 
            this.Filter.Location = new System.Drawing.Point(1162, 80);
            this.Filter.Name = "Filter";
            this.Filter.Size = new System.Drawing.Size(75, 23);
            this.Filter.TabIndex = 1;
            this.Filter.Text = "Filter";
            this.Filter.UseVisualStyleBackColor = true;
            this.Filter.Click += new System.EventHandler(this.Filter_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label1.Location = new System.Drawing.Point(64, 80);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Rules:";
            // 
            // RulesCheckedListBox
            // 
            this.RulesCheckedListBox.CheckOnClick = true;
            this.RulesCheckedListBox.FormattingEnabled = true;
            this.RulesCheckedListBox.Location = new System.Drawing.Point(130, 80);
            this.RulesCheckedListBox.Name = "RulesCheckedListBox";
            this.RulesCheckedListBox.Size = new System.Drawing.Size(166, 19);
            this.RulesCheckedListBox.TabIndex = 3;
            this.RulesCheckedListBox.Enter += new System.EventHandler(this.IncreaseListBoxHeight);
            this.RulesCheckedListBox.Leave += new System.EventHandler(this.ReduceListBoxHeight);
            // 
            // TypesCheckedListBox
            // 
            this.TypesCheckedListBox.FormattingEnabled = true;
            this.TypesCheckedListBox.Location = new System.Drawing.Point(391, 80);
            this.TypesCheckedListBox.Name = "TypesCheckedListBox";
            this.TypesCheckedListBox.Size = new System.Drawing.Size(166, 19);
            this.TypesCheckedListBox.TabIndex = 5;
            this.TypesCheckedListBox.Enter += new System.EventHandler(this.IncreaseListBoxHeight);
            this.TypesCheckedListBox.Leave += new System.EventHandler(this.ReduceListBoxHeight);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label2.Location = new System.Drawing.Point(325, 80);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Types:";
            // 
            // SeveritiesCheckedListBox
            // 
            this.SeveritiesCheckedListBox.FormattingEnabled = true;
            this.SeveritiesCheckedListBox.Location = new System.Drawing.Point(652, 80);
            this.SeveritiesCheckedListBox.Name = "SeveritiesCheckedListBox";
            this.SeveritiesCheckedListBox.Size = new System.Drawing.Size(166, 19);
            this.SeveritiesCheckedListBox.TabIndex = 7;
            this.SeveritiesCheckedListBox.Enter += new System.EventHandler(this.IncreaseListBoxHeight);
            this.SeveritiesCheckedListBox.Leave += new System.EventHandler(this.ReduceListBoxHeight);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label3.Location = new System.Drawing.Point(586, 80);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 13);
            this.label3.TabIndex = 6;
            this.label3.Text = "Severities:";
            // 
            // TagsCheckedListBox
            // 
            this.TagsCheckedListBox.FormattingEnabled = true;
            this.TagsCheckedListBox.Location = new System.Drawing.Point(913, 80);
            this.TagsCheckedListBox.Name = "TagsCheckedListBox";
            this.TagsCheckedListBox.Size = new System.Drawing.Size(166, 19);
            this.TagsCheckedListBox.TabIndex = 9;
            this.TagsCheckedListBox.Enter += new System.EventHandler(this.IncreaseListBoxHeight);
            this.TagsCheckedListBox.Leave += new System.EventHandler(this.ReduceListBoxHeight);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label4.Location = new System.Drawing.Point(847, 80);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "Tags:";
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.textBox1.Location = new System.Drawing.Point(12, 33);
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(1246, 87);
            this.textBox1.TabIndex = 10;
            // 
            // Load
            // 
            this.Load.BackColor = System.Drawing.SystemColors.ControlDark;
            this.Load.Location = new System.Drawing.Point(27, 4);
            this.Load.Name = "Load";
            this.Load.Size = new System.Drawing.Size(75, 23);
            this.Load.TabIndex = 11;
            this.Load.Text = "Load Sarif";
            this.Load.UseVisualStyleBackColor = false;
            this.Load.Click += new System.EventHandler(this.Load_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(1071, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(63, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Select Run:";
            // 
            // RunSelectComboBox
            // 
            this.RunSelectComboBox.FormattingEnabled = true;
            this.RunSelectComboBox.Location = new System.Drawing.Point(1137, 6);
            this.RunSelectComboBox.Name = "RunSelectComboBox";
            this.RunSelectComboBox.Size = new System.Drawing.Size(121, 21);
            this.RunSelectComboBox.TabIndex = 13;
            // 
            // StatusLabel
            // 
            this.StatusLabel.AutoSize = true;
            this.StatusLabel.Location = new System.Drawing.Point(12, 547);
            this.StatusLabel.Name = "StatusLabel";
            this.StatusLabel.Size = new System.Drawing.Size(10, 13);
            this.StatusLabel.TabIndex = 15;
            this.StatusLabel.Text = " ";
            // 
            // loadingTimer
            // 
            this.loadingTimer.Interval = 1000;
            this.loadingTimer.Tick += new System.EventHandler(this.loadingTimer_Tick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1,
            this.toolStripProgressBar1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 537);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1285, 22);
            this.statusStrip1.TabIndex = 16;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(183, 17);
            this.toolStripStatusLabel1.Text = "Open a sarif file to see the results.";
            // 
            // toolStripProgressBar1
            // 
            this.toolStripProgressBar1.Name = "toolStripProgressBar1";
            this.toolStripProgressBar1.Size = new System.Drawing.Size(100, 16);
            this.toolStripProgressBar1.Step = 2;
            this.toolStripProgressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            // 
            // WinSarifViewer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1285, 559);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.StatusLabel);
            this.Controls.Add(this.RunSelectComboBox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.Load);
            this.Controls.Add(this.TagsCheckedListBox);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.SeveritiesCheckedListBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.TypesCheckedListBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.RulesCheckedListBox);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.Filter);
            this.Controls.Add(this.webBrowser1);
            this.Controls.Add(this.textBox1);
            this.Name = "WinSarifViewer";
            this.Text = "WinSarifViewer";
            this.Click += new System.EventHandler(this.WinSarifViewer_Click);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.WebBrowser webBrowser1;
        private System.Windows.Forms.Button Filter;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckedListBox RulesCheckedListBox;
        private System.Windows.Forms.CheckedListBox TypesCheckedListBox;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckedListBox SeveritiesCheckedListBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckedListBox TagsCheckedListBox;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button Load;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox RunSelectComboBox;
        private System.Windows.Forms.Label StatusLabel;
        private System.ComponentModel.BackgroundWorker backgroundWorker1;
        private System.Windows.Forms.Timer loadingTimer;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripProgressBar toolStripProgressBar1;
    }
}

