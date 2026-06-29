-- Clarify app-user profile fields so token username hints do not masquerade as contact email.

set xact_abort on;

begin transaction;

if col_length('dbo.AppUsers', 'ContactEmail') is null
    alter table dbo.AppUsers add ContactEmail nvarchar(320) null;

if col_length('dbo.AppUsers', 'ContactEmailVerifiedAtUtc') is null
    alter table dbo.AppUsers add ContactEmailVerifiedAtUtc datetime2 null;

if col_length('dbo.AppUsers', 'PreferredUsername') is null
    alter table dbo.AppUsers add PreferredUsername nvarchar(320) null;

update dbo.AppUsers
set ContactEmail = Email
where ContactEmail is null;

alter table dbo.AppUsers alter column ContactEmail nvarchar(320) not null;

commit transaction;

go
