using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WinSarifViewerCompanionLibrary;

namespace WinSarifViewer
{
    enum Status
    {
        None,
        Loading,
        Populating
    }

    public partial class WinSarifViewer : Form
    {
        SarifLoader _sarifLoader;
        int numFilteredResults = 0;
        string _resultHTML;

        Status status = Status.None;
        int counter = 0;

        public WinSarifViewer()
        {
            InitializeComponent();
        }

        private void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {

        }

        private void Filter_Click(object sender, EventArgs e)
        {
            string selectedTool = RunSelectComboBox.SelectedItem.ToString();

            var rules = RulesCheckedListBox.CheckedItems.Cast<string>().ToList();
            var types = TypesCheckedListBox.CheckedItems.Cast<string>().ToList();
            var severities = SeveritiesCheckedListBox.CheckedItems.Cast<string>().ToList();
            var tags = TagsCheckedListBox.CheckedItems.Cast<string>().ToList();
            if(_sarifLoader != default)
            {
                SetupWorkForFilteringSarif(selectedTool, rules, types, severities, tags);                
            }
        }

        private void SetupWorkForFilteringSarif(string selectedTool, List<string> rules, List<string> types, List<string> severities, List<string> tags)
        {
            toolStripStatusLabel1.Text = "Filtering results...";
            loadingTimer.Enabled = true;

            //////////////////////////////////////////////
            ///// work run handler
            void loaderWork(object s, DoWorkEventArgs ev) {
                var results = _sarifLoader.GetFilteredResults(selectedTool, rules, types, severities, tags);
                numFilteredResults = results.Count;
                StringBuilder html = _sarifLoader.GenerateHTMLFromResults(selectedTool, results);

                ev.Result = html;
                this.backgroundWorker1.DoWork -= loaderWork;
            };
            this.backgroundWorker1.DoWork += loaderWork;

            //////////////////////////////////////////////
            ///// work finish handler
            void loaderFinish(object s, RunWorkerCompletedEventArgs ev)
            {
                toolStripStatusLabel1.Text = "Filtered results"; 
                StringBuilder result = (StringBuilder)ev.Result;
                _resultHTML = result.ToString();
                webBrowser1.DocumentText = _resultHTML;
                backgroundWorker1.RunWorkerCompleted -= loaderFinish;
                toolStripStatusLabel1.Text = $"Found {numFilteredResults}results matching the filter criterions";

                ResetToolStrip();
            };
            backgroundWorker1.RunWorkerCompleted += loaderFinish;

            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void ReduceListBoxHeight(object sender, EventArgs e)
        {
            ((CheckedListBox)sender).Height = 22;
        }

        private void IncreaseListBoxHeight(object sender, EventArgs e)
        {
            ((CheckedListBox)sender).Height = WinSarifViewer.ActiveForm.Height / 2;
        }

        private void WinSarifViewer_Click(object sender, EventArgs e)
        {
            ReduceListBoxHeight(RulesCheckedListBox, e);
            ReduceListBoxHeight(TypesCheckedListBox, e);
            ReduceListBoxHeight(SeveritiesCheckedListBox, e);
            ReduceListBoxHeight(TagsCheckedListBox, e);
        }

        private void Load_Click(object sender, EventArgs e)
        {

            OpenFileDialog f = new OpenFileDialog();
            var res = f.ShowDialog();

            if (res != DialogResult.OK)
            {
                return;
            }

            string sarifFile = f.FileName;
            SetupWorkForLoadingSarif(sarifFile);
        }

        private void SetupWorkForLoadingSarif(string sarifFile)
        {
            toolStripStatusLabel1.Text = "Loading sarif...";
            loadingTimer.Enabled = true;

            void loaderWork(object s, DoWorkEventArgs ev)
            {
                var sarifLoader = new SarifLoader(sarifFile);
                ev.Result = sarifLoader;
                this.backgroundWorker1.DoWork -= loaderWork;
            }
            backgroundWorker1.DoWork += loaderWork;

            void loaderFinish(object s, RunWorkerCompletedEventArgs ev)
            {
                _sarifLoader = (SarifLoader)ev.Result;

                toolStripStatusLabel1.Text = "Parsing sarif...";
                RunSelectComboBox.Items.AddRange(_sarifLoader.GetRuns());
                RunSelectComboBox.SelectedIndex = 0;
                string selectedTool = RunSelectComboBox.SelectedItem.ToString();
                RulesCheckedListBox.Items.AddRange(_sarifLoader.GetRules(selectedTool));
                TypesCheckedListBox.Items.AddRange(_sarifLoader.GetResultKinds());
                SeveritiesCheckedListBox.Items.AddRange(_sarifLoader.GetFailureLevels());
                TagsCheckedListBox.Items.AddRange(_sarifLoader.GetTags(selectedTool));

                toolStripStatusLabel1.Text = $"Found {_sarifLoader.GetResultCount(selectedTool)} results with {selectedTool}";
                backgroundWorker1.RunWorkerCompleted -= loaderFinish;
                ResetToolStrip();
            }
            backgroundWorker1.RunWorkerCompleted += loaderFinish;

            if (!backgroundWorker1.IsBusy)
            {
                backgroundWorker1.RunWorkerAsync();
            }
        }

        private void ResetToolStrip()
        {
            loadingTimer.Enabled = false;
            toolStripProgressBar1.Value = 0;
        }

        private void loadingTimer_Tick(object sender, EventArgs e)
        {
            toolStripProgressBar1.Value += 1;
        }
    }
}
