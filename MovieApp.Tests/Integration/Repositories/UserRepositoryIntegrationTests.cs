using System;
using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class UserRepositoryIntegrationTests : IDisposable
    {
        private readonly string databaseName;
        private readonly string connectionString;
        private readonly UserRepository repo;

        public UserRepositoryIntegrationTests()
        {
            databaseName = "MovieAppTestDb_User_" + Guid.NewGuid().ToString("N");

            connectionString =
                $"Server=.\\SQLEXPRESS;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(connectionString);
            initializer.EnsureCreatedAndSeeded();

            repo = new UserRepository(connectionString);
            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();

            new SqlCommand("DELETE FROM UserBadge", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM UserStats", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Bet", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Review", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM Comment", conn).ExecuteNonQuery();
            new SqlCommand("DELETE FROM [User]", conn).ExecuteNonQuery();
        }

        public void Dispose()
        {
            var masterConnectionString =
                "Server=.\\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(masterConnectionString);
            conn.Open();

            using var cmd = new SqlCommand($@"
IF DB_ID('{databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidUser_ReturnsNewId()
        {
            var user = new User();

            int id = repo.Insert(user);

            Assert.True(id > 0);

            var insertedUser = repo.GetById(id);
            Assert.NotNull(insertedUser);
            Assert.Equal(id, insertedUser!.UserId);
        }

        [Fact]
        public void GetAll_WhenUsersExist_ReturnsUsers()
        {
            repo.Insert(new User());
            repo.Insert(new User());

            var users = repo.GetAll();

            Assert.Equal(2, users.Count);
        }

        [Fact]
        public void GetById_ExistingUser_ReturnsUser()
        {
            var user = new User();

            int id = repo.Insert(user);

            var result = repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.UserId);
        }

        [Fact]
        public void GetById_NonExistingUser_ReturnsNull()
        {
            var result = repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingUser_ThrowsSqlException()
        {
            var user = new User();

            int id = repo.Insert(user);
            user.UserId = id;

            Assert.Throws<SqlException>(() => repo.Update(user));
        }

        [Fact]
        public void Update_NonExistingUser_ThrowsSqlException()
        {
            var user = new User
            {
                UserId = 999999
            };

            Assert.Throws<SqlException>(() => repo.Update(user));
        }

        [Fact]
        public void Delete_ExistingUser_ReturnsTrueAndRemovesUser()
        {
            var user = new User();

            int id = repo.Insert(user);

            bool deleted = repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingUser_ReturnsFalse()
        {
            bool deleted = repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}