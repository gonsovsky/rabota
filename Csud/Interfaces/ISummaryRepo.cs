using System;
using System.Collections.Generic;
using System.Text;
using Csud.Base;
using Csud.Models;

namespace Csud.Interfaces
{
    public interface ISummaryRepo : IBaseRepo<Summary>
    {
        public IEnumerable<string> Overview();
    }
}
