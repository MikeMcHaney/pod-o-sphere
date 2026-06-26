-- Replace the Entra-specific object ID with the OpenID Connect issuer/subject identity key.
-- Existing populated databases must explicitly backfill both values before this migration can finish.

set xact_abort on;

begin transaction;

if col_length('dbo.AppUsers', 'IdentityIssuer') is null
    alter table dbo.AppUsers add IdentityIssuer nvarchar(500) null;

if col_length('dbo.AppUsers', 'IdentitySubject') is null
    alter table dbo.AppUsers add IdentitySubject nvarchar(200) null;

go

if exists (
    select 1
    from dbo.AppUsers
    where IdentityIssuer is null or IdentitySubject is null
)
    throw 50001, 'Backfill AppUsers.IdentityIssuer and IdentitySubject before applying migration 002.', 1;

if col_length('dbo.AppUsers', 'EntraObjectId') is not null
begin
    declare @entraObjectIdConstraint sysname;

    select top (1) @entraObjectIdConstraint = keyConstraint.name
    from sys.key_constraints keyConstraint
    inner join sys.index_columns indexColumn
        on indexColumn.object_id = keyConstraint.parent_object_id
        and indexColumn.index_id = keyConstraint.unique_index_id
    inner join sys.columns columnDefinition
        on columnDefinition.object_id = indexColumn.object_id
        and columnDefinition.column_id = indexColumn.column_id
    where keyConstraint.parent_object_id = object_id('dbo.AppUsers')
        and keyConstraint.type = 'UQ'
        and columnDefinition.name = 'EntraObjectId';

    if @entraObjectIdConstraint is not null
    begin
        declare @dropConstraintSql nvarchar(max) =
            'alter table dbo.AppUsers drop constraint ' + quotename(@entraObjectIdConstraint) + ';';
        exec sys.sp_executesql @dropConstraintSql;
    end;

    alter table dbo.AppUsers drop column EntraObjectId;
end;

alter table dbo.AppUsers alter column IdentityIssuer nvarchar(500) not null;
alter table dbo.AppUsers alter column IdentitySubject nvarchar(200) not null;

if not exists (
    select 1
    from sys.key_constraints
    where parent_object_id = object_id('dbo.AppUsers')
        and name = 'UQ_AppUsers_Identity_Issuer_Subject'
)
    alter table dbo.AppUsers
        add constraint UQ_AppUsers_Identity_Issuer_Subject unique (IdentityIssuer, IdentitySubject);

commit transaction;

go
