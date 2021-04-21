using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using Csud.Interfaces;
using Csud.Models;
using LinqToDB;
using LinqToDB.Mapping;
using MongoDB.Driver.Core.Configuration;
using Npgsql;

namespace Csud.Base
{
  
    public class PostgreRepo<T> : IBaseRepo<T> where T : ModelBase
    {
        //TODO: refactor
        private const string EntityAlias = "req";

        private enum JoinType
        {
            Inner,
            Left,
        }

        private class JoinDefinition
        {
            public JoinType JoinType { get; set; }
            public Type RightEntityType { get; set; }
            public string RightEntityAlias { get; set; }
            public string LeftKey { get; set; }
            public string RightKey { get; set; }
        }

        private readonly List<JoinDefinition> _joins = new List<JoinDefinition>();

        public string TableName;

        public PostgreRepo(string connectionString, string tableName)
        {
            TableName = tableName;
            ConnectionString = connectionString;
        }

        #region Query

        public virtual IList<T> GetList()
        {
            var queryParameters = new List<KeyValuePair<string, object>>();

            string queryText =
                string.Format("SELECT {1} {0}FROM {2} {0}",
                    Environment.NewLine,
                    EntityAlias + ".*",
                    BuildFromPart());

            var table = ExecuteTable(queryText, queryParameters.ToArray());

            return
                (from DataRow row in table.Rows select Read(row)).ToList();
        }

        /// <summary>
        /// Возвращает список записей
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<T> GetList(CamlFilter filter)
        {
            var queryParameters = new List<KeyValuePair<string, object>>();

            string queryText =
                string.Format("SELECT {1} {0}FROM {2} {0}WHERE {3} {4} {5}",
                    Environment.NewLine,
                    EntityAlias + ".*",
                    BuildFromPart(),
                    BuildWherePart(filter, queryParameters),
                    BuildOrderByPart(filter),
                    BuildTakeSkipPart(filter));

            var table = ExecuteTable(queryText, queryParameters.ToArray());

            return
                (from DataRow row in table.Rows select Read(row)).ToList();
        }

        /// <summary>
        /// Преобраует строку данных в объект
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        private static T Read(DataRow row)
        {
            var entity = (T)Activator.CreateInstance(typeof(T), true);

            var properties =
                from DataColumn column in row.Table.Columns
                where row[column] != DBNull.Value
                select new KeyValuePair<string, object>(column.ColumnName, row[column]);

            foreach (var property in properties)
            {
                entity[property.Key] = property.Value;
            }

            return entity;
        }

        /// <summary>
        /// Возвращает набор уникальных значений поля
        /// </summary>
        /// <typeparam name="TV"></typeparam>
        /// <param name="fieldName"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public IList<TV> GetDistinctValues<TV>(string fieldName, CamlFilter filter)
        {
            var queryParameters = new List<KeyValuePair<string, object>>();

            string queryText =
                string.Format("SELECT DISTINCT {1} {0}FROM {2} {0}WHERE {3} {0}ORDER BY {1}",
                    Environment.NewLine,
                    fieldName,
                    BuildFromPart(),
                    BuildWherePart(filter, queryParameters));

            var table = ExecuteTable(queryText, queryParameters.ToArray());

            return
                (from DataRow row in table.Rows select (TV)row[0]).ToList();
        }

        #endregion

        #region Update

        /// <summary>
        /// Возвращает фильтр по первичному ключу элемента
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static CamlFilter GetPrimaryKeyInsertFilter(T item)
        {
            var pkProperties = typeof(T).GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Any());

            return pkProperties.Aggregate(CamlFilter.Empty,
                (current, property) => current.Merge(CamlFilterOperation.And,
                    property.GetCustomAttributes(typeof(IdentityAttribute), true).Any()
                        ? CamlFilter.Eq(property.Name, ValueCamlNode.System("SCOPE_IDENTITY()"))
                        : CamlFilter.Eq(property.Name,
                            ValueCamlNode.FromObject(item.GetPropertyValue<object>(property.Name)))));
        }


        /// <summary>
        /// Возвращает фильтр по первичному ключу элемента
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static CamlFilter GetPrimaryKeyUpdateFilter(T item)
        {
            var pkProperties = typeof(T).GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Any())
                .Select(x => x.Name);

