/* =====================================================================
   1. NHÓM DANH MỤC (MASTER DATA)
   ===================================================================== */

-- 1.1. Người dùng hệ thống
CREATE TABLE Users (
    UserID        INT IDENTITY(1,1) NOT NULL,
    Username      NVARCHAR(50)   NOT NULL,
    PasswordHash  NVARCHAR(255)  NOT NULL,          -- Hash (bcrypt / argon2 / PBKDF2)
    FullName      NVARCHAR(100)  NOT NULL,
    Email         NVARCHAR(100)  NULL,
    Phone         NVARCHAR(20)   NULL,
    Role          NVARCHAR(30)   NOT NULL,          -- Administrator | PurchasingStaff | Expert | Manager
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_Users_Status DEFAULT (N'Active'),
    LastLoginAt   DATETIME2      NULL,
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_Users PRIMARY KEY (UserID),
    CONSTRAINT UQ_Users_Username UNIQUE (Username),
    CONSTRAINT CK_Users_Role   CHECK (Role IN (N'Administrator', N'PurchasingStaff', N'Expert', N'Manager')),
    CONSTRAINT CK_Users_Status CHECK (Status IN (N'Active', N'Locked', N'Inactive'))
);
GO

-- 1.2. Nhóm hàng hoá / dịch vụ
CREATE TABLE ProductGroups (
    GroupID       INT IDENTITY(1,1) NOT NULL,
    GroupCode     NVARCHAR(20)   NOT NULL,
    GroupName     NVARCHAR(150)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_ProductGroups_Status DEFAULT (N'Active'),
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_ProductGroups_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_ProductGroups PRIMARY KEY (GroupID),
    CONSTRAINT UQ_ProductGroups_Code UNIQUE (GroupCode),
    CONSTRAINT CK_ProductGroups_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

-- 1.3. Nhà cung cấp
CREATE TABLE Suppliers (
    SupplierID     INT IDENTITY(1,1) NOT NULL,
    SupplierCode   NVARCHAR(20)   NOT NULL,
    SupplierName   NVARCHAR(200)  NOT NULL,
    Address        NVARCHAR(255)  NULL,
    ContactPerson  NVARCHAR(100)  NULL,
    Email          NVARCHAR(100)  NULL,
    Phone          NVARCHAR(20)   NULL,
    TaxCode        NVARCHAR(20)   NULL,
    Website        NVARCHAR(255)  NULL,
    BusinessField  NVARCHAR(150)  NULL,
    PaymentTerms   NVARCHAR(255)  NULL,
    Status         NVARCHAR(15)   NOT NULL CONSTRAINT DF_Suppliers_Status DEFAULT (N'Active'),
    CreatedAt      DATETIME2      NOT NULL CONSTRAINT DF_Suppliers_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt      DATETIME2      NULL,
    CONSTRAINT PK_Suppliers PRIMARY KEY (SupplierID),
    CONSTRAINT UQ_Suppliers_Code UNIQUE (SupplierCode),
    CONSTRAINT CK_Suppliers_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO

-- 1.4. Quan hệ N-N Nhà cung cấp ↔ Nhóm hàng hoá
CREATE TABLE SupplierGroups (
    SupplierID  INT NOT NULL,
    GroupID     INT NOT NULL,
    CONSTRAINT PK_SupplierGroups PRIMARY KEY (SupplierID, GroupID),
    CONSTRAINT FK_SupplierGroups_Supplier FOREIGN KEY (SupplierID) REFERENCES Suppliers(SupplierID),
    CONSTRAINT FK_SupplierGroups_Group    FOREIGN KEY (GroupID)    REFERENCES ProductGroups(GroupID)
);
GO

-- 1.5. Tiêu chí đánh giá
CREATE TABLE Criteria (
    CriteriaID    INT IDENTITY(1,1) NOT NULL,
    CriteriaCode  NVARCHAR(20)   NOT NULL,
    CriteriaName  NVARCHAR(150)  NOT NULL,
    Description   NVARCHAR(500)  NULL,
    CriteriaType  NVARCHAR(10)   NOT NULL,   -- Benefit | Cost
    Unit          NVARCHAR(30)   NULL,
    Status        NVARCHAR(15)   NOT NULL CONSTRAINT DF_Criteria_Status DEFAULT (N'Active'),
    CreatedAt     DATETIME2      NOT NULL CONSTRAINT DF_Criteria_CreatedAt DEFAULT (SYSDATETIME()),
    UpdatedAt     DATETIME2      NULL,
    CONSTRAINT PK_Criteria PRIMARY KEY (CriteriaID),
    CONSTRAINT UQ_Criteria_Code UNIQUE (CriteriaCode),
    CONSTRAINT CK_Criteria_Type   CHECK (CriteriaType IN (N'Benefit', N'Cost')),
    CONSTRAINT CK_Criteria_Status CHECK (Status IN (N'Active', N'Inactive'))
);
GO
