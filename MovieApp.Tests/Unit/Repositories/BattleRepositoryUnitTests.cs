using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class BattleRepositoryUnitTests
    {
        private readonly BattleRepository repo;

        public BattleRepositoryUnitTests()
        {
            repo = new BattleRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullFirstMovie_ThrowsInvalidOperationException()
        {
            var battle = new Battle
            {
                FirstMovie = null,
                SecondMovie = new Movie { MovieId = 2 },
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Pending"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(battle));
            Assert.Equal("Battle.FirstMovie is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullSecondMovie_ThrowsInvalidOperationException()
        {
            var battle = new Battle
            {
                FirstMovie = new Movie { MovieId = 1 },
                SecondMovie = null,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 1, 1),
                EndDate = new DateTime(2025, 1, 7),
                Status = "Pending"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(battle));
            Assert.Equal("Battle.SecondMovie is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullFirstMovie_ThrowsInvalidOperationException()
        {
            var battle = new Battle
            {
                BattleId = 1,
                FirstMovie = null,
                SecondMovie = new Movie { MovieId = 2 },
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 2, 1),
                EndDate = new DateTime(2025, 2, 7),
                Status = "Active"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(battle));
            Assert.Equal("Battle.FirstMovie is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullSecondMovie_ThrowsInvalidOperationException()
        {
            var battle = new Battle
            {
                BattleId = 1,
                FirstMovie = new Movie { MovieId = 1 },
                SecondMovie = null,
                InitialRatingFirstMovie = 7.5,
                InitialRatingSecondMovie = 8.0,
                StartDate = new DateTime(2025, 2, 1),
                EndDate = new DateTime(2025, 2, 7),
                Status = "Active"
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(battle));
            Assert.Equal("Battle.SecondMovie is required for update.", ex.Message);
        }
    }
}