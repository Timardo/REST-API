CREATE TABLE [dbo].[Nodes]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Name] NVARCHAR(25) NOT NULL, 
    [Code] NVARCHAR(50) NOT NULL, 
    [Parentcode] NVARCHAR(50) NOT NULL, 
)
