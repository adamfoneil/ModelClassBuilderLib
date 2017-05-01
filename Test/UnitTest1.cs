using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AdamOneilSoftware.ModelClassBuilder;

namespace Test
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            Engine e = new Engine();
            e.ConnectionString = "Data Source=localhost;Initial Catalog=PostulateTest;Integrated Security=True";
            e.CodeNamespace = "Whatever";
            e.ClassName = "Sample";
            e.BuildCSharpClass("dbo", "Customer");
            e.SaveAs(@"C:\Users\Adam\Desktop\Customer.cs");
        }
    }
}
