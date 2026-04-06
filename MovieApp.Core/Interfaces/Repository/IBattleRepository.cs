using MovieApp.Core.Models;

namespace MovieApp.Core.Interfaces.Repository;

public interface IBattleRepository
{
    public List<Battle> GetAll();
    public Battle? GetById(int id);
    public int Insert(Battle battle);
    public bool Update(Battle battle);
    public bool Delete(int id);
}