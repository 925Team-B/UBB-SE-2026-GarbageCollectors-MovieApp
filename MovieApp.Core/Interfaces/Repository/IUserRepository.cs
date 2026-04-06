using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IUserRepository
{
    public List<User> GetAll();
    public User? GetById(int id);
    public int Insert(User user);
    public bool Update(User user);
    public bool Delete(int id);
}