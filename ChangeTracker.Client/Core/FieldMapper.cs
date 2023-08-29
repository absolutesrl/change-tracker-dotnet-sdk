using System;
using System.Globalization;
using System.Linq.Expressions;
using ChangeTracker.Client.Models;

namespace ChangeTracker.Client.Core
{
    public static class FieldMapper
    {
        public static Field Map<TType, TField>(TType model, Expression<Func<TType, TField>> func,
            string fieldName = null, string format = null)
        {
            try
            {
                fieldName = fieldName ?? GetName(func);
            }
            catch (Exception e)
            {
                fieldName = "unknown";
            }

            string value = "";
            var fieldType = typeof(string);
            try
            {
                var dataValue = func.Compile().Invoke(model);
                fieldType = dataValue?.GetType() ?? fieldType;
                value = ConvertValue(dataValue, format);
            }
            catch (Exception e)
            {
            }

            var ret = new Field { Name = fieldName, PrevValue = value };

            ret.SetFieldType(fieldType);
            ret.SetFieldFormat(format);

            return ret;
        }

        public static string GetName<TSource, TField>(Expression<Func<TSource, TField>> field)
        {
            if (Equals(field, null))
            {
                throw new NullReferenceException("Field is required");
            }

            MemberExpression expr = null;

            if (field.Body is MemberExpression)
            {
                expr = (MemberExpression)field.Body;
            }
            else if (field.Body is UnaryExpression)
            {
                expr = (MemberExpression)((UnaryExpression)field.Body).Operand;
            }
            else
            {
                const string Format = "Expression '{0}' not supported.";
                string message = string.Format(Format, field);

                throw new ArgumentException(message, "Field");
            }

            return expr.Member.Name;
        }

        //in caso di format non presente converto i campi a default format ("G" per i numeri decimali)
        public static string ConvertValue<TType>(TType value, string format = null)
        {
            if (value == null) return string.Empty;

            string res = null;
            try
            {
                switch (value)
                {
                    case DateTime dateValue:
                        res = dateValue.ToString("o");
                        break;

                    case bool boolValue:
                        res = boolValue.ToString().ToLower(CultureInfo.InvariantCulture);
                        break;

                    case decimal decimalValue:
                        res = decimalValue.ToString(format ?? "G", CultureInfo.InvariantCulture);
                        break;

                    case float floatValue:
                        res = floatValue.ToString(format ?? "G", CultureInfo.InvariantCulture);
                        break;

                    case double doubleValue:
                        res = doubleValue.ToString(format ?? "G", CultureInfo.InvariantCulture);
                        break;
                }
            }
            catch (Exception)
            {
                if (format == null) throw;

                res = ConvertValue(value);
            }

            if (res == null)
                res = value.ToString();

            return res ?? string.Empty;
        }
    }
}
