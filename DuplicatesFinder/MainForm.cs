using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;


namespace DuplicatesFinder
{
    public partial class MainForm : Form
    {
        DuplicatesFinderEngine duplicated_files_finder;
        long totalLength = 0;


        //--------------------------------------------------------------------------------------------
        public MainForm()
        {
            InitializeComponent();

            duplicated_files_finder = new DuplicatesFinderEngine(addDuplicatedGroup);
            duplicated_files_finder.OnProcessingFinished = ProcessingFinished;
        }

        //--------------------------------------------------------------------------------------------
        private void addDuplicatedGroup(List<string> duplicated_group)
        {
            long duplicatedSizeInBytes = -1;

            var finfo = new System.IO.FileInfo(duplicated_group[0]);
            var duplicatedSizeInMBytes = ((double)finfo.Length / 1024 / 1024);
            var size = string.Format("{0:0.00}", duplicatedSizeInMBytes);
            var root = treeViewDuplicatedFiles.Nodes;
            var num_nodes = root.Count;
            var node = root.Add("Group " + num_nodes + 1 + ". File size: " + size + " MB");

            foreach (var a in duplicated_group)
            {
                finfo = new FileInfo(a);
                //node.;
                node.Nodes.Add(a);
                if (duplicatedSizeInBytes != -1)
                    duplicatedSizeInBytes += finfo.Length;
                else
                    duplicatedSizeInBytes = 0;
            }
            node.Expand();
            totalLength += duplicatedSizeInBytes;
            labelDuplicatedSize.Text = string.Format("{0:0.00}", ((double)totalLength / 1024 / 1024));
        }

        //--------------------------------------------------------------------------------------------
        private void OnSelectDirectory(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.labelDirectory.Text = fbd.SelectedPath;
            }
        }

        //--------------------------------------------------------------------------------------------
        private void ButtonRunClicked(object sender, EventArgs e)
        {
            DialogResult answer = DialogResult.None;

            if (duplicated_files_finder.IsRunning())
                answer = MessageBox.Show(this, "Terminate and start over?", "", MessageBoxButtons.YesNo);

            if(answer == DialogResult.Yes)
            {
                duplicated_files_finder.TerminateExecution();
            }

            if (!duplicated_files_finder.IsRunning())
            {
                if (!progressBar.Visible)
                    progressBar.Show();

                treeViewDuplicatedFiles.Nodes.Clear();

                totalLength = 0;
                duplicated_files_finder.StartExecution(labelDirectory.Text);
            }
        }

        private void ProcessingFinished()
        {
            progressBar.Hide();
        }

        //--------------------------------------------------------------------------------------------
        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            duplicated_files_finder.TerminateExecution();
        }

    }
}