            return pkProperties.Aggregate(CamlFilter.Empty, (current, propertyName) =>
                current.Merge(CamlFilterOperation.And,
                    CamlFilter.Eq(propertyName,
                        ValueCamlNode.FromObject(item.GetPropertyValue<object>(propertyName)))));
        }

        /// <summary>
        /// Возвращает фильтр по полю timestamp
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        private static CamlFilter GetTimestampFilter(T item)
        {
            var tsProperties = typeof(T).GetProperties()
                .Where(x => x.GetCustomAttributes(typeof(TimestampAttribute), true).Any())
                .Select(x => x.Name);

            return tsProperties.Aggregate(CamlFilter.Empty, (current, propertyName) =>
                current.Merge(CamlFilterOperation.And,
                    CamlFilter.Eq(propertyName,
                        ValueCamlNode.FromObject(item.GetPropertyValue<object>(propertyName)))));
        }

        /// <summary>
        /// Обновляет элемент
        /// </summary>
        /// <param name="item"></param>
        public T Update(T item)
        {
            var changedProperties = item.GetModifiedPropertyValues().ToList();
            if (!changedProperties.Any())
                return item;

            var queryParameters = new List<KeyValuePair<string, object>>();

            // SET

            var setClauses = new List<string>();

            int pn = 0;
            foreach (var pair in changedProperties)
            {
                setClauses.Add(string.Format("[{0}] = @{1}", pair.Key, "p" + pn));
                queryParameters.Add(new KeyValuePair<string, object>("p" + pn, pair.Value.ToSqlValue()));
                pn++;
            }

            // WHERE

            var updateFilter = GetPrimaryKeyUpdateFilter(item);
            var timestampFilter = GetTimestampFilter(item);

            string whereClause1 = BuildWherePart(
                updateFilter.Merge(CamlFilterOperation.And, timestampFilter), queryParameters);
            string whereClause2 = BuildWherePart(updateFilter, queryParameters);

            string queryText =
                string.Format(
                    "UPDATE {1} {0}SET {2} {0}WHERE {3};{0}SELECT * {0}FROM {1} {0}WHERE @@ROWCOUNT > 0 AND {4}",
                    Environment.NewLine,
                    TableName,
                    setClauses.Csv(),
                    whereClause1,
                    whereClause2);

            // Выполняем запрос
            var table = ExecuteTable(queryText, queryParameters.ToArray());

            // Запись в БД изменена другим потоком выполнения?
            if (table.Rows.Count == 0)
                throw new ApplicationException();

            // Возвращаем обновленные данные
            return Read(table.Rows[0]);
        }

        /// <summary>
        /// Добавляет элемент
        /// </summary>
        /// <param name="item"></param>
        public T Add(T item)
        {
            var queryParameters = new List<KeyValuePair<string, object>>();

            // VALUES

            var fieldClauses = new List<string>();
            var valueClauses = new List<string>();

            int pn = 0;
            foreach (var pair in item.GetCurrentPropertyValues())
            {
                var propertyInfo = typeof(T).GetProperty(pair.Key);
                if (propertyInfo != null && propertyInfo.GetCustomAttributes(typeof(TimestampAttribute), true).Any())
                    continue;

                fieldClauses.Add(string.Format("[{0}]", pair.Key));
                valueClauses.Add(string.Format("@{0}", "p" + pn));
                queryParameters.Add(new KeyValuePair<string, object>("p" + pn, pair.Value.ToSqlValue()));
                pn++;
            }

            // WHERE

            var getFilter = GetPrimaryKeyInsertFilter(item);
            string whereClause = BuildWherePart(getFilter, queryParameters);

            string queryText =
                string.Format("INSERT {1} ({2}){0}  VALUES ({3});{0}SELECT * {0}FROM {1} {0}WHERE {4}",
                    Environment.NewLine,
                    TableName,
                    fieldClauses.Csv(),
                    valueClauses.Csv(),
                    whereClause);

            // Выполняем запрос
            var table = ExecuteTable(queryText, queryParameters.ToArray());

            // Возвращаем обновленные данные
            return Read(table.Rows[0]);
        }


        #endregion

        #region Joins

        /// <summary>
        /// Добавляет к запросу внешнее (LEFT) соединение с другой таблицей
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="rightAlias"></param>
        /// <param name="leftKey"></param>
        /// <param name="rightKey"></param>
        /// <returns></returns>
        public IBaseRepo<T> LeftJoin<T1>(string rightAlias, string leftKey, string rightKey)
        {
            var repository = new PostgreRepo<T>(ConnectionString, TableName);

            repository._joins.AddRange(_joins);
            repository._joins.Add(new JoinDefinition
            {
                JoinType = JoinType.Left,
                RightEntityType = typeof(T1),
                RightEntityAlias = rightAlias,
                LeftKey = leftKey,
                RightKey = rightKey
            });

            return repository;
        }

        /// <summary>
        /// Добавляет к запросу внутреннее (INNER) соединение с другой таблицей
        /// </summary>
        /// <typeparam name="T1"></typeparam>
        /// <param name="alias"></param>
        /// <param name="leftKey"></param>
        /// <param name="rightKey"></param>
        /// <returns></returns>
        public IBaseRepo<T> InnerJoin<T1>(string alias, string leftKey, string rightKey)
        {
            var repository = new PostgreRepo<T>(ConnectionString, TableName);

            repository._joins.AddRange(_joins);
            _joins.Add(new JoinDefinition
            {
                JoinType = JoinType.Inner,
                RightEntityType = typeof(T1),
                RightEntityAlias = alias,
                LeftKey = leftKey,
                RightKey = rightKey
            });

            return repository;
        }

        #endregion

        #region SQL generation

        private string GetTableName(Type entityType)
        {
            return TableName;
        }

        private string BuildFromPart()
        {
            var sb = new StringBuilder();

            sb.AppendFormat("{0} {1}", GetTableName(typeof(T)), EntityAlias);
            foreach (var join in _joins)
            {
                sb.AppendLine();
                sb.AppendFormat("{5} JOIN {0} {1} ON {1}.{2} = {3}.{4}",
                    GetTableName(join.RightEntityType),
                    join.RightEntityAlias,
                    join.RightKey,
                    EntityAlias,
                    join.LeftKey,
                    join.JoinType.ToString().ToUpper()
                );
            }

            return sb.ToString();
        }

        private string BuildWherePart(CamlFilter filter, IList<KeyValuePair<string, object>> parameters)
        {
            CamlNode wherePart = filter.Where;

            var parametersVisitor = new InjectParametersCamlNodeVisitor(parameters);
            wherePart = parametersVisitor.ReplaceValuesWithParameters(wherePart);

            return ToSqlFilter(wherePart);
        }

        private string BuildOrderByPart(CamlFilter filter)
        {
            var sb = new StringBuilder();

            // Order by необходим для take/skip,
            // если в DynamicRepository появится поддержка пользовательного Order by,
            // то здесь нужно будет добавить проверку на его наличие
            if (filter.Take != 0 || filter.Skip != 0)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"ORDER BY {EntityAlias}.Id DESC");
                return sb.ToString();
            }
            else
                return string.Empty;
        }

        private string BuildTakeSkipPart(CamlFilter filter)
        {
            var sb = new StringBuilder();

            // Skip 0 строк не возможен при отсутствии Take
            // Skip 0> строк возможен при отсутствии Take
            if (filter.Take != 0 || filter.Skip != 0)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"OFFSET {filter.Skip} ROWS");
            }

            // Take 0 строк не возможен
            if (filter.Take != 0)
            {
                sb.Append(Environment.NewLine);
                sb.Append($"FETCH NEXT {filter.Take} ROWS ONLY");
            }

            return sb.ToString();
        }

        private string ToSqlFilter(CamlNode node)
        {
            if (node == null)
                return string.Empty;

            var binaryNode = node as BinaryCamlNode;
            if (binaryNode != null)
            {
                return ToSqlFilter(binaryNode);
            }

            var unaryNode = node as UnaryCamlNode;
            if (unaryNode != null)
            {
                return ToSqlFilter(unaryNode);
            }

            var fieldNode = node as FieldRefCamlNode;
            if (fieldNode != null)
            {
                return ToSqlFilter(fieldNode);
            }

            var valueNode = node as ValueCamlNode;
            if (valueNode != null)
            {
                return ToSqlFilter(valueNode);
            }

            var multipleValueNode = node as MultipleValueCamlNode;
            if (multipleValueNode != null)
            {
                return ToSqlFilter(multipleValueNode);
            }

            throw new NotImplementedException(node.GetType().ToString());
        }

        private string ToSqlFilter(BinaryCamlNode binaryNode)
        {
            var sLeft = ToSqlFilter(binaryNode.Left);
            var sRight = ToSqlFilter(binaryNode.Right);

            switch (binaryNode.NodeType)
            {
                case CamlNodeType.And:
                    return string.Format("({0} AND {1})", sLeft, sRight);
                case CamlNodeType.Or:
                    return string.Format("({0} OR {1})", sLeft, sRight);

                case CamlNodeType.BeginsWith:
                    return string.Format("({0} LIKE N'{1}%')", sLeft, sRight);
                case CamlNodeType.Contains:
                    return string.Format("({0} LIKE N'%'+{1}+N'%')", sLeft, sRight);
                case CamlNodeType.NContains:
                    return string.Format("({0} NOT LIKE N'%'+{1}+N'%')", sLeft, sRight);
                case CamlNodeType.In:
                    return string.Format("({0} IN ({1}))", sLeft, sRight);

                case CamlNodeType.Eq:
                    return string.Format("({0} = {1})", sLeft, sRight);
                case CamlNodeType.Neq:
                    return string.Format("({0} != {1})", sLeft, sRight);
                case CamlNodeType.Gt:
                    return string.Format("({0} > {1})", sLeft, sRight);
                case CamlNodeType.Geq:
                    return string.Format("({0} >= {1})", sLeft, sRight);
                case CamlNodeType.Lt:
                    return string.Format("({0} < {1})", sLeft, sRight);
                case CamlNodeType.Leq:
                    return string.Format("({0} < {1})", sLeft, sRight);
                default:
                    throw new NotImplementedException(binaryNode.GetType().ToString());
            }
        }

        private string ToSqlFilter(UnaryCamlNode unaryNode)
        {
            var sNode = ToSqlFilter(unaryNode.Node);

            switch (unaryNode.NodeType)
            {
                case CamlNodeType.Where:
                    return sNode;
                case CamlNodeType.Not:
                    return string.Format(" NOT ({0}) ", sNode);
                case CamlNodeType.IsNull:
                    return string.Format(" ({0} IS NULL) ", sNode);
                case CamlNodeType.IsNotNull:
                    return string.Format(" ({0} IS NOT NULL) ", sNode);
                default:
                    throw new NotImplementedException(unaryNode.GetType().ToString());
            }
        }

        private static string ToSqlFilter(FieldRefCamlNode fieldNode)
        {
            return string.Format("[{0}]", fieldNode.Name);
        }

        private static string ToSqlFilter(ValueCamlNode valueNode)
        {
            switch (valueNode.ValueType)
            {
                case ValueCamlNode.Type.System:
                    return valueNode.Value;
                case ValueCamlNode.Type.Parameter:
                    return string.Format("@{0}", valueNode.Value);
                case ValueCamlNode.Type.DateTime:
                    return string.Format("'{0}'", valueNode.DateValue().ToSqlString());
                case ValueCamlNode.Type.Integer:
                    return string.Format("{0}", valueNode.IntValue());
                case ValueCamlNode.Type.Text:
                    return string.Format("N'{0}'", (valueNode.Value ?? string.Empty).Replace("'", "''"));
                case ValueCamlNode.Type.Binary:
                    return string.Format("{0}", valueNode.Value ?? string.Empty);
                default:
                    throw new NotImplementedException(valueNode.ValueType.ToString());
            }
        }

        private static string ToSqlFilter(MultipleValueCamlNode valueNode)
        {
            switch (valueNode.ValueType)
            {
                case MultipleValueCamlNode.Type.Parameter:
                    return valueNode.Values
                        .Select(x => string.Format("@{0}", x))
                        .Csv();
                case MultipleValueCamlNode.Type.Integer:
                    return valueNode.Values
                        .Select(x => string.Format("{0}", x))
                        .Csv();
                case MultipleValueCamlNode.Type.Text:
                    return valueNode.Values
                        .Select(x => string.Format("N'{0}'", (x ?? string.Empty).Replace("'", "''")))
                        .Csv();
                default:
                    throw new NotImplementedException(valueNode.ValueType.ToString());
            }
        }

        #endregion

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