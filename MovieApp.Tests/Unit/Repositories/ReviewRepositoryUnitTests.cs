using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class ReviewRepositoryUnitTests
    {
        private readonly ReviewRepository _repo;

        public ReviewRepositoryUnitTests()
        {
            _repo = new ReviewRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullUser_ThrowsInvalidOperationException()
        {
            var review = new Review
            {
                User = null,
                Movie = new Movie { MovieId = 1 },
                StarRating = 8.5f,
                Content = "Great movie",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0),
                IsExtraReview = false
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(review));
            Assert.Equal("Review.User is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullMovie_ThrowsInvalidOperationException()
        {
            var review = new Review
            {
                User = new User { UserId = 1 },
                Movie = null,
                StarRating = 8.5f,
                Content = "Great movie",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0),
                IsExtraReview = false
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(review));
            Assert.Equal("Review.Movie is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullUser_ThrowsInvalidOperationException()
        {
            var review = new Review
            {
                ReviewId = 1,
                User = null,
                Movie = new Movie { MovieId = 1 },
                StarRating = 9.0f,
                Content = "Updated review",
                CreatedAt = new DateTime(2025, 2, 1, 10, 0, 0),
                IsExtraReview = true
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(review));
            Assert.Equal("Review.User is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullMovie_ThrowsInvalidOperationException()
        {
            var review = new Review
            {
                ReviewId = 1,
                User = new User { UserId = 1 },
                Movie = null,
                StarRating = 9.0f,
                Content = "Updated review",
                CreatedAt = new DateTime(2025, 2, 1, 10, 0, 0),
                IsExtraReview = true
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(review));
            Assert.Equal("Review.Movie is required for update.", ex.Message);
        }
    }
}