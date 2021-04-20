using System;
using System.Collections.Generic;
using System.Text;
using Csud.Base;

namespace Csud.Models
{
    public class Summary: ModelBase
    {
        public string SubjectKey { get; set; }
        public string Subject { get; set; }
        public string Account { get; set; }
        public string Person { get; set; }
        public string Provider { get; set; }
        public string ContextType { get; set; }
        public string TimeStart { get; set; }
        public string TimeEnd { get; set; }
        public string ObjectName { get; set; }
        public string ObjectType { get; set; }
        public string Task { get; set; }

    }
}
