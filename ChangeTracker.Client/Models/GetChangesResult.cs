using System.Collections.Generic;

namespace ChangeTracker.Client.Models
{
    public class GetChangesResult
    {
        public bool Ok { get; set; }
        public string ErrorText { get; set; }

        public string PaginationToken { get; set; }
        public List<Table> Changes { get; set; }

        public GetChangesResult()
        {
            Changes = new List<Table>();
        }
    }

    public class StoreChangesResult
    {
        public bool Ok { get; set; }
        public string ErrorText { get; set; }
    }
}
