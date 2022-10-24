using SarifCompanionLibrary;
using System;
using System.Windows.Forms;

namespace WinSarifViewer
{
    public partial class SarifViewer : Form
    {
        public SarifViewer()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog f = new OpenFileDialog();
            var res = f.ShowDialog();

            if (res != DialogResult.OK)
            {
                return;
            }

            string sarifFile = f.FileName;
            SarifLoader sarifLoader = new SarifLoader(sarifFile);
            Runs.Items.AddRange(sarifLoader.GetRuns());
            Runs.SelectedIndex = 0;
            string selectedTool = Runs.SelectedItem.ToString();
            titleStatus.Text = $"Found {sarifLoader.GetResultCount(selectedTool)} results with {selectedTool}";
            RulesCheckedListBox.Items.AddRange(sarifLoader.GetRules(selectedTool));
            TypesCheckedListBox.Items.AddRange(sarifLoader.GetResultKinds());
            SeveritiesCheckedListBox.Items.AddRange(sarifLoader.GetFailureLevels());
            TagsCheckedListBox.Items.AddRange(sarifLoader.GetTags(selectedTool));
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void RulesCheckedListBox_Leave(object sender, EventArgs e)
        {
            ReduceListBoxHeight(sender);
        }

        private void RulesCheckedListBox_Enter(object sender, EventArgs e)
        {
            IncreaseListBoxHeight(sender);
        }

        private void ReduceListBoxHeight(object sender)
        {
            ((CheckedListBox)sender).Height = 22;

        }
        private void IncreaseListBoxHeight(object sender)
        {
            ((CheckedListBox)sender).Height = SarifViewer.ActiveForm.Height / 2;
        }

        private void TypesCheckedListBox_Enter(object sender, EventArgs e)
        {
            IncreaseListBoxHeight(sender);
        }

        private void TypesCheckedListBox_Leave(object sender, EventArgs e)
        {
            ReduceListBoxHeight(sender);
        }

        private void SeveritiesCheckedListBox_Enter(object sender, EventArgs e)
        {
            IncreaseListBoxHeight(sender);
        }

        private void SeveritiesCheckedListBox_Leave(object sender, EventArgs e)
        {
            ReduceListBoxHeight(sender);
        }

        private void TagsCheckedListBox_Enter(object sender, EventArgs e)
        {
            IncreaseListBoxHeight(sender);
        }

        private void TagsCheckedListBox_Leave(object sender, EventArgs e)
        {
            ReduceListBoxHeight(sender);
        }

        private void SarifViewer_Enter(object sender, EventArgs e)
        {
        }

        private void SarifViewer_Click(object sender, EventArgs e)
        {
            ReduceListBoxHeight(RulesCheckedListBox);
            ReduceListBoxHeight(TypesCheckedListBox);
            ReduceListBoxHeight(SeveritiesCheckedListBox);
            ReduceListBoxHeight(TagsCheckedListBox);
        }
    }
}
