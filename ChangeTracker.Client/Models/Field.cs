using System;
using Newtonsoft.Json;

namespace ChangeTracker.Client.Models
{
    public class Field
    {
        [JsonProperty("f")] public string Name { get; set; }
        [JsonProperty("p")] public string PrevValue { get; set; }
        [JsonProperty("n")] public string NextValue { get; set; }
        private Type _type;
        private string _format;

        public void SetFieldType(Type type)
        {
            _type = type;
        }

        public Type GetFieldType()
        {
            return _type;
        }

        public void SetFieldFormat(string format)
        {
            _format = format;
        }

        public string GetFieldFormat()
        {
            return _format;
        }

        public override string ToString()
        {
            return Describe(RowStatus.Unknown);
        }

        public string Describe(string status)
        {
            if (string.Equals(PrevValue, NextValue, StringComparison.InvariantCultureIgnoreCase))
                return Name + "=(" + PrevValue + ")";

            switch (status)
            {
                case RowStatus.New:
                    return Name + "=(" + (NextValue ?? "") + ")";

                case RowStatus.Deleted:
                    return Name + "=(" + (PrevValue ?? "") + ")";
            }

            // modified
            return Name + "=(" + (PrevValue ?? "") + " => " + (NextValue ?? "") + ")";
        }
    }
}