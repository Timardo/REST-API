CREATE TABLE .[Employees]
(
	[Id] INT NOT NULL PRIMARY KEY, 
    [Title] VARCHAR(50) NULL, 
    [Name] NVARCHAR(25) NOT NULL, 
    [Surname] NVARCHAR(25) NOT NULL, 
    [Phone] VARCHAR(20) NULL, 
    [Email] VARCHAR(50) NULL, 
    [Manager] INT NOT NULL, 
    [Code] NVARCHAR(50) NULL
)
