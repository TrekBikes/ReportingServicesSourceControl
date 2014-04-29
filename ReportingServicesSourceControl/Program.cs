using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.IO;
using System.Collections.Specialized;
using System.Net.Mail;

namespace ReportingServicesSourceControl
{
    class Program
    {
        enum SourceControlProvider { git, SVN };

        private static bool _emailOnError;
        private static string _emailServer;
        private static string _emailFrom;
        private static string _emailTo;
        private static bool _alertOnEmbeddedDataSource;

        static void Main(string[] args)
        {
            _emailOnError = bool.Parse(ConfigurationManager.AppSettings["EmailOnError"]);
            _emailServer = ConfigurationManager.AppSettings["EmailServer"];
            _emailFrom = ConfigurationManager.AppSettings["EmailFrom"];
            _emailTo = ConfigurationManager.AppSettings["EmailTo"];
            _alertOnEmbeddedDataSource = bool.Parse(ConfigurationManager.AppSettings["AlertOnEmbeddedDataSource"]);

            string lastServer = "Unknown";

            SourceControlProvider scp = (SourceControlProvider)System.Enum.Parse(typeof(SourceControlProvider), ConfigurationManager.AppSettings["SourceControl"]);

            SourceControl.ISourceControl sc;

            if (scp == SourceControlProvider.git)
            {
                sc = new SourceControl.git();
            }
            else if (scp == SourceControlProvider.SVN)
            {
                sc = new SourceControl.SVN(true);
            }
            else
            {
                throw new Exception("Bad SourceControlProvider");
            }



            try
            {
                ServerConfiguration reportServers = (ServerConfiguration)ConfigurationManager.GetSection("reportServers");
                string rootPath = ConfigurationManager.AppSettings["rootPath"];

                foreach (ServerElement s in reportServers.Servers)
                {
                    string serverPath = rootPath + "\\" + s.Name;
                    if (!Directory.Exists(serverPath))
                    {
                        throw new Exception(string.Format("Server rootPath path not found ({0})", serverPath));
                    }

                    lastServer = s.Url;

                    ReportingServices.IReportService rs;
                    if (s.UseDefaultCredentials)
                    {
                        rs = new ReportingServices.MSRS2005(s.Url, _alertOnEmbeddedDataSource);
                    }
                    else
                    {
                        rs = new ReportingServices.MSRS2005(s.Url, s.Username, s.Password, s.Domain);
                    }

                    System.Console.WriteLine("START: Getting ObjectTrees");
                    SourceControl.ObjectTree ot = GetObjectTree("", rs);
                    System.Console.WriteLine("END: Getting ObjectTrees");

                    //DumpObjectTree(ot);
                    sc.LogObjectTree(serverPath + "\\Reports", ot);

                    sc.Commit(serverPath);
                }
            }
            catch (Exception ex)
            {
                if (_emailOnError)
                {
                    string Subject = "ReportingServicesSourceControl Failed";
                    string Body = "Running on: " + Environment.MachineName + "\r\nLast Server: " + lastServer + "\r\n" + ex.Message + " " + ex.InnerException + " " + ex.StackTrace;
                    SendMail(Subject, Body);

                }
                else
                {
                    Console.WriteLine(ex.Message + " " + ex.InnerException + " " + ex.StackTrace);
                    throw ex;
                }
            }
        }

        public static void DumpObjectTree(SourceControl.ObjectTree ot)
        {
            System.Console.WriteLine("== Folder: " + ot.Name);

            foreach (string key in ot.Objects)
            {
                System.Console.WriteLine(key);
            }

            foreach (SourceControl.ObjectTree childOT in ot.ObjectTrees)
            {
                DumpObjectTree(childOT);
            }
        }

        public static SourceControl.ObjectTree GetObjectTree(string Path, ReportingServices.IReportService RS)
        {
            SourceControl.ObjectTree ot = new SourceControl.ObjectTree();

            ot.Name = Path;

            NameValueCollection reports = RS.GetItems(Path);
            foreach (string key in reports)
            {
                ot.Objects.Add(key, reports[key]);
            }
            
            List<string> folders = RS.GetFolders(Path);
            foreach (string folder in folders)
            {
                if (folder != "Users Folders")
                {
                    //System.Console.WriteLine("Getting ObjectTree For: " + Path + "/" + folder + "...");
                    ot.ObjectTrees.Add(GetObjectTree(Path + "/" + folder, RS));
                }
                else
                {
                    System.Console.WriteLine("Skipping folder: " + folder);
                }
            }


            return ot;
        }

        public static void SendMail(string Subject, string Body)
        {
            SendMail(_emailServer, _emailFrom, _emailTo, Subject, Body, true);
        }

        public static void SendMail(string MailServer, string From, string To, string Subject, string Body, bool IsHtml)
        {
            MailMessage msg;
            using (msg = new MailMessage())
            {
                msg.To.Add(To);
                msg.From = new MailAddress(From);
                msg.Subject = Subject;
                msg.Body = Body;
                msg.IsBodyHtml = IsHtml;

                var smtpClient = new SmtpClient(MailServer);

                smtpClient.Send(msg);
            }
        }
    }
}
