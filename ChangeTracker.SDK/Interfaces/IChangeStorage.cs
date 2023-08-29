using System.Threading.Tasks;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK.Interfaces
{
    public interface IChangeStorage
    {
        Task<StoreChangesResult> StoreChangesAsync(string token, Table table);
        Task<GetChangesResult> GetChangesAsync(string token, string tableName, string rowKey = "", string paginationToken = "", bool backwardSearch = true);
    }
}