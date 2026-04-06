using System;
using MovieApp.Core.Models;
using Xunit;

namespace MovieApp.Tests.Unit.Models
{
    public class CommentUnitTests
    {
        [Fact]
        public void DefaultConstructor_SetsExpectedDefaults()
        {
            var comment = new Comment();

            Assert.Equal(0, comment.MessageId);
            Assert.Equal(0, comment.AuthorId);
            Assert.Equal(0, comment.MovieId);
            Assert.Null(comment.ParentCommentId);
            Assert.Equal(string.Empty, comment.Content);
            Assert.Null(comment.Author);
            Assert.Null(comment.Movie);
            Assert.Null(comment.ParentComment);
            Assert.NotNull(comment.Replies);
            Assert.Empty(comment.Replies);
        }

        [Fact]
        public void Properties_CanBeSetAndRead()
        {
            var now = DateTime.UtcNow;
            var author = new User { UserId = 10 };
            var movie = new Movie { MovieId = 5 };
            var parent = new Comment { MessageId = 1 };

            var comment = new Comment
            {
                MessageId = 99,
                AuthorId = 10,
                MovieId = 5,
                ParentCommentId = 1,
                Content = "Great movie!",
                CreatedAt = now,
                Author = author,
                Movie = movie,
                ParentComment = parent
            };

            Assert.Equal(99, comment.MessageId);
            Assert.Equal(10, comment.AuthorId);
            Assert.Equal(5, comment.MovieId);
            Assert.Equal(1, comment.ParentCommentId);
            Assert.Equal("Great movie!", comment.Content);
            Assert.Equal(now, comment.CreatedAt);
            Assert.Same(author, comment.Author);
            Assert.Same(movie, comment.Movie);
            Assert.Same(parent, comment.ParentComment);
        }


        [Fact]
        public void AuthorDisplayId_WhenAuthorIsSet_ReturnsAuthorUserId()
        {
            var comment = new Comment { Author = new User { UserId = 7 } };
            Assert.Equal(7, comment.AuthorDisplayId);
        }

        [Fact]
        public void AuthorDisplayId_WhenAuthorIsNull_ReturnsZero()
        {
            var comment = new Comment { Author = null };
            Assert.Equal(0, comment.AuthorDisplayId);
        }

        [Fact]
        public void ParentCommentDisplayId_WhenParentIsSet_ReturnsParentMessageId()
        {
            var comment = new Comment { ParentComment = new Comment { MessageId = 42 } };
            Assert.Equal(42, comment.ParentCommentDisplayId);
        }

        [Fact]
        public void ParentCommentDisplayId_WhenParentIsNull_ReturnsZero()
        {
            var comment = new Comment { ParentComment = null };
            Assert.Equal(0, comment.ParentCommentDisplayId);
        }

        [Fact]
        public void HasParentComment_WhenParentIsSet_ReturnsTrue()
        {
            var comment = new Comment { ParentComment = new Comment() };
            Assert.True(comment.HasParentComment);
        }

        [Fact]
        public void HasParentComment_WhenParentIsNull_ReturnsFalse()
        {
            var comment = new Comment { ParentComment = null };
            Assert.False(comment.HasParentComment);
        }

        [Fact]
        public void Replies_CanAddItems()
        {
            var comment = new Comment();
            comment.Replies.Add(new Comment { MessageId = 1 });
            comment.Replies.Add(new Comment { MessageId = 2 });

            Assert.Equal(2, comment.Replies.Count);
        }
    }
}