using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Collections.Specialized;
using System.IO;
using System.Configuration;

namespace ReportingServicesSourceControl.SourceControl
{
    class SVN : ISourceControl
    {
        bool _svnAuth = false;
        string _username;
        string _password;
        private static readonly log4net.ILog _logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public SVN(bool SVNAuth)
        {
            NameValueCollection appSettings = System.Configuration.ConfigurationManager.AppSettings;

            _svnAuth = SVNAuth;
            _username = ConfigurationManager.AppSettings["sourceControlUsername"];
            _password = ConfigurationManager.AppSettings["sourceControlPassword"];

        }

        public bool Add(string Path)
        {
            return ExecuteCommand("add", Path);
        }

        public bool Delete(string Path) {
            return ExecuteCommand("delete", Path);
        }

        public bool Update(string Path)
        {
            return true;
        }

        public bool Commit(string Path)
        {
            return ExecuteCommand("commit", Path, "");
        }

        private bool ExecuteCommand(string Command, string Path)
        {
            return ExecuteCommand(Command, Path, null);
        }

        private bool ExecuteCommand(string Command, string Path, string Message) 
        {
            ProcessStartInfo svnCmd = new ProcessStartInfo();
            svnCmd.FileName = "svn.exe";
            svnCmd.CreateNoWindow = true;
            svnCmd.WindowStyle = ProcessWindowStyle.Hidden;

            if (Message != null)
            {
                Message = string.Format(" --message \"{0}\"", Message);
            }
            else
            {
                Message = "";
            }

            // svn add
            if (_svnAuth)
            {
                svnCmd.Arguments = string.Format("{0} \"{1}\"{2} --username {3} --password {4}", Command, Path, Message, _username, _password);
            }
            else
            {
                svnCmd.Arguments = string.Format("{0} \"{1}\"{2} ", Command, Path, Message);
            }

            //System.Console.WriteLine("svn.exe " + svnCmd.Arguments);
            _logger.Debug("svn.exe " + svnCmd.Arguments);

            Process pCmd = Process.Start(svnCmd);

            //Wait for the process to end.
            pCmd.WaitForExit();
            return true;
        }

        public bool LogObjects(string BasePath, string ObjectTypeName, NameValueCollection Objects)
        {
            return LogObjects(BasePath, ObjectTypeName, Objects, false);
        }

        public bool LogObjects(string BasePath, string ObjectTypeName, NameValueCollection Objects, bool IsParentNew)
        {
            string fullPath = BasePath + @"\" + ObjectTypeName;
            fullPath = fullPath.Replace("/", @"\").Replace(@"\\", @"\");


            DirectoryInfo d = new DirectoryInfo(fullPath);
            FileInfo[] files = d.GetFiles("*.*");

            List<string> fileNames = new List<string>();
            List<string> objectFileNames = new List<string>();

            foreach (FileInfo file in files)
            {
                fileNames.Add(file.Name);
            }

            foreach (string key in Objects)
            {
                //System.Console.WriteLine(key);
                string filename = (fullPath + "\\" + key.Replace(@"/", "_").Replace(@"\", "_")).Replace(@"\\", @"\");
                if (!System.Text.RegularExpressions.Regex.IsMatch(key, @"\.\w+$"))
                {
                    filename += ".rdl";
                    objectFileNames.Add(key.Replace(@"/", "_").Replace(@"\", "_") + ".rdl");
                }
                else
                {
                    objectFileNames.Add(key.Replace(@"/", "_").Replace(@"\", "_"));
                }

                if (filename.Length < 260)
                {
                    //System.Console.WriteLine("Working with: " + filename);
                    _logger.Debug(String.Format("Working with file {0}",filename));
                    string newFile = Objects[key];

                    bool writeFile = true;
                    bool addFile = false;

                    if (!IsParentNew)
                    {

                        if (!File.Exists(filename))
                        {
                            addFile = true;
                        }
                        else
                        {

                            StreamReader sr = new StreamReader(filename);

                            string existingFile = sr.ReadToEnd();
                            sr.Close();

                            if (existingFile != newFile)
                            {
                                //System.Console.WriteLine("Updating: " + filename);
                                _logger.Debug(String.Format("Updating file {0}",filename));
                                Update(filename);
                            }
                            else
                            {
                                writeFile = false;
                            }
                        }
                    }

                    if (writeFile)
                    {
                        StreamWriter sw = new StreamWriter(filename, false);

                        sw.Write(newFile);
                        sw.Flush();
                        sw.Close();
                    }

                    if (addFile)
                    {
                        //System.Console.WriteLine("Adding File: " + filename);
                        _logger.Debug(String.Format("Adding file {0}",filename));
                        Add(filename);
                    }
                }
                else
                {
                    //System.Console.WriteLine("Filename \"" + filename + "\" is greater than 260 characters, skipping");
                    _logger.Warn("Filename \"" + filename + "\" is greater than 260 characters, skipping");
                }
            }

            // handle the deletes for any files - stored procedures
            foreach (string file in fileNames)
            {
                if (objectFileNames.IndexOf(file) == -1)
                {
                    string filename = fullPath + @"\" + file;
                    filename = filename.Replace(@"\\", @"\");
                    //System.Console.WriteLine("Deleting File: " + filename);
                    _logger.Debug(String.Format("Deleting file {0}",filename));
                    Delete(filename);

                }
            }

            return true;
        }
        
        public bool LogObjectTree(string BasePath, ObjectTree Tree) 
        {
            return LogObjectTree(BasePath, Tree, false);
        }

        private bool LogObjectTree(string BasePath, ObjectTree Tree, bool IsParentNew)
        {
            string fullPath = (BasePath + @"\" + Tree.Name).Replace("/", @"\").Replace(@"\\", @"\");
            bool IsThisNew = false;

            if (!Directory.Exists(fullPath))
            {
                //System.Console.WriteLine("Creating Directory: " + fullPath);
                _logger.Debug(String.Format("Creating Directory {0}",fullPath));
                Directory.CreateDirectory(fullPath);
                IsThisNew = true;
            }

            LogObjects(BasePath, Tree.Name, Tree.Objects, IsThisNew);

            foreach(ObjectTree ot in Tree.ObjectTrees)
            {
                LogObjectTree(BasePath, ot, IsThisNew);
            }

            if (IsThisNew && !IsParentNew)
            {
                //System.Console.WriteLine("Adding Folder: " + fullPath);
                _logger.Debug(String.Format("Adding Folder {0}",fullPath));
                Add(System.Text.RegularExpressions.Regex.Replace(fullPath,@"\\$",""));
            }

            return true;
        }
    }
}
