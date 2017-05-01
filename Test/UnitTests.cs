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
            e.ClassName = "Sample";
            e.BuildCSharpClass("dbo", "Customer");
            e.SaveAs(@"C:\Users\Adam\Desktop\Customer.cs");
        }

        [TestMethod]
        public void Query()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";
            e.ClassName = "Sample";
            e.BuildCSharpClass("SELECT * FROM [Organization]");
            e.SaveAs(@"C:\Users\Adam\Desktop\Organization.cs");
        }
    }
}
