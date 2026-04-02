using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IUserBadgeRepository
{
    public List<UserBadge> GetAll();
    public UserBadge? GetById(int userId, int badgeId);
    public bool Insert(UserBadge userBadge);
    public bool Update(UserBadge userBadge);
    public bool Delete(int userId, int badgeId);
}