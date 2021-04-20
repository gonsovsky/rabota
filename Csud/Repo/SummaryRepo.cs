using System;
using System.Collections.Generic;
using System.Text;
using Csud.Base;
using Csud.Interfaces;
using Csud.Models;
using Npgsql;

namespace Csud.Repo
{
    public class SummaryRepo : BaseRepo<Summary>, ISummaryRepo
    {
        public SummaryRepo(string conStr, string tableName) : base(conStr, tableName)
        {
        }

        public IEnumerable<string> Overview()
        {
            var list = GetList();
            foreach (var item in list)
            {
                
                var text = 
$@"Субъект {item.Subject} (№{item.SubjectKey}) с уч. записью
{item.Account} выданной поставщиком {item.Provider} принадлежащей сотруднику {item.Person}
в рабочее время (контекст {item.ContextType}) опредленное c {item.TimeStart} по {item.TimeEnd}
согласно должностным обязанностям (объект {item.ObjectType} {item.ObjectName})
должен выполнять работу: {item.Task}
            ";            
                yield return text;
            }
        }
    }
}
