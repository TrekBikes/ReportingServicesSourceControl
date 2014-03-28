using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace ReportingServicesSourceControl.SourceControl
{
    interface ISourceControl
    {
        bool Add(string Path);
        bool Update(string Path);
        bool Delete(string Path);
        bool Commit(string Path);
        bool LogObjects(string BasePath, string ObjectTypeName, NameValueCollection Objects);
        bool LogObjectTree(string BasePath, ObjectTree Tree);
    }
}
