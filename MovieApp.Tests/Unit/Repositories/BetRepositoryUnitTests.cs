using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class BetRepositoryUnitTests
    {
        private readonly BetRepository repo;

        public BetRepositoryUnitTests()
        {
            repo = new BetRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = null,
                Battle = new Battle { BattleId = 1 },
                Movie = new Movie { MovieId = 2 },
                Amount = 50
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(bet));
            Assert.Equal("Bet.User is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullBattle_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = new User { UserId = 1 },
                Battle = null,
                Movie = new Movie { MovieId = 2 },
                Amount = 50
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(bet));
            Assert.Equal("Bet.Battle is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullMovie_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = new User { UserId = 1 },
                Battle = new Battle { BattleId = 1 },
                Movie = null,
                Amount = 50
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Insert(bet));
            Assert.Equal("Bet.Movie is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullUser_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = null,
                Battle = new Battle { BattleId = 1 },
                Movie = new Movie { MovieId = 2 },
                Amount = 75
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(bet));
            Assert.Equal("Bet.User is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullBattle_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = new User { UserId = 1 },
                Battle = null,
                Movie = new Movie { MovieId = 2 },
                Amount = 75
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(bet));
            Assert.Equal("Bet.Battle is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullMovie_ThrowsInvalidOperationException()
        {
            var bet = new Bet
            {
                User = new User { UserId = 1 },
                Battle = new Battle { BattleId = 1 },
                Movie = null,
                Amount = 75
            };

            var ex = Assert.Throws<InvalidOperationException>(() => repo.Update(bet));
            Assert.Equal("Bet.Movie is required for update.", ex.Message);
        }
    }
}