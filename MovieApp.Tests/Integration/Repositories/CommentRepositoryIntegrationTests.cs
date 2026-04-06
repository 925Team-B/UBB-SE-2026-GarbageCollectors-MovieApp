using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class CommentRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly CommentRepository _repo;
        private readonly MovieRepository _movieRepo;

        public CommentRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_Comment_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new CommentRepository(_connectionString);
            _movieRepo = new MovieRepository(_connectionString);

            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
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
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidComment_ReturnsNewId()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                ParentComment = null,
                Content = "Nice movie",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0)
            };

            int id = _repo.Insert(comment);

            Assert.True(id > 0);

            var insertedComment = _repo.GetById(id);
            Assert.NotNull(insertedComment);
            Assert.Equal("Nice movie", insertedComment!.Content);
            Assert.Equal(1, insertedComment.Author!.UserId);
            Assert.Equal(movie.MovieId, insertedComment.Movie!.MovieId);
            Assert.Null(insertedComment.ParentComment);
        }

        [Fact]
        public void Insert_NullAuthor_ThrowsInvalidOperationException()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                Author = null,
                Movie = movie,
                Content = "Invalid comment",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0)
            };

            Assert.Throws<InvalidOperationException>(() => _repo.Insert(comment));
        }

        [Fact]
        public void GetAll_WhenCommentsExist_ReturnsComments()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            _repo.Insert(new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                Content = "First",
                CreatedAt = new DateTime(2025, 1, 1, 10, 0, 0)
            });

            _repo.Insert(new Comment
            {
                Author = ExistingUser(2),
                Movie = movie,
                Content = "Second",
                CreatedAt = new DateTime(2025, 1, 1, 11, 0, 0)
            });

            var comments = _repo.GetAll();

            Assert.Equal(2, comments.Count);
        }

        [Fact]
        public void GetById_ExistingComment_ReturnsComment()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                Content = "Find me",
                CreatedAt = new DateTime(2025, 2, 1, 9, 0, 0)
            };

            int id = _repo.Insert(comment);

            var result = _repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.MessageId);
            Assert.Equal("Find me", result.Content);
            Assert.Equal(1, result.Author!.UserId);
            Assert.Equal(movie.MovieId, result.Movie!.MovieId);
        }

        [Fact]
        public void GetById_NonExistingComment_ReturnsNull()
        {
            var result = _repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingComment_ReturnsTrueAndUpdatesComment()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                Content = "Old content",
                CreatedAt = new DateTime(2025, 3, 1, 8, 0, 0)
            };

            int id = _repo.Insert(comment);

            comment.MessageId = id;
            comment.Content = "New content";
            comment.Author = ExistingUser(2);
            comment.CreatedAt = new DateTime(2025, 3, 2, 8, 0, 0);

            bool updated = _repo.Update(comment);

            Assert.True(updated);

            var updatedComment = _repo.GetById(id);
            Assert.NotNull(updatedComment);
            Assert.Equal("New content", updatedComment!.Content);
            Assert.Equal(2, updatedComment.Author!.UserId);
            Assert.Equal(new DateTime(2025, 3, 2, 8, 0, 0), updatedComment.CreatedAt);
        }

        [Fact]
        public void Update_NonExistingComment_ReturnsFalse()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                MessageId = 999999,
                Author = ExistingUser(1),
                Movie = movie,
                Content = "Ghost",
                CreatedAt = new DateTime(2025, 3, 1, 8, 0, 0)
            };

            bool updated = _repo.Update(comment);

            Assert.False(updated);
        }

        [Fact]
        public void Delete_ExistingComment_ReturnsTrueAndRemovesComment()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var comment = new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                Content = "Delete me",
                CreatedAt = new DateTime(2025, 4, 1, 8, 0, 0)
            };

            int id = _repo.Insert(comment);

            bool deleted = _repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingComment_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999999);

            Assert.False(deleted);
        }

        [Fact]
        public void Insert_ReplyComment_SavesParentCommentId()
        {
            var movie = CreateMovie("Comment Movie", 2020, "Drama", 8.0);

            var parent = new Comment
            {
                Author = ExistingUser(1),
                Movie = movie,
                Content = "Parent comment",
                CreatedAt = new DateTime(2025, 5, 1, 10, 0, 0)
            };

            int parentId = _repo.Insert(parent);
            parent.MessageId = parentId;

            var reply = new Comment
            {
                Author = ExistingUser(2),
                Movie = movie,
                ParentComment = parent,
                Content = "Reply comment",
                CreatedAt = new DateTime(2025, 5, 1, 11, 0, 0)
            };

            int replyId = _repo.Insert(reply);

            var insertedReply = _repo.GetById(replyId);

            Assert.NotNull(insertedReply);
            Assert.NotNull(insertedReply!.ParentComment);
            Assert.Equal(parentId, insertedReply.ParentComment!.MessageId);
        }
    }
}