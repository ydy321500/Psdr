using Psdr.Dals.Admin;
using Psdr.Dals.Configuration;
using Psdr.Dals.ProductMap;
using Psdr.Dals.Dictionary;
using Psdr.Dals.Dashboard;
using Psdr.Models.Entities;
using System;
using System.Data.SqlClient;
using System.IO;

namespace AgentFunctionProbe
{
    internal static class Program
    {
        private static int Main()
        {
            var masterName = "PsdrProbe_Master_" + Guid.NewGuid().ToString("N");
            var agentId = Guid.NewGuid();
            var tenantName = agentId.ToString("D");
            var systemConnection = Connection("master");
            var masterCreated = false;
            var tenantCreated = false;
            var previousDatabase = Environment.GetEnvironmentVariable("PSDR_ADMIN_DATABASE");
            var previousServer = Environment.GetEnvironmentVariable("PSDR_ADMIN_SQL_SERVER");
            try
            {
                Execute(systemConnection, "CREATE DATABASE [" + masterName + "]");
                masterCreated = true;
                Execute(systemConnection, "CREATE DATABASE [" + tenantName + "]");
                tenantCreated = true;
                Execute(Connection(masterName), File.ReadAllText("database/master/001_Agent.sql"));
                Execute(Connection(tenantName),
                    "CREATE TABLE dbo.ProductMap (MapID int IDENTITY(1,1) PRIMARY KEY, ProductName nvarchar(max) NULL, " +
                    "KeyName nvarchar(max) NULL, ValueName nvarchar(max) NULL, FieldType nvarchar(max) NULL, " +
                    "Remark nvarchar(max) NULL, State int NULL); " +
                    "CREATE TABLE dbo.Product (ProductID int IDENTITY(1,1) PRIMARY KEY, ProductName nvarchar(max) NULL); " +
                    "CREATE TABLE dbo.[Dictionary] (DicID int IDENTITY(1,1) PRIMARY KEY, ProductName nvarchar(max) NULL, " +
                    "DicName nvarchar(max) NULL, DicValue nvarchar(max) NULL, DicModel nvarchar(max) NULL); " +
                    "CREATE TABLE dbo.Shop (ShopID int IDENTITY(1,1) PRIMARY KEY, ShopName nvarchar(100) NULL); " +
                    "CREATE TABLE dbo.Supply (SupplyID int IDENTITY(1,1) PRIMARY KEY, SupplyName nvarchar(100) NULL); " +
                    "CREATE TABLE dbo.StoreHouse (StoreHouseID int IDENTITY(1,1) PRIMARY KEY, Name nvarchar(100) NULL)");
                var oldTenantMaps = new ProductMapDAL(
                    TenantDatabase.ForConnectionString(Connection(tenantName)));
                var upgradePromptShown = false;
                try { oldTenantMaps.GetActiveProducts(); }
                catch (InvalidOperationException ex)
                {
                    upgradePromptShown = ex.Message.Contains("同步表结构");
                }
                Check(upgradePromptShown, "legacy tenant upgrade message");
                Execute(Connection(tenantName), File.ReadAllText("database/tenant/005_TenantFunction.sql"));
                Execute(Connection(masterName), "INSERT dbo.Agent " +
                    "(AgentID,AgentName,CreatTime,State,ProvisioningStatus) VALUES ('" + agentId +
                    "',N'测试代理',SYSDATETIME(),1,N'Ready')");

                Environment.SetEnvironmentVariable("PSDR_ADMIN_DATABASE", masterName);
                Environment.SetEnvironmentVariable("PSDR_ADMIN_SQL_SERVER", ".");
                var central = new CentralDatabase();
                var functions = new AgentFunctionService(central, new AgentDAL(central));
                var product = new AgentFunction
                {
                    AgentID = agentId, FunctionName = "测试产品", FunctionType = 0,
                    State = 1, StopTime = DateTime.Today.AddYears(1)
                };
                functions.Save(product);
                Check(product.FunctionID > 0, "master insert id");
                Check(Scalar(Connection(masterName), "SELECT COUNT(*) FROM dbo.AgentFunction") == 1, "master insert");
                Check(Scalar(Connection(tenantName), "SELECT COUNT(*) FROM dbo.TenantFunction") == 1, "tenant insert");
                Check(Scalar(Connection(tenantName), "SELECT COUNT(*) FROM dbo.ProductMap") == 14, "fourteen field maps");

                product.FunctionName = "更名产品";
                functions.Save(product);
                Check(Scalar(Connection(tenantName), "SELECT COUNT(*) FROM dbo.ProductMap " +
                    "WHERE ProductName=N'更名产品'") == 14, "map rename");
                var maps = new ProductMapDAL(TenantDatabase.ForConnectionString(Connection(tenantName)));
                Check(maps.GetActiveProducts().Count == 1, "active product lookup");
                var first = maps.GetMaps("更名产品")[0];
                Check(first.KeyName == "K1", "map ordering");
                Check(maps.Save(first.MapID, "更名产品", "品名", "必填", true), "map save");
                Check(Scalar(Connection(tenantName), "SELECT COUNT(*) FROM dbo.ProductMap " +
                    "WHERE ValueName=N'品名' AND State=1") == 1, "map persisted");
                var duplicateMapBlocked = false;
                try { maps.Save(maps.GetMaps("更名产品")[1].MapID, "更名产品", "品名", "必填", true); }
                catch (InvalidOperationException) { duplicateMapBlocked = true; }
                Check(duplicateMapBlocked, "duplicate map name");
                var dictionaries = new DictionaryDAL(TenantDatabase.ForConnectionString(Connection(tenantName)));
                Check(dictionaries.GetEnabledAttributes("更名产品").Count == 1, "enabled dictionary attribute");
                var option = new DictionaryTable
                {
                    ProductName = "更名产品", DicModel = "产品", DicName = "K1", DicValue = "红色"
                };
                Check(dictionaries.Save(option), "dictionary insert");
                var stored = dictionaries.GetAll("更名产品");
                Check(stored.Count == 1 && stored[0].DicValue == "红色", "dictionary read");
                var duplicateDictionaryBlocked = false;
                try { dictionaries.Save(new DictionaryTable
                    { ProductName = "更名产品", DicModel = "产品", DicName = "K1", DicValue = "红色" }); }
                catch (InvalidOperationException) { duplicateDictionaryBlocked = true; }
                Check(duplicateDictionaryBlocked, "duplicate dictionary value");
                stored[0].DicValue = "蓝色";
                Check(dictionaries.Save(stored[0]), "dictionary update");
                Check(dictionaries.Delete(stored[0].DicID, "更名产品"), "dictionary delete");
                Execute(Connection(tenantName),
                    "INSERT dbo.Shop(ShopName) VALUES (N'客户'); " +
                    "INSERT dbo.Supply(SupplyName) VALUES (N'供应商'); " +
                    "INSERT dbo.StoreHouse(Name) VALUES (N'仓库'); " +
                    "INSERT dbo.Product(ProductName) VALUES (N'更名产品')");
                var overview = new DashboardDAL(
                    TenantDatabase.ForConnectionString(Connection(tenantName))).GetSnapshot();
                Check(overview.CustomerCount == 1 && overview.SupplierCount == 1 &&
                    overview.WarehouseCount == 1 && overview.ProductCount == 1 &&
                    overview.ActiveProductTypes.Count == 1, "dashboard tenant counts");
                product.State = 0;
                functions.Save(product);
                Check(Scalar(Connection(tenantName), "SELECT State FROM dbo.TenantFunction") == 0, "state sync");
                Check(maps.GetActiveProducts().Count == 0, "disabled product hidden");
                var duplicateBlocked = false;
                try { functions.Save(new AgentFunction { AgentID = agentId, FunctionName = "更名产品",
                    FunctionType = 0, StopTime = DateTime.Today.AddYears(1), State = 1 }); }
                catch (InvalidOperationException) { duplicateBlocked = true; }
                Check(duplicateBlocked && Scalar(Connection(masterName),
                    "SELECT COUNT(*) FROM dbo.AgentFunction") == 1, "duplicate rollback");

                Execute(Connection(tenantName), "INSERT dbo.Product(ProductName) VALUES (N'更名产品')");
                var renameBlocked = false;
                try { product.FunctionName = "不可更名"; functions.Save(product); }
                catch (InvalidOperationException) { renameBlocked = true; }
                Check(renameBlocked && Scalar(Connection(masterName),
                    "SELECT COUNT(*) FROM dbo.AgentFunction WHERE FunctionName=N'更名产品'") == 1,
                    "used product rename rollback");

                Console.WriteLine("PASS: AgentFunction master/tenant synchronization and rollback");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
            finally
            {
                Environment.SetEnvironmentVariable("PSDR_ADMIN_DATABASE", previousDatabase);
                Environment.SetEnvironmentVariable("PSDR_ADMIN_SQL_SERVER", previousServer);
                SqlConnection.ClearAllPools();
                if (tenantCreated) Drop(systemConnection, tenantName);
                if (masterCreated) Drop(systemConnection, masterName);
            }
        }

        private static string Connection(string database)
        {
            return new SqlConnectionStringBuilder
            {
                DataSource = ".", InitialCatalog = database, IntegratedSecurity = true,
                TrustServerCertificate = true, ConnectTimeout = 5
            }.ConnectionString;
        }

        private static int Scalar(string connectionString, string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private static void Execute(string connectionString, string sql)
        {
            using (var connection = new SqlConnection(connectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private static void Drop(string systemConnection, string name)
        {
            Execute(systemConnection, "ALTER DATABASE [" + name + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                "DROP DATABASE [" + name + "]");
        }

        private static void Check(bool condition, string step)
        {
            if (!condition) throw new InvalidOperationException(step);
        }
    }
}
