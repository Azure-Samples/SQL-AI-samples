create table [dbo].[langtable] (id int Identity, username nvarchar(100))
GO

insert into [dbo].[langtable] (username) values('sammy')
insert into [dbo].[langtable] (username) values('mary')
insert into [dbo].[langtable] (username) values('jane')
insert into [dbo].[langtable] (username) values('fred')
insert into [dbo].[langtable] (username) values('billy')
insert into [dbo].[langtable] (username) values('jonny')
insert into [dbo].[langtable] (username) values('kenny')
insert into [dbo].[langtable] (username) values('dan')
insert into [dbo].[langtable] (username) values('frank')
insert into [dbo].[langtable] (username) values('jenny')
GO

select * from [dbo].[langtable]
GO
