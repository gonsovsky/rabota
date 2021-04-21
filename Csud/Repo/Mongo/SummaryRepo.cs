using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using Csud.Base;
using Csud.Interfaces;
using Csud.Models;
using LinqToDB.Reflection;
using MongoDB.Driver;

namespace Csud.Repo.Mongo
{
    public class SummaryRepo : MongoRepo<Summary>, ISummaryRepo
    {
        public SummaryRepo(string conStr, string tableName) : base(conStr, tableName)
        {
        }

        public IEnumerable<string> Overview()
        {
            MongoClient client = new MongoClient(ConnectionString);
            IMongoDatabase db = client.GetDatabase("csud");

            var account = db.GetCollection<dynamic>("account").Find((p) => true).ToList();
            var account_provider = db.GetCollection<dynamic>("account_provider").Find((p) => true).ToList();
            var context = db.GetCollection<dynamic>("context").Find((p) => true).ToList();
            var obj = db.GetCollection<dynamic>("object").Find((p) => true).ToList();
            var person = db.GetCollection<dynamic>("person").Find((p) => true).ToList();
            var subject = db.GetCollection<dynamic>("subject").Find((p) => true).ToList();
            var task = db.GetCollection<dynamic>("task").Find((p) => true).ToList();
            var time_context = db.GetCollection<dynamic>("time_context").Find((p) => true).ToList();

            var q = from s in subject
                join a in account on s.key equals a.subject_key
                join ap in account_provider on  s.key equals ap.key
                join p in person on a.person_key equals p.key
                join co in context on s.context_key equals co.key
                join tc in time_context on co.key equals tc.context_key
                join o in obj on co.key equals o.context_key
                join t in task on o.key equals t.object_key
                    select new Summary()
                {
                    SubjectKey = s.key.ToString(),
                    Subject = s.name.ToString(),
                    Account = a.name,
                    Provider = ap.type,
                    Person = p.first_name,
                    ContextType = co.type,
                    TimeStart = tc.time_start,
                    TimeEnd = tc.time_end,
                    ObjectName = o.name,
                    ObjectType = o.type,
                    Task = t.name
                    };

            var list = q.ToList();
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
