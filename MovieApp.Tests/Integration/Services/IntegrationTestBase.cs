using Microsoft.Data.SqlClient;

namespace Tests.Integration.Services;

public abstract class IntegrationTestBase : IDisposable
{
    protected const string ConnectionString =
        @"Data Source=(localdb)\MovieAppTest;Initial Catalog=MovieAppIntegrationTests;Integrated Security=True;TrustServerCertificate=True;";

    protected IntegrationTestBase()
    {
        EnsureSchemaExists();
        CleanTables();
    }

    private static void EnsureSchemaExists()
    {
        var builder = new SqlConnectionStringBuilder(ConnectionString);
        var dbName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        using (var masterConnection = new SqlConnection(builder.ConnectionString))
        {
            masterConnection.Open();
            using var createDbCmd = new SqlCommand(
                $"IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{dbName}') CREATE DATABASE [{dbName}];",
                masterConnection);
            createDbCmd.ExecuteNonQuery();
        }

        using var connection = new SqlConnection(ConnectionString);
        connection.Open();

        var sql = @"
IF OBJECT_ID(N'[User]', N'U') IS NULL
    CREATE TABLE [User] (UserId INT IDENTITY(1,1) PRIMARY KEY);

IF OBJECT_ID(N'Badge', N'U') IS NULL
    CREATE TABLE Badge (
        BadgeId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(100) NOT NULL,
        CriteriaValue INT NOT NULL,
        CONSTRAINT CK_Badge_CriteriaValue_NonNegative CHECK (CriteriaValue >= 0)
    );

IF OBJECT_ID(N'Movie', N'U') IS NULL
    CREATE TABLE Movie (
        MovieId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(255) NOT NULL,
        [Year] INT NOT NULL,
        Genre NVARCHAR(100),
        PosterUrl NVARCHAR(2083),
        AverageRating FLOAT DEFAULT 0.0,
        CONSTRAINT CK_Movie_Year_Reasonable CHECK ([Year] BETWEEN 1888 AND 2100),
        CONSTRAINT CK_Movie_AverageRating_Range CHECK (AverageRating >= 0 AND AverageRating <= 10)
    );

IF OBJECT_ID(N'UserStats', N'U') IS NULL
    CREATE TABLE UserStats (
        StatsId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        TotalPoints INT NOT NULL DEFAULT 0,
        WeeklyScore INT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_UserStats_UserId UNIQUE (UserId),
        CONSTRAINT CK_UserStats_TotalPoints_NonNegative CHECK (TotalPoints >= 0),
        CONSTRAINT CK_UserStats_WeeklyScore_NonNegative CHECK (WeeklyScore >= 0),
        CONSTRAINT FK_UserStats_User FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE CASCADE
    );

IF OBJECT_ID(N'UserBadge', N'U') IS NULL
    CREATE TABLE UserBadge (
        UserId INT NOT NULL,
        BadgeId INT NOT NULL,
        PRIMARY KEY (UserId, BadgeId),
        CONSTRAINT FK_UserBadge_User FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE CASCADE,
        CONSTRAINT FK_UserBadge_Badge FOREIGN KEY (BadgeId) REFERENCES Badge(BadgeId) ON DELETE CASCADE
    );

IF OBJECT_ID(N'Review', N'U') IS NULL
    CREATE TABLE Review (
        ReviewId INT IDENTITY(1,1) PRIMARY KEY,
        UserId INT NOT NULL,
        MovieId INT NOT NULL,
        StarRating FLOAT NOT NULL,
        Content NVARCHAR(MAX),
        CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        IsExtraReview BIT NOT NULL DEFAULT 0,
        CinematographyRating INT NULL,
        CinematographyText NVARCHAR(MAX) NULL,
        ActingRating INT NULL,
        ActingText NVARCHAR(MAX) NULL,
        CgiRating INT NULL,
        CgiText NVARCHAR(MAX) NULL,
        PlotRating INT NULL,
        PlotText NVARCHAR(MAX) NULL,
        SoundRating INT NULL,
        SoundText NVARCHAR(MAX) NULL,
        CONSTRAINT CK_Review_StarRating_Range CHECK (StarRating >= 0 AND StarRating <= 10),
        CONSTRAINT FK_Review_User FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE CASCADE,
        CONSTRAINT FK_Review_Movie FOREIGN KEY (MovieId) REFERENCES Movie(MovieId) ON DELETE CASCADE
    );

IF OBJECT_ID(N'Battle', N'U') IS NULL
    CREATE TABLE Battle (
        BattleId INT IDENTITY(1,1) PRIMARY KEY,
        FirstMovieId INT NOT NULL,
        SecondMovieId INT NOT NULL,
        InitialRatingFirstMovie FLOAT NOT NULL,
        InitialRatingSecondMovie FLOAT NOT NULL,
        StartDate DATETIME2(3) NOT NULL,
        EndDate DATETIME2(3) NOT NULL,
        Status INT NOT NULL DEFAULT 0,
        CONSTRAINT CK_Battle_DifferentMovies CHECK (FirstMovieId <> SecondMovieId),
        CONSTRAINT FK_Battle_FirstMovie FOREIGN KEY (FirstMovieId) REFERENCES Movie(MovieId) ON DELETE NO ACTION,
        CONSTRAINT FK_Battle_SecondMovie FOREIGN KEY (SecondMovieId) REFERENCES Movie(MovieId) ON DELETE NO ACTION
    );

IF OBJECT_ID(N'Comment', N'U') IS NULL
    CREATE TABLE Comment (
        MessageId INT IDENTITY(1,1) PRIMARY KEY,
        AuthorId INT NOT NULL,
        MovieId INT NOT NULL,
        ParentCommentId INT NULL,
        Content NVARCHAR(MAX) NOT NULL,
        CreatedAt DATETIME2(3) NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_Comment_Author FOREIGN KEY (AuthorId) REFERENCES [User](UserId) ON DELETE NO ACTION,
        CONSTRAINT FK_Comment_Movie FOREIGN KEY (MovieId) REFERENCES Movie(MovieId) ON DELETE CASCADE,
        CONSTRAINT FK_Comment_Parent FOREIGN KEY (ParentCommentId) REFERENCES Comment(MessageId) ON DELETE NO ACTION
    );

IF OBJECT_ID(N'Bet', N'U') IS NULL
    CREATE TABLE Bet (
        BetId INT IDENTITY(1,1) PRIMARY KEY,
        BattleId INT NOT NULL,
        UserId INT NOT NULL,
        MovieId INT NOT NULL,
        Amount INT NOT NULL,
        CONSTRAINT CK_Bet_Amount_Positive CHECK (Amount > 0),
        CONSTRAINT FK_Bet_Battle FOREIGN KEY (BattleId) REFERENCES Battle(BattleId) ON DELETE CASCADE,
        CONSTRAINT FK_Bet_User FOREIGN KEY (UserId) REFERENCES [User](UserId) ON DELETE NO ACTION,
        CONSTRAINT FK_Bet_Movie FOREIGN KEY (MovieId) REFERENCES Movie(MovieId) ON DELETE NO ACTION
    );";

        using var cmd = new SqlCommand(sql, connection);
        cmd.ExecuteNonQuery();
    }

    private static void CleanTables()
    {
        using var connection = new SqlConnection(ConnectionString);
        connection.Open();
        using var cmd = new SqlCommand(@"
            DELETE FROM Bet;
            DELETE FROM Comment;
            DELETE FROM Battle;
            DELETE FROM Review;
            DELETE FROM UserBadge;
            DELETE FROM UserStats;
            DELETE FROM [User];
            DELETE FROM Movie;
            DELETE FROM Badge;
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'UserStats') DBCC CHECKIDENT ('UserStats', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'Review') DBCC CHECKIDENT ('Review', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'User') DBCC CHECKIDENT ('[User]', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'Movie') DBCC CHECKIDENT ('Movie', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'Badge') DBCC CHECKIDENT ('Badge', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'Battle') DBCC CHECKIDENT ('Battle', RESEED, 0);
            IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE OBJECT_NAME(object_id) = 'Bet') DBCC CHECKIDENT ('Bet', RESEED, 0);
        ", connection);
        cmd.ExecuteNonQuery();
    }

    public void Dispose() => CleanTables();
}