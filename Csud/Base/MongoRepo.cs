using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Csud.Interfaces;
using Csud.Models;
using LinqToDB.Reflection;
using MongoDB.Driver;

namespace Csud.Base
{
    public class MongoRepo<T> : IBaseRepo<T> where T : ModelBase
    {
        public string TableName;
        public string ConnectionString;
        public MongoRepo(string connectionString, string tableName)
        {
            TableName = tableName;
            ConnectionString = connectionString;
        }

        public T Add(T item)
        {
            throw new NotImplementedException();
        }

        public void ExecuteNonQuery(string procName, params KeyValuePair<string, object>[] parameters)
        {
            throw new NotImplementedException();
        }

        public T1 ExecuteScalar<T1>(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            throw new NotImplementedException();
        }

        public DataTable ExecuteTable(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            throw new NotImplementedException();
        }

        public IList<TV> GetDistinctValues<TV>(string fieldName, CamlFilter filter)
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> GetList()
        {
            throw new NotImplementedException();
        }

        public IList<T> GetList(CamlFilter filter)
        {
            throw new NotImplementedException();
        }

        public IBaseRepo<T> InnerJoin<T1>(string alias, string leftKey, string rightKey)
        {
            throw new NotImplementedException();
        }

        public IBaseRepo<T> LeftJoin<T1>(string alias, string leftKey, string rightKey)
        {
            throw new NotImplementedException();
        }

        public T Update(T item)
        {
            throw new NotImplementedException();
        }
    }
}
