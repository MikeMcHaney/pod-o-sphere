-- Split platform roles from tenant memberships and add invite/claim onboarding tables.

set xact_abort on;

begin transaction;

if object_id('dbo.PlatformRoles', 'U') is null
begin
    create table dbo.PlatformRoles (
        PlatformRoleId int identity(1,1) not null primary key,
        RoleName nvarchar(100) not null unique,
        Description nvarchar(500) null
    );
end;

if not exists (select 1 from dbo.PlatformRoles where RoleName = 'SuperAdmin')
    insert into dbo.PlatformRoles (RoleName, Description)
    values ('SuperAdmin', 'Pod-o-Sphere operator with full platform access');

if not exists (select 1 from dbo.PlatformRoles where RoleName = 'SupportAdmin')
    insert into dbo.PlatformRoles (RoleName, Description)
    values ('SupportAdmin', 'Pod-o-Sphere support operator with delegated customer support access');

if object_id('dbo.PlatformUserRoles', 'U') is null
begin
    create table dbo.PlatformUserRoles (
        PlatformUserRoleId uniqueidentifier not null default newid() primary key,
        AppUserId uniqueidentifier not null references dbo.AppUsers(AppUserId),
        PlatformRoleId int not null references dbo.PlatformRoles(PlatformRoleId),
        IsActive bit not null default 1,
        CreatedAtUtc datetime2 not null default sysutcdatetime(),
        constraint UQ_PlatformUserRoles_User_Role unique (AppUserId, PlatformRoleId)
    );
end;

declare @superAdminPlatformRoleId int = (
    select PlatformRoleId from dbo.PlatformRoles where RoleName = 'SuperAdmin'
);

insert into dbo.PlatformUserRoles (AppUserId, PlatformRoleId)
select distinct tenantUser.AppUserId, @superAdminPlatformRoleId
from dbo.TenantUsers tenantUser
inner join dbo.Roles roleDefinition on roleDefinition.RoleId = tenantUser.RoleId
where roleDefinition.RoleName = 'SuperAdmin'
    and not exists (
        select 1
        from dbo.PlatformUserRoles existing
        where existing.AppUserId = tenantUser.AppUserId
            and existing.PlatformRoleId = @superAdminPlatformRoleId
    );

delete tenantUser
from dbo.TenantUsers tenantUser
inner join dbo.Roles roleDefinition on roleDefinition.RoleId = tenantUser.RoleId
where roleDefinition.RoleName = 'SuperAdmin';

delete from dbo.Roles
where RoleName = 'SuperAdmin';

if object_id('dbo.Invitations', 'U') is null
begin
    create table dbo.Invitations (
        InvitationId uniqueidentifier not null default newid() primary key,
        TenantId uniqueidentifier not null references dbo.Tenants(TenantId),
        ShowId uniqueidentifier null references dbo.Shows(ShowId),
        CreatedByAppUserId uniqueidentifier not null references dbo.AppUsers(AppUserId),
        AcceptedByAppUserId uniqueidentifier null references dbo.AppUsers(AppUserId),
        Email nvarchar(320) not null,
        RoleName nvarchar(100) not null,
        InvitationTokenHash nvarchar(200) not null unique,
        Status nvarchar(50) not null default 'Pending',
        ExpiresAtUtc datetime2 not null,
        CreatedAtUtc datetime2 not null default sysutcdatetime(),
        AcceptedAtUtc datetime2 null
    );

    create index IX_Invitations_Tenant_Email_Status
        on dbo.Invitations(TenantId, Email, Status);
end;

if object_id('dbo.ShowClaims', 'U') is null
begin
    create table dbo.ShowClaims (
        ShowClaimId uniqueidentifier not null default newid() primary key,
        ShowId uniqueidentifier null references dbo.Shows(ShowId),
        RequestingAppUserId uniqueidentifier not null references dbo.AppUsers(AppUserId),
        ReviewedByAppUserId uniqueidentifier null references dbo.AppUsers(AppUserId),
        ClaimType nvarchar(50) not null,
        SourceUrl nvarchar(1000) not null,
        VerificationTokenHash nvarchar(200) null,
        Status nvarchar(50) not null default 'Pending',
        Notes nvarchar(max) null,
        CreatedAtUtc datetime2 not null default sysutcdatetime(),
        ReviewedAtUtc datetime2 null
    );

    create index IX_ShowClaims_RequestingUser_Status
        on dbo.ShowClaims(RequestingAppUserId, Status);

    create index IX_ShowClaims_Show_Status
        on dbo.ShowClaims(ShowId, Status);
end;

commit transaction;

go
