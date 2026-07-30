CREATE TABLE P_Tenants (
    TenantId        UNIQUEIDENTIFIER NOT NULL,
    Name            NVARCHAR(200)    NOT NULL,
    Password        NVARCHAR(MAX)    NOT NULL,
    ApiKey          NVARCHAR(200)    NOT NULL,
    Status          NVARCHAR(20)     NOT NULL DEFAULT 'active',
    CreatedAtUTC    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    CONSTRAINT PK_P_Tenants PRIMARY KEY (TenantId),
    CONSTRAINT CK_P_Tenants_Status CHECK (Status IN ('active', 'suspended'))
);

CREATE UNIQUE INDEX UQ_P_Tenants_ApiKey ON P_Tenants(ApiKey);