CREATE TABLE [dbo].[CartonEvents] (
    [EventId] BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [MessageId] VARCHAR(50) NOT NULL UNIQUE,
    [EventType] VARCHAR(20) NOT NULL,
    [LabelNumber] VARCHAR(50) NOT NULL,
    [TargetChute] VARCHAR(20) NOT NULL,
    [ProcessedTimestamp] DATETIME NOT NULL
);