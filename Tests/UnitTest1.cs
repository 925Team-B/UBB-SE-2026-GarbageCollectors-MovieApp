using MovieApp.Core.Models;
using Xunit;

namespace Tests
{
    public class UnitTest1
    {
        [Fact]
        public void Movie_ToString_ReturnsTitle()
        {
            // Arrange
            var movie = new Movie { Title = "Inception" };

            // Act
            var result = movie.ToString();

            // Assert
            Assert.Equal("Inception", result);
        }
    }
}