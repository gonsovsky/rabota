using Csud.Interfaces;
using Csud.Models;
using Csud.Repo;

using Npgsql;

namespace Csud
{
    public class Csud
    {
        public Csud(string conStr)
        {
            if (conStr.Contains("mongo"))
            {
                Persons = new Repo.Mongo.PersonRepo(conStr, "person");
                Summary = new Repo.Mongo.SummaryRepo(conStr, "summary_by_account");
            }
            else
            {
                Persons = new Repo.Postgre.PersonRepo(conStr, "person");
                Summary = new Repo.Postgre.SummaryRepo(conStr, "summary_by_account");
            }

        }

        public IPersonRepo Persons;

        public ISummaryRepo Summary;
    }
}
