alter table dbo.DataSources
add InventoryMode nvarchar(50) not null constraint DF_DataSources_InventoryMode default 'Full',
    MaxEpisodes int null;

go

alter table dbo.DataSources
add constraint CK_DataSources_MaxEpisodes_Positive check (MaxEpisodes is null or MaxEpisodes > 0);

go
