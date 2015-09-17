CREATE TABLE [dbo].[Books] (
    [B_ID]    INT            IDENTITY (1, 1) NOT NULL,
    [ISBN]    NVARCHAR (255) NULL,
    [Title]   NVARCHAR (255) NOT NULL,
    [Author]  NVARCHAR (255) NULL,
    [Edition] NVARCHAR (255) NULL,
    [Year]    NVARCHAR (255) NULL,
    [Owner]   NVARCHAR (255) NULL,
    [BrwdBy]  NVARCHAR (255) NULL,
    PRIMARY KEY CLUSTERED ([B_ID] ASC)
);