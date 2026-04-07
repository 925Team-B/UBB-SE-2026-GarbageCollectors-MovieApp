using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class BattleRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly BattleRepository repo;
        private readonly MovieRepository movieRepo;

        public BattleRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_Battle_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new BattleRepository(connectionString);
            movieRepo = new MovieRepository(connectionString);

            ClearBattleTables();
        }

        private void ClearBattleTables()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Battle", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Movie", conn).ExecuteNonQuery();
        }

        private Movie CreateMovie(string title, int year, string genre, double rating)
        {
            var movie = new Movie
            {
                Title = title,
                Year = year,
                PosterUrl = title.Replace(" ", string.Empty) + ".jpg",
                Genre = genre,
                AverageRating = rating
            };

            int id = movieRepo.Insert(movie);
            movie.MovieId = id;
            return movie;
        }

        public void Dispose()
        {
            var masterConnectionString =
                "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidBattle_ReturnsNewId()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);

            var battle = new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Active"
            };

            int id = repo.Insert(battle);

            Assert.True(id > 0);

            var insertedBattle = repo.GetById(id);
            Assert.NotNull(insertedBattle);
            Assert.Equal(id, insertedBattle!.BattleId);
            Assert.Equal(firstMovie.MovieId, insertedBattle.FirstMovie!.MovieId);
            Assert.Equal(secondMovie.MovieId, insertedBattle.SecondMovie!.MovieId);
            Assert.Equal("Active", insertedBattle.Status);
        }

        [Fact]
        public void Insert_NullFirstMovie_ThrowsInvalidOperationException()
        {
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);

            var battle = new Battle
            {
                FirstMovie = null,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Pending"
            };

            Assert.Throws<InvalidOperationException>(() => repo.Insert(battle));
        }

        [Fact]
        public void GetAll_WhenBattlesExist_ReturnsBattles()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var thirdMovie = CreateMovie("Movie C", 2022, "Sci-Fi", 8.3);

            repo.Insert(new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Active"
            });

            repo.Insert(new Battle
            {
                FirstMovie = secondMovie,
                SecondMovie = thirdMovie,
                InitialRatingFirstMovie = 8.0,
                InitialRatingSecondMovie = 8.3,
                StartDate = new DateTime(2025, 2, 1),
                EndDate = new DateTime(2025, 2, 7),
                Status = "Pending"
            });

            var battles = repo.GetAll();

            Assert.Equal(2, battles.Count);
        }

        [Fact]
        public void GetById_ExistingBattle_ReturnsBattle()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);

            var battle = new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 3, 1),
                EndDate = new DateTime(2025, 3, 7),
                Status = "Finished"
            };

            int id = repo.Insert(battle);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.BattleId);
            Assert.Equal(firstMovie.MovieId, result.FirstMovie!.MovieId);
            Assert.Equal(secondMovie.MovieId, result.SecondMovie!.MovieId);
            Assert.Equal("Finished", result.Status);
        }

        [Fact]
        public void GetById_NonExistingBattle_ReturnsNull()
        {
            var result = repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingBattle_ReturnsTrueAndUpdatesBattle()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var thirdMovie = CreateMovie("Movie C", 2022, "Sci-Fi", 9.0);

            var battle = new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 4, 1),
                EndDate = new DateTime(2025, 4, 7),
                Status = "Pending"
            };

            int id = repo.Insert(battle);

            battle.BattleId = id;
            battle.SecondMovie = thirdMovie;
            battle.InitialRatingSecondMovie = 9.0;
            battle.Status = "Active";
            battle.EndDate = new DateTime(2025, 4, 10);

            bool updated = repo.Update(battle);

            Assert.True(updated);

            var updatedBattle = repo.GetById(id);
            Assert.NotNull(updatedBattle);
            Assert.Equal(thirdMovie.MovieId, updatedBattle!.SecondMovie!.MovieId);
            Assert.Equal(9.0, updatedBattle.InitialRatingSecondMovie);
            Assert.Equal("Active", updatedBattle.Status);
            Assert.Equal(new DateTime(2025, 4, 10), updatedBattle.EndDate);
        }

        [Fact]
        public void Update_NonExistingBattle_ReturnsFalse()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);

            var battle = new Battle
            {
                BattleId = 999999,
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Pending"
            };

            bool updated = repo.Update(battle);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingBattle_ReturnsTrueAndRemovesBattle()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);

            var battle = new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 5, 1),
                EndDate = new DateTime(2025, 5, 7),
                Status = "Pending"
            };

            int id = repo.Insert(battle);

            bool deleted = repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingBattle_ReturnsFalse()
        {
            bool deleted = repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}