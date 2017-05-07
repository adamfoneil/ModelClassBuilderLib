# ModelClassBuilderLib

Install via Nuget package **AoModelClassBuilder**.

I created this as part of a larger project where I needed to generate C# model classes from SQL Server queries and tables. For example, for any arbitrary SELECT query, I want to generate a C# class that matches the column names and types of the query results. Likewise, for a given schema + table name, I want to generate a class that matches the columns of the table as well as including some metadata such as primary, unqiue, and foreign key info. This metadata is returned as comments in the generated class.

I also had a need to generate classes both with and without `using` statements and `namespace` blocks. To generate classes with `using`s and `namespace`s, use the *OuterClass* methods. To include only the core class definition in the output, use the *InnerClass* methods. All of the `CSharp`* methods return a `StringBuilder`.

This package has only one object `Engine` that provides access to everything on offer:

## Properties
    SqlConnection Connection
Connection used by all methods.

    string CodeNamespace 
Sets the namespace that encloses the generated class.
    
    bool IncludeAttributes 
Indicates whether the [MaxLength] attribute is set on columns. Default is true.

## Methods
    StringBuilder CSharpInnerClassFromCommand(SqlCommand, string) 
    StringBuilder CSharpInnerClassFromQuery(string query, string className) 
    StringBuilder CSharpInnerClassFromTable(string schema, string tableName, [string className])
    StringBuilder CSharpOuterClassFromCommand(SqlCommand, string)
    StringBuilder CSharpOuterClassFromQuery(string query, string className)
    StringBuilder CSharpOuterClassFromTable(string schema, string tableName, [string className])
    void GenerateAllClasses(string outputFolder)
    void SaveAs(string fileName)
