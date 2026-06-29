alter table dbo.ProcessingJobs
add LeaseExpiresAtUtc datetime2 null;

go

;with RankedSources as (
    select
        DataSourceId,
        first_value(DataSourceId) over (
            partition by ShowId, SourceType, SourceUrl
            order by CreatedAtUtc, DataSourceId
        ) as KeepDataSourceId,
        row_number() over (
            partition by ShowId, SourceType, SourceUrl
            order by CreatedAtUtc, DataSourceId
        ) as DuplicateRank
    from dbo.DataSources
)
update job
set DataSourceId = ranked.KeepDataSourceId
from dbo.ProcessingJobs job
join RankedSources ranked on ranked.DataSourceId = job.DataSourceId
where ranked.DuplicateRank > 1;

go

;with RankedSources as (
    select
        DataSourceId,
        row_number() over (
            partition by ShowId, SourceType, SourceUrl
            order by CreatedAtUtc, DataSourceId
        ) as DuplicateRank
    from dbo.DataSources
)
delete source
from dbo.DataSources source
join RankedSources ranked on ranked.DataSourceId = source.DataSourceId
where ranked.DuplicateRank > 1;

go

create unique index UQ_DataSources_Show_Type_Url
on dbo.DataSources(ShowId, SourceType, SourceUrl);

go

create index IX_ProcessingJobs_LeaseExpiry
on dbo.ProcessingJobs(Status, LeaseExpiresAtUtc)
where Status = 'InProgress';

go
