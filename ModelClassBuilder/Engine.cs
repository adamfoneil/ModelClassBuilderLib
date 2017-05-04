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
        private StringBuilder _content;

        public Engine()
        {
        }

        public Engine(SqlConnection connection)
        {
            Connection = connection;
        }

        public SqlConnection Connection { get; set; }        
        public string CodeNamespace { get; set; }
        public bool IncludeAttributes { get; set; } = true;

        public StringBuilder CSharpOuterClassFromTable(string schema, string tableName, string className = null)
        {
            _content = CSharpClassFromTable(schema, tableName, WriteClassHeader, WriteClassFooter);
            return _content;
        }

        public StringBuilder CSharpOuterClassFromQuery(string query, string className)
        {            
            _content = BuildCSharpClass(Connection, query, className, WriteClassHeader, WriteClassFooter);
            return _content;

        }

        public StringBuilder CSharpOuterClassFromCommand(SqlCommand command, string className)
        {
            return BuildCSharpClassFromCommand(command, className, WriteClassHeader, WriteClassFooter, columnAttributes:IncludeAttributes);
        }

        public StringBuilder CSharpInnerClassFromTable(string schema, string tableName, string className = null)
        {
            _content = CSharpClassFromTable(schema, tableName, null, null, className);
            return _content;
        }

        public StringBuilder CSharpInnerClassFromQuery(SqlConnection connection, string query, string className)
        {
            _content = BuildCSharpClass(connection, query, className, null, null);
            return _content;
        }

        public StringBuilder CSharpInnerClassFromQuery(string query, string className)
        {
            return CSharpInnerClassFromQuery(Connection, query, className);
        }

        public StringBuilder CSharpInnerClassFromCommand(SqlCommand command, string className)
        {
            return BuildCSharpClassFromCommand(command, className, null, null, columnAttributes: IncludeAttributes);
        }

        public void GenerateAllClasses(string outputFolder)
        {
            var tables = Connection.Query(
                @"SELECT SCHEMA_NAME([schema_id]) AS [Schema], [name] AS [TableName] FROM [sys].[tables]");
            foreach (var tbl in tables)
            {
                string fileName = Path.Combine(outputFolder, $"{tbl.Schema}-{tbl.TableName}.cs");
                if (!File.Exists(fileName))
                {
                    var content = CSharpOuterClassFromTable(tbl.Schema, tbl.TableName);
                    SaveInner(content, fileName);
                }
            }
        }

        public void SaveAs(string fileName)
        {
            if (_content == null) throw new NullReferenceException("Please call the CSharpClassFromTable or CSharpClassFromQuery method first.");
            SaveInner(_content, fileName);
        }

        public static IEnumerable<ColumnRef> GetReferencingTables(SqlConnection cn, string schema, string tableName)
        {
            return cn.Query<ColumnRef>(
                @"SELECT	
                    SCHEMA_NAME([child].[schema_id]) AS [Schema],
                    [child].[name] AS [TableName],
	                [child_col].[name] AS [ColumnName]
                FROM 
	                [sys].[foreign_keys] [fk] INNER JOIN [sys].[tables] [child] ON [fk].[parent_object_id]=[child].[object_id] 
	                INNER JOIN [sys].[tables] [parent] ON [fk].[referenced_object_id]=[parent].[object_id]	
	                INNER JOIN [sys].[foreign_key_columns] [fkcol] ON 
		                [fk].[object_id]=[fkcol].[constraint_object_id]
	                INNER JOIN [sys].[columns] [child_col] ON
		                [fkcol].[parent_object_id]=[child_col].[object_id] AND
		                [fkcol].[parent_column_id]=[child_col].[column_id]
                WHERE
                	SCHEMA_NAME([parent].[schema_id])=@schema AND
                	[parent].[name]=@table", new { schema = schema, table = tableName });
        }

        public static IEnumerable<string> GetPrimaryKeyColumns(SqlConnection cn, string schema, string tableName)
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

        public static Dictionary<string, ColumnRef> GetForeignKeyColumns(SqlConnection connection, string schema, string tableName)
        {
            return connection.Query(
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

        public static Dictionary<string, string> GetUniqueConstraints(SqlConnection cn, string schema, string tableName)
        {
            var unique = cn.Query(
                @"SELECT
	                [col].[name] AS [ColumnName], [ndx].[name] AS [ConstraintName]
                FROM 
	                [sys].[indexes] [ndx] INNER JOIN [sys].[index_columns] [ndxcol] ON 
		                [ndx].[object_id]=[ndxcol].[object_id] AND
		                [ndx].[index_id]=[ndxcol].[index_id]
	                INNER JOIN [sys].[columns] [col] ON 
		                [ndxcol].[column_id]=[col].[column_id] AND
		                [ndxcol].[object_id]=[col].[object_id]
	                INNER JOIN [sys].[tables] [t] ON [col].[object_id]=[t].[object_id]
                WHERE 
	                [is_unique_constraint]=1 AND
                    SCHEMA_NAME([t].[schema_id])=@schema AND
                    [t].[name]=@table", new { schema = schema, table = tableName });
            return unique.ToDictionary(
                item => (string)item.ColumnName,
                item => (string)item.ConstraintName);
        }

        public static IEnumerable<ColumnInfo> GetColumnInfo(SqlConnection connection, string schema, string tableName)
        {
            using (SqlCommand cmd = new SqlCommand($"SELECT * FROM [{schema}].[{tableName}]", connection))
            {
                return GetColumnInfo(cmd);
            }
        }

        public static IEnumerable<ColumnInfo> GetColumnInfo(SqlConnection connection, string query)
        {
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                return GetColumnInfo(cmd);
            }
        }

        public static IEnumerable<ColumnInfo> GetColumnInfo(SqlCommand command)
        {
            using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                var tbl = reader.GetSchemaTable();
                return GetColumnInfo(tbl);
            }
        }

        private StringBuilder CSharpClassFromTable(
            string schema, string tableName,
            Action<StringBuilder, IEnumerable<ColumnInfo>> header, Action<StringBuilder> footer, string className = null)
        {            
             return CSharpClassFromTable(Connection, schema, tableName, header, footer, className);
        }

        private StringBuilder CSharpClassFromTable(SqlConnection cn, string schema, string tableName, Action<StringBuilder, IEnumerable<ColumnInfo>> header, Action<StringBuilder> footer, string className)
        {
            if (string.IsNullOrEmpty(className)) className = tableName;

            var pkColumns = GetPrimaryKeyColumns(cn, schema, tableName);
            var fkColumns = GetForeignKeyColumns(cn, schema, tableName);
            var uniqueConstraints = GetUniqueConstraints(cn, schema, tableName);
            var childTables = GetReferencingTables(cn, schema, tableName).GroupBy(cr => cr.Schema + "." + cr.TableName).Select(grp => grp.Key);

            return BuildCSharpClass(
                cn, $"SELECT * FROM [{schema}].[{tableName}]", className,
                header, footer,
                pkColumns, uniqueConstraints, fkColumns, childTables, IncludeAttributes);
        }

        private static void SaveInner(StringBuilder content, string fileName)
        {
            string folder = Path.GetDirectoryName(fileName);
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            using (StreamWriter output = File.CreateText(fileName))
            {
                output.Write(content.ToString());
            }
        }

        private static StringBuilder BuildCSharpClass(
            SqlConnection connection, string query, string className,
            Action<StringBuilder, IEnumerable<ColumnInfo>> header, Action<StringBuilder> footer,
            IEnumerable<string> pkColumns = null, Dictionary<string, string> uniqueConstraints = null,
            Dictionary<string, ColumnRef> fkColumns = null, IEnumerable<string> childTables = null, bool columnAttributes = true)
        {            
            using (SqlCommand cmd = new SqlCommand(query, connection))
            {
                return BuildCSharpClassFromCommand(cmd, className, header, footer, pkColumns, uniqueConstraints, fkColumns, childTables, columnAttributes);
            }            
        }

        private static StringBuilder BuildCSharpClassFromCommand(
            SqlCommand cmd, string className, Action<StringBuilder, IEnumerable<ColumnInfo>> header, Action<StringBuilder> footer,
            IEnumerable<string> pkColumns = null, Dictionary<string, string> uniqueConstraints = null, 
            Dictionary<string, ColumnRef> fkColumns = null, IEnumerable<string> childTables = null, bool columnAttributes = true)
        {
            StringBuilder result = new StringBuilder();

            using (SqlDataReader reader = cmd.ExecuteReader(CommandBehavior.SchemaOnly))
            {
                var tbl = reader.GetSchemaTable();
                IEnumerable<ColumnInfo> columns = GetColumnInfo(tbl);

                int indentLevel = 0;

                if (header != null)
                {
                    indentLevel++;
                    header.Invoke(result, columns);
                }

                string indent = new string('\t', indentLevel);

                if (childTables?.Any() ?? false)
                {
                    result.AppendLine($"{indent}// referencing tables: {string.Join(", ", childTables)}");
                }

                result.AppendLine($"{indent}public class {className}\r\n{indent}{{");
                foreach (var col in columns)
                {
                    if (pkColumns?.Contains(col.Name) ?? false)
                    {
                        result.AppendLine($"{indent}\t// primary key column");
                    }

                    if (uniqueConstraints?.ContainsKey(col.Name) ?? false)
                    {
                        result.AppendLine($"{indent}\t// unique constraint {uniqueConstraints[col.Name]}");
                    }

                    if (fkColumns?.ContainsKey(col.Name) ?? false)
                    {
                        result.AppendLine($"{indent}\t// references {fkColumns[col.Name]}");
                    }

                    if (col.Size.HasValue && columnAttributes)
                    {
                        result.AppendLine($"{indent}\t[MaxLength({col.Size})]");
                    }
                    result.AppendLine($"{indent}\tpublic {col.CSharpType} {col.Name} {{ get; set; }}");
                }
                result.AppendLine($"{indent}}}"); // class

                footer?.Invoke(result);
            }

            return result;
        }

        private static IEnumerable<ColumnInfo> GetColumnInfo(DataTable schemaTable)
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

        private static string CSharpTypeName(CSharpCodeProvider provider, Type type)
        {
            CodeTypeReference typeRef = new CodeTypeReference(type);
            return provider.GetTypeOutput(typeRef).Replace("System.", string.Empty);
        }

        private void WriteClassHeader(StringBuilder sb, IEnumerable<ColumnInfo> columns)
        {
            sb.AppendLine("using System;");
            if (columns.Any(col => col.Size.HasValue))
            {
                sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            }
            sb.AppendLine();
            sb.AppendLine($"namespace {CodeNamespace}\r\n{{");
        }

        private void WriteClassFooter(StringBuilder sb)
        {
            sb.AppendLine("}"); // end namespace
        }

        public class ColumnInfo
        {
            public string Name { get; set; }
            public string CSharpType { get; set; }
            public int? Size { get; set; }
            public bool IsNullable { get; set; }
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