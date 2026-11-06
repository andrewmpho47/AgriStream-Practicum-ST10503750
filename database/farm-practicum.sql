CREATE TABLE dbo.FarmUsers
(
    UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FarmUsers PRIMARY KEY,
    Username NVARCHAR(50) NOT NULL,
    PasswordHash NVARCHAR(256) NOT NULL,
    SystemRole NVARCHAR(20) NOT NULL CONSTRAINT DF_FarmUsers_SystemRole DEFAULT N'Farmer',
    CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FarmUsers_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT UQ_FarmUsers_Username UNIQUE (Username),
    CONSTRAINT CK_FarmUsers_SystemRole CHECK (SystemRole IN (N'Farmer', N'Agronomist', N'Admin'))
);

CREATE TABLE dbo.Devices
(
    DeviceId NVARCHAR(100) NOT NULL CONSTRAINT PK_Devices PRIMARY KEY,
    OperationalStatus NVARCHAR(30) NOT NULL CONSTRAINT DF_Devices_OperationalStatus DEFAULT N'Online',
    LastCommand NVARCHAR(20) NULL,
    LastCommandUtc DATETIME2(3) NULL,
    UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_Devices_UpdatedAtUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Devices_OperationalStatus CHECK (OperationalStatus IN (N'Online', N'Irrigating'))
);

CREATE INDEX IX_FarmUsers_SystemRole ON dbo.FarmUsers (SystemRole);
CREATE INDEX IX_Devices_UpdatedAtUtc ON dbo.Devices (UpdatedAtUtc DESC);

IF DATABASE_PRINCIPAL_ID(N'FarmWebAppRole') IS NULL
    EXEC(N'CREATE ROLE FarmWebAppRole');
IF DATABASE_PRINCIPAL_ID(N'FarmFunctionsRole') IS NULL
    EXEC(N'CREATE ROLE FarmFunctionsRole');

GRANT SELECT, INSERT, UPDATE ON dbo.FarmUsers TO FarmWebAppRole;
GRANT SELECT, INSERT, UPDATE ON dbo.Devices TO FarmFunctionsRole;
