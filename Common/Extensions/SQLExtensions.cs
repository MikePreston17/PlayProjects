﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
namespace Common.Extensions
{
    public static class SQLExtensions
    {
        public static bool IsConnectionString(this string connectionString)
        {
            var csb = new SqlConnectionStringBuilder(connectionString);
            if (csb.DataSource == null || csb.InitialCatalog == null)
                throw new Exception($"Connection string: '{connectionString} invalid");
            return true;
        }
        public static bool CanOpen(this string connectionString)
        {
            return new SqlConnection(connectionString).CanOpen();
        }
        public static bool CanOpen(this SqlConnection connection)
        {
            try
            {
                if (connection == null) { return false; }
                connection.Open();
                var canOpen = connection.State == ConnectionState.Open;
                connection.Close();
                return canOpen;
            }
            catch
            {
                return false;
            }
        }
        public static List<SqlParameter> GetSqlParams(this string connectionString, string tableName)
        {
            if (tableName.IsNullOrWhiteSpace()) throw new Exception("No table name provided!");
            if (!connectionString.IsConnectionString()) return null;
            List<SqlDbType> sql_db_types = new List<SqlDbType>();
            DataTable dt = new DataTable();
            //
            /// Get the schema into a datatable
            ////
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandText = $"SET FMTONLY ON; select * from dbo.{tableName}; SET FMTONLY OFF";
                    SqlDataReader reader = cmd.ExecuteReader();
                    dt = reader.GetSchemaTable();
                }
            }
            //
            /// Get the name of column, type of column and size and assign to tuples
            ////
            List<Tuple<string, SqlDbType, int>> sql_pre_param_list = new List<Tuple<string, SqlDbType, int>>();
            foreach (var row in dt.Rows.Cast<DataRow>())
            {
                SqlDbType type = (SqlDbType)(int.Parse(row["ProviderType"].ToString()));
                int size = int.Parse(row["ColumnSize"].ToString());
                string name = row["ColumnName"].ToString();
                //Debug.WriteLine($"type: {type.ToString()}; size: {size}");
                Tuple<string, SqlDbType, int> tuple = new Tuple<string, SqlDbType, int>(name, type, size);
                sql_pre_param_list.Add(tuple);
            }
            //
            /// Create full parameter list!
            ////
            List<SqlParameter> sql_params_list = new List<SqlParameter>();
            foreach (var item in sql_pre_param_list)
            {
                SqlParameter sp = new SqlParameter
                {
                    ParameterName = item.Item1,
                    SqlDbType = item.Item2,
                    Size = item.Item3
                };
                sql_params_list.Add(sp);
            }
            //sql_params_list.ForEach(x => { Debug.WriteLine($"{x.ParameterName}\t{x.SqlDbType.ToString()}\t{x.Size}"); });
            return sql_params_list;
        }
        public static string GetInsertQuery(this IEnumerable<SqlParameter> sqlparameters, string tableName)
        {
            if (tableName.IsNullOrWhiteSpace()) return null;
            StringBuilder query = new StringBuilder($"INSERT INTO dbo.{tableName}\n");
            query.Append("(");
            foreach (var parameter in sqlparameters)
                query.Append($"{parameter.ParameterName}, ");
            query.Length -= 2; //removes extra comma
            query.Append(")\nVALUES\n(");
            foreach (var parameter in sqlparameters)
                query.Append($"@{parameter.ParameterName}, ");
            query.Length -= 2; //removes extra comma
            query.Append(")");

            Debug.WriteLine($"Generated query: {query.ToString()}");

            return query.ToString();
        }
        /// <summary>
        /// Find Duplicate Rows from a given table
        /// Can Exclude columns by name
        /// TODO: pull this out into it's own abstraction or part of one!
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="tablename"></param>
        /// <param name="excludedColumns"></param>
        /// <param name="max_appearances_allowed"></param>
        /// <returns></returns>
        public static DataTable FindDuplicateRows(string connectionString, string tablename, IEnumerable<string> excludedColumns = null, int max_appearances_allowed = 1)
        {
            SqlConnectionStringBuilder csb = new SqlConnectionStringBuilder(connectionString);
            string database = csb.InitialCatalog;
            string connStr = csb.ConnectionString;
            List<string> nonPKeyColumnNames = new List<string>();
            string sql_get_schema = $"SET FMTONLY ON; select * from [{database}].dbo.[{tablename}]; SET FMTONLY OFF";
            //
            /// GET SCHEMA and NON-UNIQUEIDENTIFIER COLUMN NAMES
            ////
            DataTable dtSchema = new DataTable($"{tablename} Schema");
            using (SqlConnection connection = new SqlConnection(connStr))
            {
                using (SqlCommand command = new SqlCommand(sql_get_schema, connection))
                {
                    try
                    {
                        command.Connection.Open();
                        SqlDataReader reader = command.ExecuteReader();
                        dtSchema = reader.GetSchemaTable();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().Name, ex.Message));
                    }
                }
            }
            foreach (var row in dtSchema.Rows.Cast<DataRow>())
            {
                SqlDbType type = (SqlDbType)(int.Parse(row["ProviderType"].ToString()));
                if (type != SqlDbType.UniqueIdentifier)
                    nonPKeyColumnNames.Add(row["ColumnName"].ToString());
            }
            if (excludedColumns != null) nonPKeyColumnNames.RemoveAll(x => excludedColumns.Contains(x));
            //
            /// RETRIEVE DUPLICATES
            //// 
            DataTable dt = new DataTable($"{tablename} Duplicates");
            StringBuilder sql = new StringBuilder();
            try
            {
                List<string> correctedColumNames = new List<string>();
                foreach (var name in nonPKeyColumnNames)
                {
                    if (!name.EndsWith(","))
                        correctedColumNames.Add(name + ",");
                    else correctedColumNames.Add(name);
                }
                correctedColumNames[correctedColumNames.Count - 1] = correctedColumNames.Last().Replace(",", " ").Trim();
                sql.Append("SELECT ");
                //Add Columns Names:
                foreach (string columnName in correctedColumNames)
                    sql.Append(string.Format("{0} ", columnName));
                sql.Append(", count (*) as APPEARANCES ");
                sql.AppendLine(string.Format(" \nFROM {0}.dbo.{1}", database, tablename));
                sql.AppendLine(" Group By ");
                //Add Columns Names again:
                foreach (string columnName in correctedColumNames)
                    sql.Append(string.Format("{0} ", columnName));
                sql.AppendLine(" having count (*) > " + max_appearances_allowed);
            }
            catch (Exception ex)
            {
                throw new Exception(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().Name, ex.Message));
            }
            using (SqlDataAdapter da = new SqlDataAdapter(sql.ToString(), connStr))
            {
                try
                {
                    da.SelectCommand.CommandTimeout = 180;
                    da.Fill(dt);
                    if (dt.Rows.Count > max_appearances_allowed)
                    {
                        Debug.WriteLine(string.Format("--Number of Duplicate rows (including original) in table {0}.dbo.{1}: {2}", database, tablename, dt.Rows.Count));
                        Debug.WriteLine(sql.ToString());
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception(string.Format("{0}: {1}", MethodBase.GetCurrentMethod().Name, ex.Message));
                }
            }
            return dt;
        }
    }
}
