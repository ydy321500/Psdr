# 数据库结构来源与执行顺序

以 `E:\LQ\NewPsdr\FyPsdr\FyPsdrModels` 中的 SqlSugar 实体和新版 DAL 查询为依据。

## 主库 `Psdr_Master`

- 新版业务表（6）：`Agent`、`AgentFunction`、`NewMessage`、`RedPacketRequestList`、`RunLog`、`ServerLog`。
- 重写版管理登录表（1）：`AdminUser`。
- 初始化：依次执行 `master/001_Agent.sql`、`002_AdminUser.sql`、`003_LegacyTables.sql`，由 `scripts/Initialize-AdminDatabase.ps1` 调用。

## 每个代理库

- 基础资料：`Dictionary`、`Ini`、`Product`、`ProductMap`、`ProductResponding`、`Shop`、`Supply`、`SalesMan`、`SalesPerson`、`StoreHouse`、`User`。
- 采购、销售、库存：`BarCode`、`Indent`、`IndentInfo`、`Return`、`ReturnInfo`、`Stock`、`StorehouseRecord`、`StorehouseRecordInfo`。
- 财务与红包：`FinanceShop`、`FinanceSupply`、`PersonPriceShop`、`PersonPriceSupply`、`RedPacketList`、`RedPacketRequestList`。
- 移动端、报表及日志：`M_Cart`、`M_Indent`、`M_Product`、`M_User`、`ReportStyle`、`Log`。
- 共 32 张表（含重写版的 `TenantFunction` 镜像表）；另有 `View_IndentInfo`、`View_PersonPriceShopInfo`、`View_PersonPriceSupplyInfo`、`View_StockInfo`、`View_StorehouseRecordInfo` 5 个视图。
- 新增代理时，`TenantProvisioningService` 从程序集资源依次执行 `tenant/001_StoreHouse.sql`、`002_User.sql`、`003_LegacyTables.sql`、`004_LegacyViews.sql`、`005_TenantFunction.sql`，在同一事务中完成，并创建代理 SQL 账号、权限及业务管理员。
- 主管理员保存 `AgentFunction` 时，程序同步写入该代理库的 `TenantFunction`；业务客户端只查询自己的代理库，不读取主库。

新版源码只有实体字段、主键与自增标记，没有原数据库 DDL。新增脚本中的字符串长度、金额精度及视图连接关系是按实体和 DAL 使用方式重建的；若需要与现网数据库逐字节兼容，应再与现网结构比对。主库和代理库各有一张同名 `RedPacketRequestList`，字段映射分别由各自的实体定义。
