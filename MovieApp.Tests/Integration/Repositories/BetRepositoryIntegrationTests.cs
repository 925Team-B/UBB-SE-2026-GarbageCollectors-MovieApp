using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class BetRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly BetRepository repo;
        private readonly MovieRepository movieRepo;
        private readonly BattleRepository battleRepo;

        public BetRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_Bet_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new BetRepository(connectionString);
            movieRepo = new MovieRepository(connectionString);
            battleRepo = new BattleRepository(connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Battle", conn).ExecuteNonQuery();
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

        private Battle CreateBattle(Movie firstMovie, Movie secondMovie, string status = "Pending")
        {
            var battle = new Battle
            {
                FirstMovie = firstMovie,
                SecondMovie = secondMovie,
                InitialRatingFirstMovie = firstMovie.AverageRating,
                InitialRatingSecondMovie = secondMovie.AverageRating,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = status
            };

            int id = battleRepo.Insert(battle);
            battle.BattleId = id;
            return battle;
        }

        private User ExistingUser(int userId = 1)
        {
            return new User { UserId = userId };
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
        public void Insert_ValidBet_ReturnsTrue()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            var bet = new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = firstMovie,
                Amount = 50
            };

            bool inserted = repo.Insert(bet);

            Assert.True(inserted);

            var result = repo.GetById(1, battle.BattleId);
            Assert.NotNull(result);
            Assert.Equal(50, result!.Amount);
            Assert.Equal(firstMovie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie);

            var bet = new Bet
            {
                User = null,
                Battle = battle,
                Movie = firstMovie,
                Amount = 20
            };

            Assert.Throws<InvalidOperationException>(() => repo.Insert(bet));
        }

        [Fact]
        public void GetAll_WhenBetsExist_ReturnsBets()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var thirdMovie = CreateMovie("Movie C", 2022, "Sci-Fi", 8.4);

            var battle1 = CreateBattle(firstMovie, secondMovie, "Active");
            var battle2 = CreateBattle(secondMovie, thirdMovie, "Active");

            repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle1,
                Movie = firstMovie,
                Amount = 10
            });

            repo.Insert(new Bet
            {
                User = ExistingUser(2),
                Battle = battle2,
                Movie = thirdMovie,
                Amount = 30
            });

            var bets = repo.GetAll();

            Assert.Equal(2, bets.Count);
        }

        [Fact]
        public void GetById_ExistingBet_ReturnsBet()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = secondMovie,
                Amount = 70
            });

            var result = repo.GetById(1, battle.BattleId);

            Assert.NotNull(result);
            Assert.Equal(70, result!.Amount);
            Assert.Equal(1, result.User!.UserId);
            Assert.Equal(battle.BattleId, result.Battle!.BattleId);
            Assert.Equal(secondMovie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void GetById_NonExistingBet_ReturnsNull()
        {
            var result = repo.GetById(999, 999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingBet_ReturnsTrueAndUpdatesBet()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            var bet = new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = firstMovie,
                Amount = 25
            };

            repo.Insert(bet);

            bet.Movie = secondMovie;
            bet.Amount = 90;

            bool updated = repo.Update(bet);

            Assert.True(updated);

            var result = repo.GetById(1, battle.BattleId);
            Assert.NotNull(result);
            Assert.Equal(90, result!.Amount);
            Assert.Equal(secondMovie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void Update_NonExistingBet_ReturnsFalse()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie);

            var bet = new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = firstMovie,
                Amount = 40
            };

            bool updated = repo.Update(bet);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingBet_ReturnsTrueAndRemovesBet()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = firstMovie,
                Amount = 35
            });

            bool deleted = repo.Delete(1, battle.BattleId);

            Assert.True(deleted);
            Assert.Null(repo.GetById(1, battle.BattleId));
        }

        [Fact]
        public void Delete_NonExistingBet_ReturnsFalse()
        {
            bool deleted = repo.Delete(999, 999);

            Assert.False(deleted);
        }

        [Fact]
        public void DeleteByBattleId_ExistingBattle_DeletesOnlyThatBattlesBets()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var thirdMovie = CreateMovie("Movie C", 2022, "Sci-Fi", 8.4);

            var battle1 = CreateBattle(firstMovie, secondMovie, "Active");
            var battle2 = CreateBattle(secondMovie, thirdMovie, "Active");

            repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle1,
                Movie = firstMovie,
                Amount = 10
            });

            repo.Insert(new Bet
            {
                User = ExistingUser(2),
                Battle = battle1,
                Movie = secondMovie,
                Amount = 20
            });

            repo.Insert(new Bet
            {
                User = ExistingUser(3),
                Battle = battle2,
                Movie = thirdMovie,
                Amount = 30
            });

            repo.DeleteByBattleId(battle1.BattleId);

            Assert.Null(repo.GetById(1, battle1.BattleId));
            Assert.Null(repo.GetById(2, battle1.BattleId));
            Assert.NotNull(repo.GetById(3, battle2.BattleId));
        }
    }
}