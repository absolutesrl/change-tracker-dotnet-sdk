using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ChangeTracker.Client.Core;
using ChangeTracker.Client.Interfaces;
using ChangeTracker.Client.Models;

namespace ChangeTracker.Client
{
    internal class ChangeTrackerGateway
    {
        private readonly string _apiSecretGet;
        private readonly string _apiSecretPost;
        private readonly int _tokenMinuteDuration;
        public IChangeCalculator ChangeCalculator { get; set; }
        public IChangeStorage ChangeStorage { get; set; }

        public ChangeTrackerGateway(string changeTrackerAccountName, string apiSecretGet, string apiSecretPost,
            int tokenMinuteDuration)
        {
            _apiSecretGet = apiSecretGet;
            _apiSecretPost = apiSecretPost;
            _tokenMinuteDuration = tokenMinuteDuration;

            ChangeCalculator = new StandardChangeCalculator();
            ChangeStorage = new RemoteChangeStorage($"https://{changeTrackerAccountName}.hosts.changetracker.it");
        }

        public async Task<StoreChangesResult> StoreChangesAsync(string tableName, string userName,
            string rowDescription, Row prev, Row next, string ipAddress = "")
        {
            if (prev == null && next == null) return null;

            var token = TokenGenerator.GenerateToken(_apiSecretPost, tableName, duration: _tokenMinuteDuration);

            var row = ChangeCalculator.Diff(prev, next);

            if (row == null)
                return new StoreChangesResult
                    { Ok = false, ErrorText = "ChangeTracker, diff: missing or invalid diff models" };
            
            if (row.IsFull()) return new StoreChangesResult { Ok = true };

            var table = Table.CreateTable(new List<Row> { row }, tableName, userName, ipAddress);

            if (table == null)
                return new StoreChangesResult
                    { Ok = false, ErrorText = "ChangeTracker, createTable: invalid rows model" };

            return await ChangeStorage.StoreChangesAsync(token, table);
        }

        public string GetToken(string tableName, string rowKey)
        {
            return TokenGenerator.GenerateToken(_apiSecretGet, tableName, rowKey, _tokenMinuteDuration);
        }
    }
}