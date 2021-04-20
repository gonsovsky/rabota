using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using LinqToDB;
using Npgsql;

namespace Csud.Base
{
    public partial class BaseRepoT<T> :  IBaseRepoT<T> where T : ModelBase
    {
        protected string ConnectionString { get; }

        #region Object mapping

        private static TR Read<TR>(IDataRecord record, params object[] constructorArgs)
        {
            var fieldValues = new object[record.FieldCount];
            record.GetValues(fieldValues);

            var obj = (TR)Activator.CreateInstance(typeof(TR),
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, default(Binder), constructorArgs,
                null);

            for (var i = 0; i < fieldValues.Length; ++i)
            {
                var fieldName = record.GetName(i);
                var propertyInfo = typeof(TR).GetProperty(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (propertyInfo != null)
                    propertyInfo.SetValue(obj, fieldValues[i], null);
            }
            return obj;
        }

        #endregion

        #region Data context interface

        public TResult ExecuteLinqContext<TContext, TResult>(Func<TContext, TResult> action)
            where TContext : DataContext
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();

                var dataContext = (TContext)Activator.CreateInstance(typeof(TContext), (NpgsqlConnection)cn);

                return action(dataContext);
            }
        }

        public void ExecuteLinqContext<TContext>(Action<TContext> action) where TContext : DataContext
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();

                var dataContext = (TContext)Activator.CreateInstance(typeof(TContext), (NpgsqlConnection)cn);

                action(dataContext);
            }
        }

        #endregion

        #region Database direct interface

        private T ExecuteCommand<T>(string commandText, IEnumerable<KeyValuePair<string, object>> parameters,
            Func<NpgsqlCommand, T> action, int timeout = 30)
        {
            using (var cn = new NpgsqlConnection(ConnectionString))
            {
                cn.Open();

                var cmd = new NpgsqlCommand()
                {
                    CommandText = commandText,
                    CommandType = commandText.Contains(" ") ? CommandType.Text : CommandType.StoredProcedure,
                    Connection = cn,
                    CommandTimeout = timeout
                };

                foreach (var p in parameters)
                {
                    cmd.Parameters.AddWithValue(p.Key, p.Value ?? DBNull.Value);
                }

                return action(cmd);
            }
        }

        public virtual T ExecuteScalar<T>(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            return (T)ExecuteCommand(commandText, parameters, cmd => cmd.ExecuteScalar());
        }

        public virtual void ExecuteNonQuery(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            ExecuteCommand(commandText, parameters, cmd => cmd.ExecuteNonQuery());
        }

        public virtual void ExecuteNonQuery(string commandText, int timeout,
            params KeyValuePair<string, object>[] parameters)
        {
            ExecuteCommand(commandText, parameters, cmd => cmd.ExecuteNonQuery(), timeout);
        }

        public virtual T ExecuteSingleResult<T>(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Read<T>(reader) : default(T);
                }
            });
        }

        public virtual T ExecuteSingleResult<T>(object[] constructorArgs, string commandText,
            params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read() ? Read<T>(reader, constructorArgs) : default(T);
                }
            });
        }

        public virtual IDictionary<string, object> ExecuteSingleResult(string commandText,
            params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                using (var reader = cmd.ExecuteReader())
                {
                    return reader.Read()
                        ? Enumerable.Range(0, reader.FieldCount).ToDictionary(reader.GetName, reader.GetValue)
                        : null;
                }
            });
        }

        public virtual IList<T> ExecuteReader<T>(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                var rows = new List<T>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(Read<T>(reader));
                    }
                }

                return rows;
            });
        }

        public virtual IList<T> ExecuteReader<T>(object[] constructorArgs, string commandText,
            params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                var rows = new List<T>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        rows.Add(Read<T>(reader, constructorArgs));
                    }
                }

                return rows;
            });
        }

        public virtual DataTable ExecuteTable(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                var rows = new DataTable();

                var dataAdapter = new NpgsqlDataAdapter(cmd);
                dataAdapter.Fill(rows);

                return rows;
            });
        }

        public virtual DataTable ExecuteTableAdHoc(string commandText, params KeyValuePair<string, object>[] parameters)
        {
            return ExecuteCommand(commandText, parameters, cmd =>
            {
                var rows = new DataTable();

                var dataAdapter = new NpgsqlDataAdapter(cmd);
                dataAdapter.Fill(rows);

                return rows;
            });
        }

        #endregion
    }
}
