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
using System.Security.Cryptography;
using System.Linq;

namespace DuplicatesFinder
{
    public partial class Form1 : Form
    {
        private Object sync = new Object();
        private Thread m_thread = null;
        private List<List<string>> duplicated_gorupds = new List<List<string>>();
        private SortedDictionary<long, List<string>> files_by_sizes = new SortedDictionary<long, List<string>>();
        private volatile bool thread_started = false;
        private string m_dir_for_processing;
        long totalLength = 0;

        public delegate void OnDuplicatedGroupFound(List<string> group);

        public OnDuplicatedGroupFound deleg = null;



        //--------------------------------------------------------------------------------------------
        Dictionary<string, List<string>> FindDuplicated(List<string> input_files)
        {
            var res = new HashSet<string>();
            var id_path = new Dictionary<string, List<string>>();

            var inp_files_set = new HashSet<string>(input_files);

            foreach(var file_path in input_files)
            {
                if (thread_started == false)
                    break;

                byte[] hash;

                using (var md5_calculator = SHA1.Create())
                {
                    const int buffer_size = 8 * 1024;
                    using (var stream = new BufferedStream(File.OpenRead(file_path), buffer_size))
                    {
                        hash = md5_calculator.ComputeHash(stream);
                    }

                }

                var sb = new StringBuilder();
                for (int i = 0; i < hash.Length; ++i)
                {
                    sb.Append(hash[i].ToString("x2"));
                }
                var hash_str = sb.ToString();
                List<string> str;
                if(id_path.TryGetValue(hash_str, out str))
                {
                    str.Add(file_path);
                }
                else
                {
                    str = new List<string>();
                    str.Add(file_path);
                    id_path.Add(hash_str, str);
                }
            }
            return id_path;
        }

        //--------------------------------------------------------------------------------------------
        void ScanDirectory(string sDir)
        {
            try
            {
                var files = Directory.GetFiles(sDir, "*.*");
                foreach (string f in files)
                {
                    if (thread_started == false)
                        break;

                    var finfo = new FileInfo(f);

                    List<string> list = null;
                    if (files_by_sizes.TryGetValue(finfo.Length, out list))
                    {
                        list.Add(f);
                    }
                    else
                    {
                        list = new List<string>();
                        list.Add(f);
                        files_by_sizes.Add(finfo.Length, list);
                    }
                }

            }
            catch (System.Exception)
            {
            }

            try
            {
                var dirs = Directory.GetDirectories(sDir);
                foreach (string d in dirs)
                {
                    ScanDirectory(d);
                }
            }
            catch (System.Exception)
            {
            }



        }

        //--------------------------------------------------------------------------------------------
        public Form1()
        {
            InitializeComponent();
            deleg = addDuplicatedGroup;
        }

        //--------------------------------------------------------------------------------------------
        private List<string> getFiles()
        {
            List<string> res = null;

            lock (sync)
            {
                if(duplicated_gorupds.Count > 0)
                {
                    res = duplicated_gorupds[duplicated_gorupds.Count - 1];
                    duplicated_gorupds.RemoveAt(duplicated_gorupds.Count - 1);
                }
            }
            return res;
        }

        //--------------------------------------------------------------------------------------------
        private void FindDuplicatedGroups()
        {
            thread_started = true;
            files_by_sizes.Clear();
            ScanDirectory(m_dir_for_processing);

            foreach (var entry in files_by_sizes.Reverse())
            {
                if (entry.Value.Count > 1)
                {
                    var res = FindDuplicated(entry.Value);

                    foreach (var entry2 in res)
                    {
                        if (entry2.Value.Count > 1)
                        {
                            lock (sync)
                            {
                                duplicated_gorupds.Add(entry2.Value);
                            }
                        }
                    }
                }
            }
            thread_started = false;
        }

        //--------------------------------------------------------------------------------------------
        private void addDuplicatedGroup(List<string> group)
        {
            long сLength = -1;

            var finfo = new FileInfo(group[0]);
            var length_mb = ((double)finfo.Length / 1024 / 1024);
            var size = string.Format("{0:0.00}", length_mb);
            var root = treeViewDuplicatedFiles.Nodes;
            var num_nodes = root.Count;
            var node = root.Add("Group " + num_nodes + 1 + ". File size: " + size + " MB");

            foreach (var a in group)
            {
                finfo = new FileInfo(a);
                //node.;
                node.Nodes.Add(a);
                if (сLength != -1)
                    сLength += finfo.Length;
                else
                    сLength = 0;
            }
            node.Expand();

            totalLength += сLength;
            labelDuplicatedSize.Text = string.Format("{0:0.00}", ((double)totalLength / 1024 / 1024));
        }

        //--------------------------------------------------------------------------------------------
        private void labelDirectory_Click_2(object sender, EventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog();
            DialogResult result = fbd.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.labelDirectory.Text = fbd.SelectedPath;
            }
        }

        //--------------------------------------------------------------------------------------------
        private void button1_Click(object sender, EventArgs e)
        {
            m_dir_for_processing = labelDirectory.Text;

            DialogResult answer = DialogResult.None;

            if (m_thread != null && m_thread.IsAlive)
                answer = MessageBox.Show(this, "Terminate and start over?", "", MessageBoxButtons.YesNo);

            if(answer == DialogResult.Yes)
            {
                TerminateExecution();
            }

            if (m_thread == null || !m_thread.IsAlive)
            {
                StartExecution();
                InitUIUpdates();
            }
        }


        //--------------------------------------------------------------------------------------------
        private void StartExecution()
        {
            m_thread = new Thread(FindDuplicatedGroups);
            m_thread.Start();
        }

        //--------------------------------------------------------------------------------------------
        private void InitUIUpdates()
        {
            treeViewDuplicatedFiles.Nodes.Clear();
            var timer = new System.Windows.Forms.Timer { Interval = 100 };
            totalLength = 0;
            timer.Tick += delegate
            {
                if (!progressBar.Visible)
                    progressBar.Show();
                List<string> res = null;
                while ((res = getFiles()) != null)
                {
                    deleg(res);
                    if (thread_started == true)
                        break;
                }

                if (thread_started == false)
                {
                    progressBar.Hide();
                    thread_started = true;
                    timer.Stop();
                }
            };

            timer.Start();
        }


        //--------------------------------------------------------------------------------------------
        private void TerminateExecution()
        {
            if (m_thread != null && m_thread.IsAlive)
            {
                thread_started = false;
                m_thread.Join();
            }
        }

        //--------------------------------------------------------------------------------------------
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            TerminateExecution();
        }

    }
}
