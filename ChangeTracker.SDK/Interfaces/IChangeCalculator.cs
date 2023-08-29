using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK.Interfaces
{
    public interface IChangeCalculator
    {
        Row Diff(Row prev, Row next);
    }
}