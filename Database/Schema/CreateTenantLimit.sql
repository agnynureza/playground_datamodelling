CREATE TABLE P_TenantLimits (
    TenantId            UNIQUEIDENTIFIER NOT NULL,
    RequestsPerWindow   INT              NOT NULL,
    WindowSizeSeconds   INT              NOT NULL,
    Tier                NVARCHAR(50)     NULL,
    CONSTRAINT PK_P_TenantLimits PRIMARY KEY (TenantId),
    CONSTRAINT FK_P_TenantLimits_P_Tenants FOREIGN KEY (TenantId) REFERENCES P_Tenants(TenantId)
);