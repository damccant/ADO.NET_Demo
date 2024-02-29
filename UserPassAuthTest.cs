using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ADO.NET_Demo
{
    [TestClass]
    public class UserPassAuthTest
    {
        private readonly UserPassAuth auth = new();
        [TestInitialize] public void InitializeTests()
        {
            auth.TableName = "UserTest";
            auth.DropTables();
            auth.CreateTables();
        }
        [TestCleanup] public void Cleanup()
        {
            // comment out the below line to skip deleting the test data
            // (for examining the tables after the test)
            auth.DropTables();
            auth.CloseConnection();
        }

        [TestMethod] public void TestSingleUser()
        {
            Assert.IsTrue(auth.CreateUser("admin", "password1234!"));
            Assert.IsTrue(auth.TryLoginUser("admin", "password1234!"));
            Assert.IsFalse(auth.TryLoginUser("admin", "wrong password"));
            auth.ChangeUserPassword("admin", "new password");
            Assert.IsFalse(auth.TryLoginUser("admin", "password1234!"));
            Assert.IsTrue(auth.TryLoginUser("admin", "new password"));
            Assert.IsTrue(auth.DeleteUser("admin"));
            Assert.IsFalse(auth.TryLoginUser("admin", "new password"));
        }
    }
}