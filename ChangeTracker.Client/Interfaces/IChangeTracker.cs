using ChangeTracker.Client.Models;

namespace ChangeTracker.Client.Interfaces
{
    public interface IChangeTrackerClient
    {
        void SetPreviousVersion(Row prev);
        void SetNextVersion(Row next);
        Table Diff(string tableName, string userName, string rowDescription, string ipAddress = "");
    }
}