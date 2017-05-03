using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdamOneilSoftware.ModelClassBuilder;

namespace Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void TableOuterClass()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";         
            
            e.CSharpOuterClassFromTable("dbo", "Customer");
            e.SaveAs(@"C:\Users\Adam\Desktop\CustomerOuter.cs");

            e.CSharpOuterClassFromTable("dbo", "Organization");
            e.SaveAs(@"C:\Users\Adam\Desktop\OrganizationOuter.cs");
        }

        [TestMethod]
        public void QueryOuterClass()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";            
            e.CSharpOuterClassFromQuery("SELECT * FROM [Organization]", "OrgaizationQuery");
            e.SaveAs(@"C:\Users\Adam\Desktop\OrganizationQuery.cs");
        }

        [TestMethod]
        public void TableInnerClass()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";

            e.CSharpInnerClassFromTable("dbo", "Customer");
            e.SaveAs(@"C:\Users\Adam\Desktop\CustomerInner.cs");

            e.CSharpInnerClassFromTable("dbo", "Organization");
            e.SaveAs(@"C:\Users\Adam\Desktop\OrganizationInner.cs");
        }
    }
}
