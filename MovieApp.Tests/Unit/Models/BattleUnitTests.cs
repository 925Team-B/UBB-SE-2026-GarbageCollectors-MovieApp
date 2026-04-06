using System;
using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class BattleUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var battle = new Battle();

            Assert.Equal(0, battle.BattleId);
            Assert.Equal(0.0, battle.InitialRatingFirstMovie);
            Assert.Equal(0.0, battle.InitialRatingSecondMovie);
            Assert.Equal("Active", battle.Status);
            Assert.Null(battle.FirstMovie);
            Assert.Null(battle.SecondMovie);
            Assert.NotNull(battle.Bets);
            Assert.Empty(battle.Bets);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var start = new DateTime(2025, 1, 6);
            var end = new DateTime(2025, 1, 12);
            var m1 = new Movie { MovieId = 1, Title = "Inception" };
            var m2 = new Movie { MovieId = 2, Title = "Interstellar" };

            var battle = new Battle
            {
                BattleId = 7,
                InitialRatingFirstMovie = 8.5,
                InitialRatingSecondMovie = 7.9,
                StartDate = start,
                EndDate = end,
                Status = "Finished",
                FirstMovie = m1,
                SecondMovie = m2
            };

            Assert.Equal(7, battle.BattleId);
            Assert.Equal(8.5, battle.InitialRatingFirstMovie);
            Assert.Equal(7.9, battle.InitialRatingSecondMovie);
            Assert.Equal(start, battle.StartDate);
            Assert.Equal(end, battle.EndDate);
            Assert.Equal("Finished", battle.Status);
            Assert.Same(m1, battle.FirstMovie);
            Assert.Same(m2, battle.SecondMovie);
        }

        [Fact]
        public void Bets_CanAddItems()
        {
            var battle = new Battle();
            battle.Bets.Add(new Bet());
            battle.Bets.Add(new Bet());

            Assert.Equal(2, battle.Bets.Count);
        }

        [Fact]
        public void Status_DefaultIsActive()
        {
            var battle = new Battle();
            Assert.Equal("Active", battle.Status);
        }
    }
}