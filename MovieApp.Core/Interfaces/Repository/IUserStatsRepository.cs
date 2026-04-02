using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IUserStatsRepository
{
    public List<UserStats> GetAll();
    public UserStats? GetById(int id);
    public UserStats? GetByUserId(int userId);
    public int Insert(UserStats userStats);
    public bool Update(UserStats userStats);
    public bool Delete(int id);
    

}