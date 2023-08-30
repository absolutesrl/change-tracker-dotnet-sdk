using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK.Interfaces
{
    public interface IChangeTrackerService
    {
        StoreChangesResult Store(string tableName, string userName, string rowDescription, Row prev, Row next,
            string ipAddress = "");

        string GetToken(string tableName, string rowKey = null);
    }
}