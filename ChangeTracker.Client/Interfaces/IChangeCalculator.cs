using ChangeTracker.Client.Models;

namespace ChangeTracker.Client.Interfaces
{
    public interface IChangeCalculator
    {
        Row Diff(Row prev, Row next);
    }
}