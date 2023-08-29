using System;
using System.Linq;
using ChangeTracker.SDK.Interfaces;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK.Core
{
    public class StandardChangeCalculator : IChangeCalculator
    {
        public Row Diff(Row prev, Row next)
        {
            var diff = new Row();

            if (prev != null) diff.Key = prev.Key;
            if (prev == null && next != null) diff.Key = next.Key;
            if (prev == null && next == null) return null;

            if (next == null) diff.State = RowStatus.Deleted;
            if (prev == null) diff.State = RowStatus.New;

            if (prev != null)
                foreach (var field in prev.Fields)
                {
                    if (diff.State == RowStatus.Deleted &&
                        field.PrevValue == GetDefault(field)) continue;

                    var diffField = new Field { Name = field.Name, PrevValue = field.PrevValue };
                    diff.Fields.Add(diffField);
                }

            if (next != null)
            {
                foreach (var field in next.Fields)
                {
                    if (diff.State == RowStatus.New && field.PrevValue == GetDefault(field)) continue;

                    var diffField = diff.Fields.SingleOrDefault(el =>
                        string.Equals(el.Name, field.Name, StringComparison.InvariantCultureIgnoreCase));

                    if (diffField == null)
                    {
                        diffField = new Field { Name = field.Name, NextValue = field.PrevValue };
                        diff.Fields.Add(diffField);
                    }
                    else
                        diffField.NextValue = field.PrevValue;
                }
            }

            // Prende solo quelli differenti
            diff.Fields = diff.Fields.Where(el =>
                !string.Equals(el.PrevValue, el.NextValue, StringComparison.InvariantCultureIgnoreCase)).ToList();

            if (string.IsNullOrEmpty(diff.State))
                diff.State = diff.Fields.Any()
                    ? RowStatus.Modified
                    : RowStatus.Unchanged;

            /*
            switch (diff.State)
            {
                case RowStatus.New:
                    diff.Fields = diff.Fields.Where(el => el.NextValue != GetDefault(el.GetFieldType())?.ToString())
                        .ToList();
                    break;

                case RowStatus.Deleted:
                    diff.Fields = diff.Fields.Where(el => el.PrevValue != GetDefault(el.GetFieldType())?.ToString())
                        .ToList();
                    break;
            }*/

            if (prev?.Tables != null && prev.Tables.Any())
                foreach (var table in prev.Tables)
                {
                    var addedTable = new Table { Name = table.Name };
                    diff.Tables.Add(addedTable);

                    foreach (var row in table.Rows)
                    {
                        var nextRow = next?
                            .Tables?.SingleOrDefault(el => string.Equals(el.Name, table.Name,
                                StringComparison.InvariantCultureIgnoreCase))?
                            .Rows?.SingleOrDefault(el => el.Key == row.Key);

                        var diffRow = Diff(row, nextRow);
                        if (diffRow != null && diffRow.IsFull()) addedTable.Rows.Add(diffRow);
                    }
                }

            if (next?.Tables != null && next.Tables.Any())
                foreach (var table in next.Tables)
                {
                    var addedTable = diff.Tables.SingleOrDefault(el => el.Name == table.Name);
                    if (addedTable == null)
                    {
                        addedTable = new Table { Name = table.Name };
                        diff.Tables.Add(addedTable);
                    }

                    foreach (var row in table.Rows)
                    {
                        var prevRow = prev?
                            .Tables?.SingleOrDefault(el => string.Equals(el.Name, table.Name,
                                StringComparison.InvariantCultureIgnoreCase))?
                            .Rows?.SingleOrDefault(el => el.Key == row.Key);

                        var diffRow = Diff(prevRow, row);
                        var alreadyRow = addedTable.Rows.SingleOrDefault(el => el.Key == row.Key);

                        if (alreadyRow == null && diffRow != null && diffRow.IsFull())
                            addedTable.Rows.Add(diffRow);
                    }
                }

            diff.Tables = diff.Tables.Where(el => el.Rows.Any()).ToList();

            return diff;
        }

        private string GetDefault(Field field)
        {
            var type = field.GetFieldType();
            var format = field.GetFieldFormat();

            if (type != null && type.IsValueType)
                return FieldMapper.ConvertValue(Activator.CreateInstance(type), format);

            return string.Empty;
        }
    }
}