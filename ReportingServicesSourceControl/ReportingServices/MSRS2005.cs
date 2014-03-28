using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using ReportingServicesSourceControl.ReportingServices.Proxy.ReportingServices2005;

namespace ReportingServicesSourceControl.ReportingServices
{
    class MSRS2005 : IReportService
    {
        ReportingService2005 _svc = new ReportingService2005();
        bool _alertOnEmbeddedDataSource = false;

        public MSRS2005(string Url, bool AlertOnEmbeddedDataSource)
        {
            _svc.Url = Url;
            _svc.UseDefaultCredentials = true;
            _alertOnEmbeddedDataSource = AlertOnEmbeddedDataSource;
        }

        public MSRS2005(string Url, string Username, string Password, string Domain)
        {
            _svc.Url = Url;
            _svc.Credentials = new System.Net.NetworkCredential(Username, Password, Domain);
        }

        public NameValueCollection GetItems(string Path)
        {
            NameValueCollection nv = new NameValueCollection();
            try
            {

                if (string.IsNullOrEmpty(Path))
                {
                    Path = "/";
                    nv.Add("Home." + "FolderSecurity", GetItemSecurity("/"));
                }

                foreach (CatalogItem item in _svc.ListChildren(Path, false))
                {
                    try
                    {
                        if (item.Type == ItemTypeEnum.Report)
                        {
                            if (_alertOnEmbeddedDataSource)
                            {
                                foreach (DataSource ds in _svc.GetItemDataSources(item.Path))
                                {
                                    if (!(ds.Item is DataSourceReference || ds.Item is InvalidDataSourceReference))
                                    {
                                        string Subject = "[SQLSchemaSourceControl] Embedded Data Source Detected";
                                        string Body = string.Format("Report: {0}\nType: {1}", item.Path, ds.Item.GetType().ToString());
                                        ReportingServicesSourceControl.Program.SendMail(Subject, Body);
                                    }
                                }
                            }

                            System.IO.MemoryStream ms = new System.IO.MemoryStream(_svc.GetReportDefinition(item.Path));
                            System.IO.BinaryReader br = new System.IO.BinaryReader(ms);
                            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
                            xml.Load(ms);

                            StringBuilder sb = new StringBuilder();
                            using (System.Xml.XmlTextWriter xtw = new System.Xml.XmlTextWriter(new System.IO.StringWriter(sb)))
                            {
                                xtw.Formatting = System.Xml.Formatting.Indented;
                                xml.Save(xtw);
                            }

                            string xmlText = sb.ToString();
                            xmlText = xmlText.Replace(" encoding=\"utf-16\"", "");

                            nv.Add(item.Name, xmlText);

                            foreach (Subscription s in _svc.ListSubscriptions(item.Path, null))
                            {
                                string subscriptionText = string.Format("Description: {0}\nEventType: {1}\nExtension: {2}\n\n"
                                        ,s.Description
                                        ,s.EventType
                                        ,s.DeliverySettings.Extension );

                                ParameterValueOrFieldReference[] extensionParams = s.DeliverySettings.ParameterValues;

                                if (extensionParams != null)
                                {
                                    foreach (ParameterValueOrFieldReference extensionParam in extensionParams)
                                    {
                                        subscriptionText += string.Format("{0}: {1}\n", ((ParameterValue)extensionParam).Name, ((ParameterValue)extensionParam).Value);
                                        //Console.WriteLine(((ParameterValue)extensionParam).Name + ": " + ((ParameterValue)extensionParam).Value);
                                    }
                                }

                                ScheduleDefinitionOrReference def;

                                _svc.GetExecutionOptions(item.Path, out def);

                                if (def != null)
                                {
                                    subscriptionText += "\nSubscription Definition:\n";
                                    subscriptionText += "Start On: " + ((ScheduleDefinition)def).StartDateTime + "\n";
                                    subscriptionText += "End On: " + ((ScheduleDefinition)def).EndDateSpecified + "\n";
                                    subscriptionText += "Recurrance: " + "\n";

                                    if (((ScheduleDefinition)def).Item is MinuteRecurrence) 
                                    {
                                        subscriptionText += "Repeat Every " + ((MinuteRecurrence)((ScheduleDefinition)def).Item).MinutesInterval.ToString("g") + " Minute(s)." + "\n";
                                    }

                                    if (((ScheduleDefinition)def).Item is DailyRecurrence)
                                    {
                                        subscriptionText += "Repeat Every " + ((DailyRecurrence)((ScheduleDefinition)def).Item).DaysInterval.ToString("g") + " Day(s)." + "\n";
                                    }

                                    if (((ScheduleDefinition)def).Item is WeeklyRecurrence)
                                    {
                                        subscriptionText += "Repeat every " + ((WeeklyRecurrence)((ScheduleDefinition)def).Item).WeeksInterval.ToString("g") + " Week(s) on " + ParseDaysOfWeek(((WeeklyRecurrence)((ScheduleDefinition)def).Item).DaysOfWeek) + "\n";
                                    }
                                    if (((ScheduleDefinition)def).Item is MonthlyRecurrence)
                                    {
                                        subscriptionText += "Run " + ParseMonthsOfYear(((MonthlyRecurrence)((ScheduleDefinition)def).Item).MonthsOfYear) + " on the " + ((MonthlyRecurrence)((ScheduleDefinition)def).Item).Days + " of each month" + "\n";
                                    }
                                    if (((ScheduleDefinition)def).Item is MonthlyDOWRecurrence)
                                    {
                                        subscriptionText += "Run " + ParseMonthsOfYear(((MonthlyDOWRecurrence)((ScheduleDefinition)def).Item).MonthsOfYear) + " on " + ParseDaysOfWeek(((MonthlyDOWRecurrence)((ScheduleDefinition)def).Item).DaysOfWeek) + "\n";
                                    }
                                }

                                if (s.IsDataDriven)
                                {
                                    subscriptionText += "\nDataDriven Subscription\n";
                                    ExtensionSettings es;
                                    DataRetrievalPlan drp;
                                    string desc;
                                    ActiveState active;
                                    string status;
                                    string eventtype;
                                    string matchdata;

                                    _svc.GetDataDrivenSubscriptionProperties(s.SubscriptionID, out es, out drp, out desc, out active, out status, out eventtype, out matchdata, out extensionParams);

                                    subscriptionText += "CommandType: " + drp.DataSet.Query.CommandType + "\n";
                                    subscriptionText += "CommandText: " + drp.DataSet.Query.CommandText + "\n";

                                    if (extensionParams != null)
                                    {
                                        foreach (ParameterValueOrFieldReference extensionParam in extensionParams)
                                        {
                                            subscriptionText += string.Format("{0}: {1}\n", ((ParameterValue)extensionParam).Name, ((ParameterValue)extensionParam).Value);
                                        }
                                    }
                                }

                                nv.Add(item.Name + "-" + s.SubscriptionID + ".Subscription", subscriptionText);
                            }
                        }

                        if (item.Type == ItemTypeEnum.DataSource)
                        {
                            DataSourceDefinition dsd = _svc.GetDataSourceContents(item.Path);
                            string dsText = string.Format("ConnectString: {0}\nCredentialRetrieval: {1}\nExtension: {2}\nImpersonateUser: {3}\nImpersonateUserSpecified: {4}\nPrompt: {5}\nUserName: {6}\nWindowsCredentials: {7}\n"
                                   , dsd.ConnectString
                                   , dsd.CredentialRetrieval.ToString("g")
                                   , dsd.Extension
                                   , dsd.ImpersonateUser
                                   , dsd.ImpersonateUserSpecified
                                   , dsd.Prompt
                                   , dsd.UserName
                                   , dsd.WindowsCredentials);

                            nv.Add(item.Name + ".dsProperties", dsText);
                        }

                        nv.Add(item.Name + "." + item.Type.ToString("g") + "Security", GetItemSecurity(item.Path));
                    }
                    catch
                    {
                        System.Console.WriteLine("Item Failed: " + item.Path);
                    }
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine("ListChildren Failed: " + Path);
            }
            return nv;
        }

        private string ParseMonthsOfYear(MonthsOfYearSelector MOY)
        {
            string Months = "";
            if (MOY.January) Months += "January, ";
            if (MOY.February) Months += "February, ";
            if (MOY.March) Months += "March, ";
            if (MOY.April) Months += "April, ";
            if (MOY.May) Months += "May, ";
            if (MOY.June) Months += "June, ";
            if (MOY.July) Months += "July, ";
            if (MOY.August) Months += "August, ";
            if (MOY.September) Months += "September, ";
            if (MOY.October) Months += "October, ";
            if (MOY.November) Months += "November, ";
            if (MOY.December) Months += "December, ";
            return System.Text.RegularExpressions.Regex.Replace(Months, ", $", "");
        }
        
        private string ParseDaysOfWeek(DaysOfWeekSelector DOW)
        {
            string Days = "";
            if (DOW.Sunday) Days += "Sunday, ";
            if (DOW.Monday) Days += "Monday, ";
            if (DOW.Tuesday) Days += "Tuesday, ";
            if (DOW.Wednesday) Days += "Wednesday, ";
            if (DOW.Thursday) Days += "Thursday, ";
            if (DOW.Friday) Days += "Friday, ";
            if (DOW.Saturday) Days += "Saturday, ";
            return System.Text.RegularExpressions.Regex.Replace(Days, ", $", "");
        }

        private string GetItemSecurity(string Item) 
        {
            string itemSecurity = "";
            bool inheritParent;

            itemSecurity += "Group or User                           Role(s)\n";
            itemSecurity += "-------------                           -------\n";
            foreach (Policy p in _svc.GetPolicies(Item, out inheritParent))
            {
                string roles = "";

                foreach (Role r in p.Roles)
                {
                    roles += r.Name + ", ";
                }
                roles = System.Text.RegularExpressions.Regex.Replace(roles, ", $", "");
                itemSecurity += string.Format("{0}{1}\n", p.GroupUserName.PadRight(40, ' '), roles);
            }

            itemSecurity += "\nInheritedFromParent: " + inheritParent.ToString() + "\n";
            return itemSecurity;
        }

        public List<string> GetFolders(string Path)
        {
            List<string> folders = new List<string>();

            if (string.IsNullOrEmpty(Path))
            {
                Path = "/";
            }

            foreach (CatalogItem item in _svc.ListChildren(Path, false))
            {
                if (item.Type == ItemTypeEnum.Folder)
                {
                    folders.Add(item.Name);
                }
            }

            return folders;
        }
    }
}
