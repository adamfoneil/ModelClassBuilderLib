using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdamOneilSoftware.ModelClassBuilder;

namespace Test
{
    [TestClass]
    public class UnitTests
    {
        [TestMethod]
        public void Table()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";         
            
            e.CSharpClassFromTable("dbo", "Customer");
            e.SaveAs(@"C:\Users\Adam\Desktop\Customer.cs");

            e.CSharpClassFromTable("dbo", "Organization");
            e.SaveAs(@"C:\Users\Adam\Desktop\Organization.cs");
        }

        [TestMethod]
        public void Query()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";            
            e.CSharpClassFromQuery("SELECT * FROM [Organization]", "OrgaizationQuery");
            e.SaveAs(@"C:\Users\Adam\Desktop\OrganizationQuery.cs");
        }
    }
}
