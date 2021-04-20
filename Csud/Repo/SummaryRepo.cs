using System;
using System.Collections.Generic;
using System.Text;
using Csud.Base;
using Csud.Interfaces;
using Csud.Models;
using Npgsql;

namespace Csud.Repo
{
    public class SummaryRepo : BaseRepoT<Summary>, ISummaryRepo
    {
        public SummaryRepo(string conStr, string tableName) : base(conStr, tableName)
        {
        }



        //public override IEnumerable<Summary> GetList()
        //{
        //    return base.GetList();
        //}

        public IEnumerable<string> Overview()
        {
            throw new NotImplementedException();
        }
    }
}
