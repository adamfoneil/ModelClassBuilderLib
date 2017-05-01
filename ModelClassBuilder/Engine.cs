using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;

namespace AdamOneilSoftware.ModelClassBuilder
{
    public class Engine
    {
        private StringBuilder _stringBuilder;

        public Engine()
        {
        }

        public string ConnectionString { get; set; }
        public string CodeNamespace { get; set; }
        public string ClassName { get; set; }

        public StringBuilder BuildCSharpClass(string schema, string tableName)
        {            
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                var pkColumns = GetPrimaryKeyColumns(cn, schema, tableName);
                var fkColumns = GetForeignKeyColumns(cn, schema, tableName);
                return BuildCSharpClass(cn, $"SELECT * FROM [{schema}].[{tableName}]", pkColumns, fkColumns);
            }            
        }
        public StringBuilder BuildCSharpClass(string query)
        {            
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                return BuildCSharpClass(cn, query, null, null);
            }            
        }
        public void CopyToClipboard()
        {
            
        }

        public void SaveAs(string fileName)
        {
            using (StreamWriter output = File.CreateText(fileName))
            {                
                output.Write(_stringBuilder.ToString());
            }
        }

        private StringBuilder BuildCSharpClass(SqlConnection connection, string query, IEnumerable<string> pkColumns = null, Dictionary<string, ColumnRef> fkColumns = null)
        {
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
                {
                    var tbl = reader.GetSchemaTable();
                    IEnumerable<ColumnInfo> columns = GetColumnInfo(tbl);
                    _stringBuilder = new StringBuilder();

                    _stringBuilder.AppendLine("using System;");
                    if (columns.Any(col => col.Size.HasValue))
                    {
                        _stringBuilder.AppendLine("using System.ComponentModel.DataAnnotations;");
                    }

                    _stringBuilder.AppendLine();
                    _stringBuilder.AppendLine($"namespace {CodeNamespace}\r\n{{");
                    _stringBuilder.AppendLine($"\tpublic class {ClassName}\r\n\t{{");
                    foreach (var col in columns)
                    {
                        if (pkColumns?.Contains(col.Name) ?? false)
                        {
                            _stringBuilder.AppendLine("\t\t// primary key column");
                        }

                        if (fkColumns.ContainsKey(col.Name))
                        {
                            _stringBuilder.AppendLine($"\t\t// references {fkColumns[col.Name]}");
                        }
                        
                        if (col.Size.HasValue)
                        {
                            _stringBuilder.AppendLine($"\t\t[MaxLength({col.Size})]");
                        }
                        _stringBuilder.AppendLine($"\t\tpublic {col.CSharpType} {col.Name} {{ get; set; }}");
                    }
                    _stringBuilder.AppendLine("\t}"); // class
                    _stringBuilder.AppendLine("}"); // namespace
                    return _stringBuilder;
                }
            }

        }

        private IEnumerable<ColumnInfo> GetColumnInfo(DataTable schemaTable)
        {
            List<ColumnInfo> results = new List<ColumnInfo>();

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                foreach (DataRow row in schemaTable.Rows)
                {
                    ColumnInfo columnInfo = new ColumnInfo()
                    {
                        Name = row.Field<string>("ColumnName"),
                        CSharpType = CSharpTypeName(provider, row.Field<Type>("DataType")),
                        IsNullable = row.Field<bool>("AllowDBNull")
                    };

                    if (columnInfo.IsNullable && !columnInfo.CSharpType.ToLower().Equals("string")) columnInfo.CSharpType += "?";
                    if (columnInfo.CSharpType.ToLower().Equals("string") && row.Field<int>("ColumnSize") < int.MaxValue) columnInfo.Size = row.Field<int>("ColumnSize");

                    results.Add(columnInfo);
                }
            }

            return results;
        }

        private string CSharpTypeName(CSharpCodeProvider provider, Type type)
        {
            CodeTypeReference typeRef = new CodeTypeReference(type);
            return provider.GetTypeOutput(typeRef).Replace("System.", string.Empty);
        }

        private IEnumerable<string> GetPrimaryKeyColumns(SqlConnection cn, string schema, string tableName)
        {
            return cn.Query<string>(
                @"SELECT 	
	                [col].[name]
                FROM 
	                [sys].[indexes] [ndx] INNER JOIN [sys].[index_columns] [ndxcol] ON 
		                [ndx].[object_id]=[ndxcol].[object_id] AND
		                [ndx].[index_id]=[ndxcol].[index_id]
	                INNER JOIN [sys].[columns] [col] ON 
		                [ndxcol].[column_id]=[col].[column_id] AND
		                [ndxcol].[object_id]=[col].[object_id]
	                INNER JOIN [sys].[tables] [t] ON [col].[object_id]=[t].[object_id]
                WHERE 
	                [is_primary_key]=1 AND
	                SCHEMA_NAME([t].[schema_id])=@schema AND
	                [t].[name]=@table", new { schema = schema, table = tableName });
        }

        private Dictionary<string, ColumnRef> GetForeignKeyColumns(SqlConnection cn, string schema, string tableName)
        {
            return cn.Query(
                @"SELECT 
	                [col].[Name] AS [ForeignKeyColumn], [parent].[Name] AS [TableName], SCHEMA_NAME([parent].[schema_id]) AS [Schema], [parent_col].[name] AS [ColumnName]
                FROM 
	                [sys].[foreign_key_columns] [fkcol] INNER JOIN [sys].[columns] [col] ON 
		                [fkcol].[parent_object_id]=[col].[object_id] AND
		                [fkcol].[parent_column_id]=[col].[column_id]
	                INNER JOIN [sys].[foreign_keys] [fk] ON [fkcol].[constraint_object_id]=[fk].[object_id]
	                INNER JOIN [sys].[tables] [child] ON [fkcol].[parent_object_id]=[child].[object_id]
	                INNER JOIN [sys].[tables] [parent] ON [fkcol].[referenced_object_id]=[parent].[object_id]
	                INNER JOIN [sys].[columns] [parent_col] ON 
		                [fkcol].[referenced_column_id]=[parent_col].[column_id] AND
		                [fkcol].[referenced_object_id]=[parent_col].[object_id]
                WHERE
	                SCHEMA_NAME([child].[schema_id])=@schema AND
	                [child].[name]=@table", new { schema = schema, table = tableName })
                    .ToDictionary(
                        item => (string)item.ForeignKeyColumn, 
                        item => new ColumnRef() { Schema = item.Schema, TableName = item.TableName, ColumnName = item.ColumnName });
        }

        internal class ColumnInfo
        {
            public string Name { get; set; }
            public string CSharpType { get; set; }
            public int? Size { get; set; }
            public bool IsNullable { get; set; }
        }

        public class KeyInfo
        {
            public string ColumnName { get; set; }
            public bool InPrimaryKey { get; set; }
            public ColumnRef ForeignKey { get; set; }
        }

        public class ColumnRef
        {
            public string Schema { get; set; }
            public string TableName { get; set; }
            public string ColumnName { get; set; }

            public override string ToString()
            {
                return $"{Schema}.{TableName}.{ColumnName}";
            }
        }
    }
}