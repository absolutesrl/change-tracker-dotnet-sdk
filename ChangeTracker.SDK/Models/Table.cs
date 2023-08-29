using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ChangeTracker.SDK.Models
{
    public class Table
    {
        [JsonProperty("odt")]
        public DateTime OperationDateTime { get; set; }

        public string Name { get; set; }
        public string Ip { get; set; }
        public string User { get; set; }

        public List<Row> Rows { get; set; }

        public Table()
        {
            Rows = new List<Row>();
        }

        public static Table CreateTable(List<Row> rows, string tableName, string userName, string ipAddress)
        {
            if (rows == null)
                return null;

            var model = new Table
            {
                Name = tableName,
                User = userName,
                OperationDateTime = DateTime.UtcNow,
                Ip = ipAddress,
                Rows = rows

            };
            
            return model;
        }
    }
}