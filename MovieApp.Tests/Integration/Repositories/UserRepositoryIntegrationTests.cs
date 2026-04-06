using Microsoft.Data.SqlClient;
using MovieApp.Core.Models;
using MovieApp.Core.Repositories;
using System;
using Xunit;

namespace MovieApp.Tests.Integration.Repositories
{
    public class UserRepositoryIntegrationTests : IDisposable
    {
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly UserRepository _repo;

        public UserRepositoryIntegrationTests()
        {
            _databaseName = "MovieAppTestDb_User_" + Guid.NewGuid().ToString("N");

            _connectionString =
                $"Server=.\\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True;";

            var initializer = new DatabaseInitializer(_connectionString);
            initializer.EnsureCreatedAndSeeded();

            _repo = new UserRepository(_connectionString);
            ClearTables();
        }

        private void ClearTables()
        {
            using var conn = new SqlConnection(_connectionString);
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
IF DB_ID('{_databaseName}') IS NOT NULL
BEGIN
    ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [{_databaseName}];
END", conn);

            cmd.ExecuteNonQuery();
        }

        [Fact]
        public void Insert_ValidUser_ReturnsNewId()
        {
            var user = new User();

            int id = _repo.Insert(user);

            Assert.True(id > 0);

            var insertedUser = _repo.GetById(id);
            Assert.NotNull(insertedUser);
            Assert.Equal(id, insertedUser!.UserId);
        }

        [Fact]
        public void GetAll_WhenUsersExist_ReturnsUsers()
        {
            _repo.Insert(new User());
            _repo.Insert(new User());

            var users = _repo.GetAll();

            Assert.Equal(2, users.Count);
        }

        [Fact]
        public void GetById_ExistingUser_ReturnsUser()
        {
            var user = new User();

            int id = _repo.Insert(user);

            var result = _repo.GetById(id);

            Assert.NotNull(result);
            Assert.Equal(id, result!.UserId);
        }

        [Fact]
        public void GetById_NonExistingUser_ReturnsNull()
        {
            var result = _repo.GetById(999999);

            Assert.Null(result);
        }

        [Fact]
        public void Update_ExistingUser_ThrowsSqlException()
        {
            var user = new User();

            int id = _repo.Insert(user);
            user.UserId = id;

            Assert.Throws<SqlException>(() => _repo.Update(user));
        }

        [Fact]
        public void Update_NonExistingUser_ThrowsSqlException()
        {
            var user = new User
            {
                UserId = 999999
            };

            Assert.Throws<SqlException>(() => _repo.Update(user));
        }

        [Fact]
        public void Delete_ExistingUser_ReturnsTrueAndRemovesUser()
        {
            var user = new User();

            int id = _repo.Insert(user);

            bool deleted = _repo.Delete(id);

            Assert.True(deleted);
            Assert.Null(_repo.GetById(id));
        }

        [Fact]
        public void Delete_NonExistingUser_ReturnsFalse()
        {
            bool deleted = _repo.Delete(999999);

            Assert.False(deleted);
        }
    }
}