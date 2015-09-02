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
            m_thread = new Thread(FindDuplicatedGroups);
            m_thread.Start();
            InitUIUpdates();
        }

        //--------------------------------------------------------------------------------------------
        public void InitUIUpdates()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 100 };
            timer.Tick += delegate
            {
                List<string> res = null;
                while ((res = getFiles()) != null)
                {
                    OnDuplocatedGroupFound(res);
                    if (thread_started == true)
                        break;
                }

                if (!IsRunning())
                {
                    thread_started = true;
                    timer.Stop();
                    if (OnProcessingFinished != null)
                        OnProcessingFinished();
                }
            };

            timer.Start();
        }


        //--------------------------------------------------------------------------------------------
        public void TerminateExecution()
        {
            if (m_thread != null && m_thread.IsAlive)
            {
                thread_started = false;
                m_thread.Join();
            }
        }

        public bool IsRunning()
        {
            return m_thread != null && m_thread.IsAlive;
        }


        private List<string> getFiles()
        {
            List<string> res = null;

            lock (sync)
            {
                if (duplicated_gorupds.Count > 0)
                {
                    res = duplicated_gorupds[duplicated_gorupds.Count - 1];
                    duplicated_gorupds.RemoveAt(duplicated_gorupds.Count - 1);
                }
            }
            return res;
        }


        //--------------------------------------------------------------------------------------------
        Dictionary<string, List<string>> FindDuplicated(List<string> input_files)
        {
            var res = new HashSet<string>();
            var id_path = new Dictionary<string, List<string>>();

            var inp_files_set = new HashSet<string>(input_files);

            foreach (var file_path in input_files)
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
                if (id_path.TryGetValue(hash_str, out str))
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


        private DuplicatedGroupFoundDelegate OnDuplocatedGroupFound = null;

        private Object sync = new Object();
        private Thread m_thread = null;
        private List<List<string>> duplicated_gorupds = new List<List<string>>();
        private SortedDictionary<long, List<string>> files_by_sizes = new SortedDictionary<long, List<string>>();
        private volatile bool thread_started = false;
        private string m_dir_for_processing;

    }
}
