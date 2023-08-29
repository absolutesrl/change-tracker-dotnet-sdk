using System.Collections.Generic;
using System.Linq;

namespace ChangeTracker.Client.Models
{
    public class Row
    {
        public string State { get; set; }

        public string Key { get; set; }

        public string Desc { get; set; }

        public List<Field> Fields { get; set; }

        public List<Table> Tables { get; set; }

        public Row()
        {
            Fields = new List<Field>();
            Tables = new List<Table>();
        }
        public bool IsFull()
        {
            var ret = Fields.Any() || Tables.Any(table => table.Rows.Any(row => row.IsFull()));

            return ret;
        }
    }
}