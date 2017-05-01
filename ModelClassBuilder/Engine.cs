using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

            return BuildCSharpClass($"SELECT * FROM [{schema}].[{tableName}]");
        }

        public StringBuilder BuildCSharpClass(string query)
        {            
            using (SqlConnection cn = new SqlConnection(ConnectionString))
            {
                cn.Open();
                using (SqlCommand cmd = new SqlCommand(query, cn))
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

        internal class ColumnInfo
        {
            public string Name { get; set; }
            public string CSharpType { get; set; }
            public int? Size { get; set; }
            public bool IsNullable { get; set; }
        }

        internal class KeyInfo
        {
            public string ColumnName { get; set; }
            public bool InPrimaryKey { get; set; }
            public ColumnRef ForeignKey { get; set; }
            public string[] UniqueConstraints { get; set; }
            public string[] Indexes { get; set; }
        }

        internal class ColumnRef
        {
            public string Schema { get; set; }
            public string TableName { get; set; }
            public string ColumnName { get; set; }
        }
    }
}