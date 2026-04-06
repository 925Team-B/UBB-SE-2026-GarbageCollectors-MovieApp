using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IBadgeRepository
{
    public List<Badge> GetAll();
    public Badge? GetById(int id);
    public int Insert(Badge badge);
    public bool Update(Badge badge);
    public bool Delete(int id);
}