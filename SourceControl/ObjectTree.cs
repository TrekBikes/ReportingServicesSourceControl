using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;

namespace ReportingServicesSourceControl.SourceControl
{
    class ObjectTree
    {
        public string Name;
        public List<ObjectTree> ObjectTrees = new List<ObjectTree>();
        public NameValueCollection Objects = new NameValueCollection();
    }
}
