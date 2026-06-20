-- Pod-o-Sphere MSSQL core metadata schema
-- Ownership: tenancy, identity mapping, billing metadata, domains, sources, and jobs.

create table dbo.Tenants (
    TenantId uniqueidentifier not null default newid() primary key,
    TenantName nvarchar(200) not null,
    Slug nvarchar(120) not null unique,
    Status nvarchar(50) not null default 'Active',
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null
);

go

create table dbo.AppUsers (
    AppUserId uniqueidentifier not null default newid() primary key,
    EntraObjectId nvarchar(100) not null unique,
    Email nvarchar(320) not null,
    DisplayName nvarchar(200) null,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    LastLoginAtUtc datetime2 null
);

go

create table dbo.Roles (
    RoleId int identity(1,1) not null primary key,
    RoleName nvarchar(100) not null unique,
    Description nvarchar(500) null
);

go

insert into dbo.Roles (RoleName, Description)
values
('SuperAdmin', 'Pod-o-Sphere operator with full platform access'),
('TenantOwner', 'Client owner with billing and admin access'),
('TenantAdmin', 'Client admin with show and portal management access'),
('ContentEditor', 'Can review and edit content intelligence metadata'),
('Viewer', 'Reserved for future private portals');

go

create table dbo.TenantUsers (
    TenantUserId uniqueidentifier not null default newid() primary key,
    TenantId uniqueidentifier not null references dbo.Tenants(TenantId),
    AppUserId uniqueidentifier not null references dbo.AppUsers(AppUserId),
    RoleId int not null references dbo.Roles(RoleId),
    IsActive bit not null default 1,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    constraint UQ_TenantUsers_Tenant_User_Role unique (TenantId, AppUserId, RoleId)
);

go

create table dbo.Shows (
    ShowId uniqueidentifier not null default newid() primary key,
    TenantId uniqueidentifier not null references dbo.Tenants(TenantId),
    ShowName nvarchar(200) not null,
    Slug nvarchar(120) not null,
    Description nvarchar(max) null,
    Status nvarchar(50) not null default 'Draft',
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null,
    constraint UQ_Shows_Tenant_Slug unique (TenantId, Slug)
);

go

create table dbo.PortalSettings (
    PortalSettingsId uniqueidentifier not null default newid() primary key,
    ShowId uniqueidentifier not null unique references dbo.Shows(ShowId),
    PortalDisplayName nvarchar(200) null,
    LogoBlobUrl nvarchar(1000) null,
    BannerBlobUrl nvarchar(1000) null,
    PrimaryHex char(7) not null default '#2563EB',
    SecondaryHex char(7) not null default '#7C3AED',
    AccentHex char(7) not null default '#F97316',
    BackgroundHex char(7) not null default '#FFFFFF',
    TextHex char(7) not null default '#111827',
    IsPublished bit not null default 0,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null,
    constraint CK_PortalSettings_PrimaryHex check (PrimaryHex like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    constraint CK_PortalSettings_SecondaryHex check (SecondaryHex like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    constraint CK_PortalSettings_AccentHex check (AccentHex like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    constraint CK_PortalSettings_BackgroundHex check (BackgroundHex like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]'),
    constraint CK_PortalSettings_TextHex check (TextHex like '#[0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f][0-9A-Fa-f]')
);

go

create table dbo.PortalDomains (
    PortalDomainId uniqueidentifier not null default newid() primary key,
    ShowId uniqueidentifier not null references dbo.Shows(ShowId),
    Hostname nvarchar(255) not null unique,
    DomainType nvarchar(50) not null default 'Custom', -- HostedSlug, Custom
    VerificationStatus nvarchar(50) not null default 'Pending',
    VerificationToken nvarchar(200) null,
    VerifiedAtUtc datetime2 null,
    CreatedAtUtc datetime2 not null default sysutcdatetime()
);

go

create table dbo.DataSources (
    DataSourceId uniqueidentifier not null default newid() primary key,
    ShowId uniqueidentifier not null references dbo.Shows(ShowId),
    SourceType nvarchar(50) not null, -- YouTubeChannel, PodcastRss, ManualUpload
    SourceUrl nvarchar(1000) not null,
    ExternalSourceId nvarchar(200) null,
    Status nvarchar(50) not null default 'Pending',
    LastInventoryAtUtc datetime2 null,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null
);

go

create table dbo.ProcessingJobs (
    ProcessingJobId uniqueidentifier not null default newid() primary key,
    TenantId uniqueidentifier not null references dbo.Tenants(TenantId),
    ShowId uniqueidentifier not null references dbo.Shows(ShowId),
    DataSourceId uniqueidentifier null references dbo.DataSources(DataSourceId),
    JobType nvarchar(100) not null,
    Status nvarchar(50) not null default 'Pending',
    Priority int not null default 50,
    PayloadJson nvarchar(max) null,
    AttemptCount int not null default 0,
    MaxAttempts int not null default 3,
    ClaimedBy nvarchar(200) null,
    ClaimedAtUtc datetime2 null,
    LastHeartbeatAtUtc datetime2 null,
    StartedAtUtc datetime2 null,
    CompletedAtUtc datetime2 null,
    ErrorCode nvarchar(100) null,
    ErrorMessage nvarchar(max) null,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null
);

go

create index IX_ProcessingJobs_Status_Priority on dbo.ProcessingJobs(Status, Priority desc, CreatedAtUtc);
create index IX_ProcessingJobs_ShowId on dbo.ProcessingJobs(ShowId);

go

create table dbo.BillingAccounts (
    BillingAccountId uniqueidentifier not null default newid() primary key,
    TenantId uniqueidentifier not null unique references dbo.Tenants(TenantId),
    BillingProvider nvarchar(50) not null default 'Stripe',
    ProviderCustomerId nvarchar(200) null,
    PlanCode nvarchar(100) null,
    BillingStatus nvarchar(50) not null default 'Trial',
    CurrentPeriodStartUtc datetime2 null,
    CurrentPeriodEndUtc datetime2 null,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null
);

go

create table dbo.UsageCounters (
    UsageCounterId uniqueidentifier not null default newid() primary key,
    TenantId uniqueidentifier not null references dbo.Tenants(TenantId),
    ShowId uniqueidentifier null references dbo.Shows(ShowId),
    UsageMonth char(7) not null, -- YYYY-MM
    VideosProcessed int not null default 0,
    TranscriptMinutes int not null default 0,
    Searches int not null default 0,
    ChatMessages int not null default 0,
    CreatedAtUtc datetime2 not null default sysutcdatetime(),
    UpdatedAtUtc datetime2 null,
    constraint UQ_UsageCounters_Tenant_Show_Month unique (TenantId, ShowId, UsageMonth)
);

go
