using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace ReportingServicesSourceControl.ReportingServices
{
    interface IReportService
    {
        NameValueCollection GetItems(string Path);
        List<string> GetFolders(string Path);
    }
}
