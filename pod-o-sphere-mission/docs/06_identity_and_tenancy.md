# 06 Identity and Tenancy

## Identity provider

Use Microsoft Entra ID for authentication.

## Authorization model

Use app-owned roles and permissions in MSSQL rather than relying only on Entra groups.

## Roles

### SuperAdmin

Can manage all tenants, shows, jobs, plans, domains, and system settings.

### TenantOwner

Can manage billing, users, shows, sources, portal branding, and publishing.

### TenantAdmin

Can manage shows, sources, portal branding, and content settings.

### ContentEditor

Can review summaries, tags, topics, and episode metadata.

### Viewer

Reserved for future private portals.

## Tenant-aware API rules

Every admin API request should resolve:

- Current user ID
- Tenant membership
- Role/permissions
- Active tenant context

Every content mutation must check tenant ownership.

## Public portal rules

Public portal routes resolve tenant/show from:

- Hostname
- Slug
- Custom domain mapping

Public APIs should only expose published shows and published content.

