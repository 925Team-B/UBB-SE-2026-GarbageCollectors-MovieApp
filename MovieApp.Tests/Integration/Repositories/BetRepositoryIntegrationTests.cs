using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class BetRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly BetRepository _repo;
        private readonly MovieRepository _movieRepo;
        private readonly BattleRepository _battleRepo;

        public BetRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_Bet_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=LAPTOP-E1FUUK3D\\WIZPRO;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new BetRepository(_connectionString);
            _movieRepo = new MovieRepository(_connectionString);
            _battleRepo = new BattleRepository(_connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(_connectionString);
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
                PosterUrl = title.Replace(" ", "") + ".jpg",
                Genre = genre,
                AverageRating = rating
            };

            int id = _movieRepo.Insert(movie);
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

            int id = _battleRepo.Insert(battle);
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
                "Server=LAPTOP-E1FUUK3D\\WIZPRO;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
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

            bool inserted = _repo.Insert(bet);

            Assert.True(inserted);

            var result = _repo.GetById(1, battle.BattleId);
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

            Assert.Throws<InvalidOperationException>(() => _repo.Insert(bet));
        }

        [Fact]
        public void GetAll_WhenBetsExist_ReturnsBets()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var thirdMovie = CreateMovie("Movie C", 2022, "Sci-Fi", 8.4);

            var battle1 = CreateBattle(firstMovie, secondMovie, "Active");
            var battle2 = CreateBattle(secondMovie, thirdMovie, "Active");

            _repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle1,
                Movie = firstMovie,
                Amount = 10
            });

            _repo.Insert(new Bet
            {
                User = ExistingUser(2),
                Battle = battle2,
                Movie = thirdMovie,
                Amount = 30
            });

            var bets = _repo.GetAll();

            Assert.Equal(2, bets.Count);
        }

        [Fact]
        public void GetById_ExistingBet_ReturnsBet()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            _repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = secondMovie,
                Amount = 70
            });

            var result = _repo.GetById(1, battle.BattleId);

            Assert.NotNull(result);
            Assert.Equal(70, result!.Amount);
            Assert.Equal(1, result.User!.UserId);
            Assert.Equal(battle.BattleId, result.Battle!.BattleId);
            Assert.Equal(secondMovie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void GetById_NonExistingBet_ReturnsNull()
        {
            var result = _repo.GetById(999, 999);

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

            _repo.Insert(bet);

            bet.Movie = secondMovie;
            bet.Amount = 90;

            bool updated = _repo.Update(bet);

            Assert.True(updated);

            var result = _repo.GetById(1, battle.BattleId);
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

            bool updated = _repo.Update(bet);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingBet_ReturnsTrueAndRemovesBet()
        {
            var firstMovie = CreateMovie("Movie A", 2020, "Action", 7.5);
            var secondMovie = CreateMovie("Movie B", 2021, "Drama", 8.0);
            var battle = CreateBattle(firstMovie, secondMovie, "Active");

            _repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle,
                Movie = firstMovie,
                Amount = 35
            });

            bool deleted = _repo.Delete(1, battle.BattleId);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(1, battle.BattleId));
        }

        [Fact]
        public void Delete_NonExistingBet_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999, 999);

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

            _repo.Insert(new Bet
            {
                User = ExistingUser(1),
                Battle = battle1,
                Movie = firstMovie,
                Amount = 10
            });

            _repo.Insert(new Bet
            {
                User = ExistingUser(2),
                Battle = battle1,
                Movie = secondMovie,
                Amount = 20
            });

            _repo.Insert(new Bet
            {
                User = ExistingUser(3),
                Battle = battle2,
                Movie = thirdMovie,
                Amount = 30
            });

            _repo.DeleteByBattleId(battle1.BattleId);

            Assert.Null(_repo.GetById(1, battle1.BattleId));
            Assert.Null(_repo.GetById(2, battle1.BattleId));
            Assert.NotNull(_repo.GetById(3, battle2.BattleId));
        }
    }
}