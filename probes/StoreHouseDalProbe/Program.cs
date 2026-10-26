using Psdr.Dals.Configuration;
using Psdr.Dals.StoreHouse;
using Psdr.Dals.Shop;
using Psdr.Dals.SalesMan;
using Psdr.Dals.User;
using Psdr.Dals.Security;
using Psdr.Models.Entities;
using System;
using System.Data.SqlClient;
using System.Linq;

namespace StoreHouseDalProbe
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var locations = new ProvinceCityCatalog();
            Check(locations.Provinces.Count == 34, "province resource count");
            Check(locations.GetCities("浙江").Contains("杭州"), "province city mapping");
            Check(locations.GetCities("不存在").Count == 0, "unknown province fallback");

            var databaseName = "PsdrProbe_" + Guid.NewGuid().ToString("N");
            var masterConnectionString = new SqlConnectionStringBuilder
            {
                DataSource = "localhost",
                InitialCatalog = "master",
                IntegratedSecurity = true,
                ConnectTimeout = 5
            }.ConnectionString;
            var created = false;

            try
            {
                Execute(masterConnectionString, "CREATE DATABASE [" + databaseName + "]");
                created = true;

                var tenantConnection = new SqlConnectionStringBuilder(masterConnectionString)
                {
                    InitialCatalog = databaseName
                }.ConnectionString;
                Execute(tenantConnection,
                    "CREATE TABLE dbo.StoreHouse (StoreHouseID int IDENTITY(1,1) PRIMARY KEY, " +
                    "Name nvarchar(100) NOT NULL, Remark nvarchar(500) NULL)");
                Execute(tenantConnection,
                    "CREATE TABLE dbo.SalesMan (SalesID int IDENTITY(1,1) PRIMARY KEY, " +
                    "Name nvarchar(max) NULL, State int NOT NULL)");
                Execute(tenantConnection,
                    "CREATE TABLE dbo.Shop (ShopID int IDENTITY(1,1) PRIMARY KEY, ShopName nvarchar(max) NULL, " +
                    "Province nvarchar(max) NULL, City nvarchar(max) NULL, Contacts nvarchar(max) NULL, " +
                    "Tel nvarchar(max) NULL, Phone nvarchar(max) NULL, Fax nvarchar(max) NULL, " +
                    "Salesman nvarchar(max) NULL, Addr nvarchar(max) NULL, Remark nvarchar(max) NULL)");
                Execute(tenantConnection,
                    "CREATE TABLE dbo.[User] (UserID int IDENTITY(1,1) PRIMARY KEY, UserCode nvarchar(50) NOT NULL, " +
                    "UserName nvarchar(100) NOT NULL, UserPassword nvarchar(300) NOT NULL, Role nvarchar(500) NULL, " +
                    "State int NULL, Online int NULL)");

                var dal = new StoreHouseDAL(TenantDatabase.ForConnectionString(tenantConnection));
                Check(dal.GetAll().Count == 0, "initial list");
                Check(dal.Save(new StoreHouse { Name = "测试仓库", Remark = null }), "insert");
                Check(!dal.Save(new StoreHouse { Name = "测试仓库" }), "duplicate rejection");

                var saved = dal.GetAll().Single();
                Check(saved.StoreHouseID > 0 && saved.Remark == null, "read and optional remark");
                saved.Name = "修改后的仓库";
                saved.Remark = "备注";
                Check(dal.Save(saved), "update");
                Check(dal.GetAll().Single().Name == "修改后的仓库", "updated value");
                Check(dal.Delete(saved.StoreHouseID), "delete");
                Check(dal.GetAll().Count == 0, "empty after delete");

                var salesmen = new SalesManDAL(TenantDatabase.ForConnectionString(tenantConnection));
                Check(salesmen.Save(new Psdr.Models.Entities.SalesMan { Name = "业务员甲", State = 1 }), "salesperson insert");
                Check(!salesmen.Save(new Psdr.Models.Entities.SalesMan { Name = "业务员甲", State = 1 }), "salesperson duplicate");
                var salesperson = salesmen.GetAll().Single();
                salesperson.State = 0;
                Check(salesmen.Save(salesperson) && salesmen.GetAll().Single().State == 0, "salesperson update");

                var shops = new ShopDAL(TenantDatabase.ForConnectionString(tenantConnection));
                Check(shops.GetSalesmanNames().Single() == "业务员甲", "customer salesperson list");
                Check(shops.Save(new Psdr.Models.Entities.Shop { ShopName = "测试客户", Province = "浙江", City = "杭州",
                    Contacts = "联系人", Salesman = "业务员甲" }), "customer insert");
                Check(!shops.Save(new Psdr.Models.Entities.Shop { ShopName = "测试客户" }), "customer duplicate");
                var shop = shops.GetAll().Single();
                Check(shop.ShopID > 0 && shop.Salesman == "业务员甲", "customer read");
                salesperson.Name = "业务员乙";
                Check(salesmen.Save(salesperson) && shops.GetAll().Single().Salesman == "业务员乙",
                    "salesperson rename updates customers");
                var deleteBlocked = false;
                try { salesmen.Delete(salesperson.SalesID); }
                catch (InvalidOperationException) { deleteBlocked = true; }
                Check(deleteBlocked, "referenced salesperson delete blocked");
                shop.City = "宁波";
                shop.Salesman = "业务员乙";
                Check(shops.Save(shop) && shops.GetAll().Single().City == "宁波", "customer update");
                Check(shops.Delete(shop.ShopID) && shops.GetAll().Count == 0, "customer delete");
                Check(salesmen.Delete(salesperson.SalesID) && salesmen.GetAll().Count == 0, "salesperson delete");

                var users = new UserDAL(TenantDatabase.ForConnectionString(tenantConnection));
                Check(users.Save(new Psdr.Models.Entities.User { UserCode = "worker", UserName = "操作员",
                    Role = "基础数据-客户维护|", State = 1 }), "user insert");
                Check(!users.Save(new Psdr.Models.Entities.User { UserCode = "worker", UserName = "重复",
                    Role = "基础数据-客户维护|", State = 1 }), "user duplicate");
                var user = users.GetAll().Single();
                Check(user.UserPassword == null && user.UserCode == "worker", "user list excludes password");
                Check(users.Authenticate("worker", DefaultUserCredentials.Password) != null, "default password login");
                user.UserName = "新姓名";
                user.State = 0;
                Check(users.Save(user) && users.Authenticate("worker", DefaultUserCredentials.Password) == null,
                    "user update preserves password and enforces disabled state");
                user.State = 1;
                Check(users.Save(user) && users.Authenticate("worker", DefaultUserCredentials.Password) != null,
                    "user reenabled with original password");
                var reservedBlocked = false;
                try { users.Save(new Psdr.Models.Entities.User { UserCode = "sys", UserName = "保留账号" }); }
                catch (InvalidOperationException) { reservedBlocked = true; }
                Check(reservedBlocked, "sys account reserved");
                Check(users.Delete(user.UserID) && users.GetAll().Count == 0, "user delete");
                Console.WriteLine("PASS: StoreHouse, Shop, SalesMan and User DAL workflows");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex.Message);
                return 1;
            }
            finally
            {
                if (created)
                {
                    SqlConnection.ClearAllPools();
                    Execute(masterConnectionString,
                        "ALTER DATABASE [" + databaseName + "] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                        "DROP DATABASE [" + databaseName + "]");
                }
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

        private static void Check(bool condition, string step)
        {
            if (!condition) throw new InvalidOperationException(step);
        }
    }
}
