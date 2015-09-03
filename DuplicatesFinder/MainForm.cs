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
        DuplicatesFinderEngine m_duplicated_files_finder;
        long m_total_size_duplicated = 0;


        //--------------------------------------------------------------------------------------------
        public MainForm()
        {
            InitializeComponent();

            m_duplicated_files_finder = new DuplicatesFinderEngine(addDuplicatedGroup);
            m_duplicated_files_finder.OnProcessingFinished = ProcessingFinished;
            m_duplicated_files_finder.OnProgressStarted = ProgressStarted;
            m_duplicated_files_finder.OnProgressChanged = ProgressChanged;
        }

        private static string ConvertToMbStr(long i_size_in_Kb)
        {
            var SizeInMBytes = ((double)i_size_in_Kb / 1024 / 1024);
            return string.Format("{0:0.00}", SizeInMBytes);
        }

        //--------------------------------------------------------------------------------------------
        private void addDuplicatedGroup(List<string> duplicated_group)
        {
            long duplicatedSizeInBytes = -1;

            var finfo = new System.IO.FileInfo(duplicated_group[0]);
            var size = ConvertToMbStr(finfo.Length);
            var root = treeViewDuplicatedFiles.Nodes;
            var node_number = root.Count + 1;
            var node = root.Add("Group " + node_number + ". File size: " + size + " MB");

            foreach (var a in duplicated_group)
            {
                finfo = new FileInfo(a);

                node.Nodes.Add(a);
                if (duplicatedSizeInBytes != -1)
                    duplicatedSizeInBytes += finfo.Length;
                else
                    duplicatedSizeInBytes = 0;
            }
            node.Expand();
            m_total_size_duplicated += duplicatedSizeInBytes;
            labelDuplicatedSize.Text = ConvertToMbStr(m_total_size_duplicated);
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
            DialogResult answer = DialogResult.Yes;

            if (m_duplicated_files_finder.IsRunning())
                answer = MessageBox.Show(this, "Terminate and start over?", "", MessageBoxButtons.YesNo);

            if(answer != DialogResult.Yes)
                return;

            treeViewDuplicatedFiles.Nodes.Clear();
            m_total_size_duplicated = 0;

            m_duplicated_files_finder.StartExecution(labelDirectory.Text);
            progressBar.Show();
        }

        //--------------------------------------------------------------------------------------------
        private void ProgressStarted(DuplicatesFinderEngine.ProgressType i_progress_type)
        {
            if (i_progress_type == DuplicatesFinderEngine.ProgressType.Indeterminate)
                progressBar.Style = ProgressBarStyle.Marquee;
            else if (i_progress_type == DuplicatesFinderEngine.ProgressType.Determinate)
                progressBar.Style = ProgressBarStyle.Continuous;
        }

        //--------------------------------------------------------------------------------------------
        private void ProgressChanged(int progress)
        {
            progressBar.Value = progress;
        }

        //--------------------------------------------------------------------------------------------
        private void ProcessingFinished()
        {
            progressBar.Hide();
        }

        //--------------------------------------------------------------------------------------------
        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            m_duplicated_files_finder.TerminateExecution();
        }
    }
}
