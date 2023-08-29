using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ChangeTracker.SDK.Interfaces;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK.Core
{
    public class LocalChangeStorage : IChangeStorage
    {
        private List<LocalChangeModel> _changes = new List<LocalChangeModel>();
        private object _lockObject = new object();

        public Task<StoreChangesResult> StoreChangesAsync(string token, Table table)
        {
            if (table == null || !table.Rows.Any()) return null;

            lock (_lockObject)
            {
                var row = table.Rows[0];

                _changes.Add(new LocalChangeModel
                {
                    TableName = table.Name,
                    RowKey = row.Key,
                    OperatingDate = table.OperationDateTime,
                    Model = table
                });
            }

            return null;
        }

        public async Task<GetChangesResult> GetChangesAsync(string token, string tableName, string rowKey = "", string paginationToken = "", bool backwardSearch = true)
        {
            List<Table> ret;

            lock (_lockObject)
            {
                ret = _changes
                    .Where(el =>
                        el.TableName == tableName &&
                        (string.IsNullOrEmpty(rowKey) || el.RowKey != null && el.RowKey.StartsWith(rowKey)))
                    .Select(el => el.Model).ToList();
            }

            return new GetChangesResult
            {
                PaginationToken = "",
                Changes = ret
            };
        }

        private class LocalChangeModel
        {
            public string TableName { get; set; }
            public string RowKey { get; set; }
            public DateTime OperatingDate { get; set; }
            public Table Model { get; set; }
        }
    }
}