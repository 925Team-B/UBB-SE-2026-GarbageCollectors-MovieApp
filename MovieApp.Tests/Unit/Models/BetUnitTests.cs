using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class BetUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var bet = new Bet();

            Assert.Equal(0, bet.Amount);
            Assert.Null(bet.User);
            Assert.Null(bet.Battle);
            Assert.Null(bet.Movie);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var user = new User { UserId = 1 };
            var battle = new Battle { BattleId = 2 };
            var movie = new Movie { MovieId = 3 };

            var bet = new Bet
            {
                Amount = 50,
                User = user,
                Battle = battle,
                Movie = movie
            };

            Assert.Equal(50, bet.Amount);
            Assert.Same(user, bet.User);
            Assert.Same(battle, bet.Battle);
            Assert.Same(movie, bet.Movie);
        }

        [Fact]
        public void Amount_CanBeZero()
        {
            var bet = new Bet { Amount = 0 };
            Assert.Equal(0, bet.Amount);
        }

        [Fact]
        public void Amount_CanBeNegative()
        {
            var bet = new Bet { Amount = -10 };
            Assert.Equal(-10, bet.Amount);
        }
    }
}