// dont forget change table name remove "P_ prefix"
CREATE TABLE P_Logs (
    Id              UNIQUEIDENTIFIER NOT NULL,
    TimestampUTC    DATETIME2        NOT NULL DEFAULT SYSUTCDATETIME(),
    TenantId        UNIQUEIDENTIFIER NULL,
    Username        NVARCHAR(256)    NULL,
    Method          NVARCHAR(10)     NOT NULL,
    Path            NVARCHAR(500)    NOT NULL,
    RequestBody     NVARCHAR(MAX)    NULL,
    ResponseBody    NVARCHAR(MAX)    NULL,
    StatusCode      INT              NOT NULL,
    DurationMs      INT              NULL,
    CONSTRAINT PK_P_Logs PRIMARY KEY (Id)
);

CREATE NONCLUSTERED INDEX IX_P_Logs_TenantId ON P_Logs(TenantId);