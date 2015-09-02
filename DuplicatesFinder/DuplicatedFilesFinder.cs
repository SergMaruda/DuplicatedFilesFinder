using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.IO;

namespace DuplicatesFinder
{
    public class DuplicatesFinderEngine
    {
        public delegate void DuplicatedGroupFoundDelegate(List<string> group);
        public delegate void ProcessingFinishedDelegate();

        public DuplicatesFinderEngine(DuplicatedGroupFoundDelegate i_delgate)
        {
            if (i_delgate == null)
                throw new System.Exception("Delegate for duplicated gorup should be specified");

            OnDuplocatedGroupFound = i_delgate;
        }

        public ProcessingFinishedDelegate OnProcessingFinished = null;

        //--------------------------------------------------------------------------------------------
        public void StartExecution(string i_directory)
        {
            m_dir_for_processing = i_directory;
            m_processing_thread = new Thread(FindDuplicatedGroups);
            m_processing_thread.Start();
            InitUIUpdates();
        }


        //--------------------------------------------------------------------------------------------
        public void TerminateExecution()
        {
            if (m_processing_thread != null && m_processing_thread.IsAlive)
            {
                m_thread_stop_flag = true;
                m_processing_thread.Join();
            }
        }

        //--------------------------------------------------------------------------------------------
        public bool IsRunning()
        {
            return m_processing_thread != null && m_processing_thread.IsAlive;
        }

        //--------------------------------------------------------------------------------------------
        private List<string> getNextDuplicatedGroupOfFiles()
        {
            lock (m_mutex)
            {
                if (m_duplicated_gorups.Count > 0)
                {
                    var count = m_duplicated_gorups.Count;
                    var res = m_duplicated_gorups[count - 1];
                    m_duplicated_gorups.RemoveAt(count - 1);
                    return res;
                }
            }
            return null;
        }

        //--------------------------------------------------------------------------------------------
        private void InitUIUpdates()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += delegate
            {
                List<string> duplicated_group = null;
                while ((duplicated_group = getNextDuplicatedGroupOfFiles()) != null)
                {
                    OnDuplocatedGroupFound(duplicated_group);
                    if (IsRunning())
                        break;
                }

                if (!IsRunning())
                {
                    timer.Stop();
                    if (OnProcessingFinished != null)
                        OnProcessingFinished();
                }
            };

            timer.Start();
        }


        //--------------------------------------------------------------------------------------------
        private Dictionary<string, List<string>> FindDuplicated(List<string> i_input_files)
        {
            var duplicated_files_groups = new Dictionary<string, List<string>>();
            var inp_files_set = new HashSet<string>(i_input_files);

            foreach (var file_path in i_input_files)
            {
                if (m_thread_stop_flag)
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
                if (duplicated_files_groups.TryGetValue(hash_str, out str))
                {
                    str.Add(file_path);
                }
                else
                {
                    str = new List<string>();
                    str.Add(file_path);
                    duplicated_files_groups.Add(hash_str, str);
                }
            }
            return duplicated_files_groups;
        }

        //--------------------------------------------------------------------------------------------
        private void FindDuplicatedGroups()
        {
            try
            {
                m_thread_stop_flag = false;
                m_files__grouped_by_sizes.Clear();
                ScanDirectory(m_dir_for_processing);

                foreach (var entry in m_files__grouped_by_sizes.Reverse())
                {
                    if (entry.Value.Count > 1)
                    {
                        var res = FindDuplicated(entry.Value);

                        foreach (var entry2 in res)
                        {
                            if (entry2.Value.Count > 1)
                            {
                                lock (m_mutex)
                                {
                                    m_duplicated_gorups.Add(entry2.Value);
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                m_thread_stop_flag = true;
            }
        }

        //--------------------------------------------------------------------------------------------
        void ScanDirectory(string i_directory)
        {
            try
            {
                var files = Directory.GetFiles(i_directory, "*.*");
                foreach (string f in files)
                {
                    if (m_thread_stop_flag)
                        break;

                    var finfo = new FileInfo(f);

                    List<string> list = null;
                    if (m_files__grouped_by_sizes.TryGetValue(finfo.Length, out list))
                    {
                        list.Add(f);
                    }
                    else
                    {
                        list = new List<string>();
                        list.Add(f);
                        m_files__grouped_by_sizes.Add(finfo.Length, list);
                    }
                }

            }
            catch (System.Exception)
            {
            }

            try
            {
                var dirs = Directory.GetDirectories(i_directory);
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
        private DuplicatedGroupFoundDelegate OnDuplocatedGroupFound = null;
        private Object m_mutex = new Object();
        private Thread m_processing_thread = null;
        private List<List<string>> m_duplicated_gorups = new List<List<string>>();
        private SortedDictionary<long, List<string>> m_files__grouped_by_sizes = new SortedDictionary<long, List<string>>();
        private volatile bool m_thread_stop_flag = false;
        private string m_dir_for_processing;

    }
}
