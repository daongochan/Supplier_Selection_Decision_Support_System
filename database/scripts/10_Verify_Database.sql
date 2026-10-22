/* ================================================================
   KIỂM TRA CSDL SAU KHI DEPLOY
   Chạy sau khi đã chạy 00 -> 09
   ================================================================ */

USE SupplierSelectionDSS;
GO

-- 1. Danh sách bảng
SELECT TABLE_SCHEMA, TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = N'BASE TABLE'
ORDER BY TABLE_NAME;
GO

-- 2. Danh sách View
SELECT name AS ViewName
FROM sys.views
ORDER BY name;
GO

-- 3. Danh sách Trigger
SELECT name AS TriggerName, OBJECT_NAME(parent_id) AS ParentTable
FROM sys.triggers
WHERE parent_class_desc = N'OBJECT_OR_COLUMN'
ORDER BY ParentTable, TriggerName;
GO

-- 4. Số lượng bản ghi master data
SELECT N'Users' AS TableName, COUNT(*) AS RecordCount FROM Users
UNION ALL SELECT N'ProductGroups', COUNT(*) FROM ProductGroups
UNION ALL SELECT N'Suppliers', COUNT(*) FROM Suppliers
UNION ALL SELECT N'Criteria', COUNT(*) FROM Criteria;
GO

-- 5. Kiểm tra View
SELECT TOP 20 * FROM vw_BatchRanking ORDER BY BatchID, RankNo;
GO

SELECT TOP 20 * FROM vw_BatchCriteriaWeights ORDER BY BatchID, CriteriaID;
GO
