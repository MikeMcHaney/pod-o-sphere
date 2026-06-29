-- Add append-only audit events for auth, ownership, and operator actions.

set xact_abort on;

begin transaction;

if object_id('dbo.AuditEvents', 'U') is null
begin
    create table dbo.AuditEvents (
        AuditEventId uniqueidentifier not null default newid() primary key,
        ActorAppUserId uniqueidentifier null references dbo.AppUsers(AppUserId),
        TenantId uniqueidentifier null references dbo.Tenants(TenantId),
        ShowId uniqueidentifier null references dbo.Shows(ShowId),
        EventType nvarchar(100) not null,
        TargetType nvarchar(100) null,
        TargetId nvarchar(200) null,
        ActorIdentityIssuer nvarchar(500) null,
        ActorIdentitySubject nvarchar(200) null,
        MetadataJson nvarchar(max) null,
        CreatedAtUtc datetime2 not null default sysutcdatetime()
    );

    create index IX_AuditEvents_CreatedAt_EventType
        on dbo.AuditEvents(CreatedAtUtc, EventType);

    create index IX_AuditEvents_Actor_CreatedAt
        on dbo.AuditEvents(ActorAppUserId, CreatedAtUtc);

    create index IX_AuditEvents_Tenant_CreatedAt
        on dbo.AuditEvents(TenantId, CreatedAtUtc);
end;

commit transaction;

go
