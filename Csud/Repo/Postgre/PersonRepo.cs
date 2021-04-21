using System.Collections.Generic;
using Csud.Base;
using Csud.Interfaces;
using Csud.Models;

namespace Csud.Repo.Postgre
{
    public class PersonRepo: PostgreRepo<Person>, IPersonRepo
    {
        public PersonRepo(string conStr, string tableName) : base(conStr, tableName)
        {
        }

        public override IList<Person> GetList()
        {
            return base.GetList();  
        }

        //public override 
        //{
        //    string sql = "SELECT first_name FROM person";
        //    using var cmd = new NpgsqlCommand(sql);

        //    using NpgsqlDataReader rdr = cmd.ExecuteReader();

        //    while (rdr.Read())
        //    {
        //        var x = new Person() {FirstName = rdr.GetString(0)};
        //        yield return x;
        //    }
        //    rdr.Close();
        //}
    }
}
