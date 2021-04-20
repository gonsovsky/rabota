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
            Persons = new PersonRepo(conStr, "person");
            Summary = new SummaryRepo(conStr, "summary_by_account");
        }

        public IPersonRepo Persons;

        public ISummaryRepo Summary;
    }
}
