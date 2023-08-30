using ChangeTracker.SDK.Interfaces;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK
{
    public class ChangeTrackerService : IChangeTrackerService
    {
        private readonly ChangeTrackerGateway _changeTrackerGateway;

        public ChangeTrackerService(string changeTrackerAccountName, string apiSecretGet, string apiSecretPost,
            int tokenMinuteDuration = 5)
        {
            _changeTrackerGateway = new ChangeTrackerGateway(
                changeTrackerAccountName,
                apiSecretGet, apiSecretPost, tokenMinuteDuration
            );
        }
        public StoreChangesResult Store(string tableName, string userName, string rowDescription, Row prev, Row next,
            string ipAddress = "")
        {
            return _changeTrackerGateway.StoreChangesAsync(
                tableName,
                userName,
                rowDescription,
                prev,
                next,
                ipAddress).Result;
        }

        public string GetToken(string tableName, string rowKey = null)
        {
            return _changeTrackerGateway.GetToken(tableName, rowKey);
        }
    }
}
