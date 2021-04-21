using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Csud.Base;
using Csud.Models;

namespace Csud.Interfaces
{
    public interface IBaseRepo<T> where T : ModelBase
    {
        T Add(T item);
        T Update(T item);
        IList<T> GetList();
        IList<T> GetList(CamlFilter filter);
        IList<TV> GetDistinctValues<TV>(string fieldName, CamlFilter filter);
        IBaseRepo<T> InnerJoin<T1>(string alias, string leftKey, string rightKey);
        IBaseRepo<T> LeftJoin<T1>(string alias, string leftKey, string rightKey);

        void ExecuteNonQuery(string procName, params KeyValuePair<string, object>[] parameters);
        T1 ExecuteScalar<T1>(string commandText, params KeyValuePair<string, object>[] parameters);
        DataTable ExecuteTable(string commandText, params KeyValuePair<string, object>[] parameters);
    }
}
