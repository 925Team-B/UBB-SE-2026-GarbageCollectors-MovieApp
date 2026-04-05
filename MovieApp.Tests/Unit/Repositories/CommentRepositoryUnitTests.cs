using System;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Unit.Repositories
{
    public class CommentRepositoryUnitTests
    {
        private readonly CommentRepository _repo;

        public CommentRepositoryUnitTests()
        {
            _repo = new CommentRepository("fake-connection-string");
        }

        [Fact]
        public void Insert_NullAuthor_ThrowsInvalidOperationException()
        {
            var comment = new Comment
            {
                Author = null,
                Movie = new Movie { MovieId = 1 },
                Content = "Test comment",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0)
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(comment));
            Assert.Equal("Comment.Author is required for insert.", ex.Message);
        }

        [Fact]
        public void Insert_NullMovie_ThrowsInvalidOperationException()
        {
            var comment = new Comment
            {
                Author = new User { UserId = 1 },
                Movie = null,
                Content = "Test comment",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0)
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Insert(comment));
            Assert.Equal("Comment.Movie is required for insert.", ex.Message);
        }

        [Fact]
        public void Update_NullAuthor_ThrowsInvalidOperationException()
        {
            var comment = new Comment
            {
                MessageId = 1,
                Author = null,
                Movie = new Movie { MovieId = 1 },
                Content = "Updated comment",
                CreatedAt = new DateTime(2025, 2, 1, 10, 0, 0)
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(comment));
            Assert.Equal("Comment.Author is required for update.", ex.Message);
        }

        [Fact]
        public void Update_NullMovie_ThrowsInvalidOperationException()
        {
            var comment = new Comment
            {
                MessageId = 1,
                Author = new User { UserId = 1 },
                Movie = null,
                Content = "Updated comment",
                CreatedAt = new DateTime(2025, 2, 1, 10, 0, 0)
            };

            var ex = Assert.Throws<InvalidOperationException>(() => _repo.Update(comment));
            Assert.Equal("Comment.Movie is required for update.", ex.Message);
        }
    }
}