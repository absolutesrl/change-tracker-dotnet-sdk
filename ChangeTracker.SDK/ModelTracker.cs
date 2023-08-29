using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ChangeTracker.SDK.Core;
using ChangeTracker.SDK.Models;

namespace ChangeTracker.SDK
{
    public static class ModelTracker
    {
        public static ModelMapper<TType> CreateMap<TType>(TType model)
        {
            return new ModelMapper<TType>(model);
        }

        public static ModelMapper<TType> MapAll<TType>(TType model)
        {
            var map = CreateMap(model);

            map = map.MapAll();

            return map;
        }

        public static ModelMapper<TType> Map<TType, TField>(TType model, Expression<Func<TType, TField>> func,
            string fieldName = null, string format = null)
        {
            var map = CreateMap(model);

            map = map.Map(func, fieldName, format);

            return map;
        }

        public static ModelMapper<TType> Map<TType>(TType model, string mapping,
            string fieldName = null, string format = null)
        {
            var map = CreateMap(model);

            map = map.Map(mapping, fieldName, format);

            return map;
        }
    
        public static Table ToTableModel(this IEnumerable<Row> rowModels, string tableName)
        {
            var res = new Table
            {
                Name = tableName,
                Rows = rowModels?.ToList() ?? new List<Row>()
            };

            return res;
        }

        public class ModelMapper<TType>
        {
            private TType Model { get; set; }
            private Dictionary<string, LambdaExpression> Fields { get; set; }
            private Dictionary<string, string> FieldFormats { get; set; }

            public ModelMapper(TType model)
            {
                if (model == null) throw new ArgumentNullException("model");

                Model = model;
                Fields = new Dictionary<string, LambdaExpression>();
                FieldFormats = new Dictionary<string, string>();
            }

            public ModelMapper<TType> MapAll()
            {
                var type = Model.GetType();

                var properties =
                    type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                foreach (var property in properties)
                {
                    var attribute = GetModelTrackerAttibute(property);
                    var propertyName = property.Name;

                    if (attribute != null)
                    {
                        if (attribute.Ignore) continue;
                        propertyName = string.IsNullOrEmpty(attribute.Name) ? propertyName : attribute.Name;

                        if (!string.IsNullOrEmpty(attribute.Format))
                            if (FieldFormats.ContainsKey(propertyName))
                                FieldFormats[propertyName] = attribute.Format;
                            else
                                FieldFormats.Add(propertyName, attribute.Format);

                        if (ParseAttributeMapping(attribute, propertyName)) continue;
                    }

                    if (!property.PropertyType.IsSimpleType()) continue;

                    if (Fields.ContainsKey(propertyName))
                        Fields[propertyName] = CreateGenerator(Model, property);
                    else
                        Fields.Add(propertyName, CreateGenerator(Model, property));
                }

                return this;
            }

            private bool ParseAttributeMapping(ModelTrackerAttribute attribute, string name)
            {
                //Nel caso in cui il mapping sia vuoto provo a recuperare il dato partendo dalla property
                //Se il mapping è presente ma errato escludo il campo dai risultati
                if (attribute == null || attribute.Ignore || string.IsNullOrEmpty(attribute.Mapping)) return false;

                return ParseMapping(attribute.Mapping, name);
            }

            private bool ParseMapping(string mapping, string name)
            {
                var split = mapping.Split(new[] {'.'}, StringSplitOptions.RemoveEmptyEntries);

                if (split.Length == 0) return true;

                var propertyType = typeof(TType);

                var parameter = Expression.Parameter(typeof(TType), "x");
                Expression result = parameter;
                var checkNullProps = new List<Expression>();

                foreach (var token in split)
                {
                    var checkNull = token.IndexOf('?') > 0;
                    var propertyName = token.Trim('?');
                    var propertyInfo = propertyType.GetProperty(propertyName);

                    if (propertyInfo == null)
                    {
                        return true;
                        /*throw new ArgumentException(
                            $"Cannot find the property {propertyName} in the class {propertyType.Name}. Please check the Mapping \"{attribute.Mapping}\" defined in the ModelTrackerAttribute on {property?.Name} property");*/
                    }

                    propertyType = propertyInfo.PropertyType;

                    result = Expression.Property(result, propertyName);

                    if (checkNull)
                        checkNullProps.Insert(0, result);
                }

                //la property finale del mapping dev'essere di tipo primitivo
                if (!IsSimpleType(propertyType))
                    return true;

                if (result.Type.IsValueType && Nullable.GetUnderlyingType(result.Type) == null)
                    result = Expression.Convert(result, typeof(Nullable<>).MakeGenericType(result.Type));

                var nullResult = Expression.Constant(null, result.Type);

                //secondo parse per costruire le null conditions
                foreach (var prop in checkNullProps)
                {
                    result = Expression.Condition(
                        Expression.NotEqual(prop, Expression.Constant(null)),
                        result, nullResult);
                }

                var lambda = Expression.Lambda(result, parameter);

                if (Fields.ContainsKey(name))
                    Fields[name] = lambda;
                else
                    Fields.Add(name, lambda);

                return true;
            }

            public ModelMapper<TType> Map<TField>(Expression<Func<TType, TField>> func,
                string fieldName = null, string format = null)
            {
                fieldName = fieldName ?? FieldMapper.GetName(func);

                if (Fields.ContainsKey(fieldName))
                    Fields[fieldName] = func;
                else
                    Fields.Add(fieldName, func);

                if (string.IsNullOrEmpty(format)) return this;

                if (FieldFormats.ContainsKey(fieldName))
                    FieldFormats[fieldName] = format;
                else
                    FieldFormats.Add(fieldName, format);

                return this;
            }

            public ModelMapper<TType> Map(string mapping, string fieldName = null, string format = null)
            {
                if (string.IsNullOrEmpty(mapping)) return this;

                fieldName = fieldName ?? mapping.Replace(".", "").Replace("?", "");

                ParseMapping(mapping, fieldName);

                if (string.IsNullOrEmpty(format)) return this;

                if (FieldFormats.ContainsKey(fieldName))
                    FieldFormats[fieldName] = format;
                else
                    FieldFormats.Add(fieldName, format);

                return this;
            }

            public ModelMapper<TType> Ignore<TField>(Expression<Func<TType, TField>> func)
            {
                var fieldName = FieldMapper.GetName(func);

                Ignore(fieldName);

                return this;
            }

            public ModelMapper<TType> Ignore(string fieldName)
            {
                if (string.IsNullOrEmpty(fieldName)) throw new ArgumentNullException("fieldName");

                if (Fields.ContainsKey(fieldName))
                    Fields.Remove(fieldName);

                return this;
            }

            public List<Field> ToList()
            {
                var res = Fields.Select(el =>
                {
                    string value = null;
                    var fieldType = typeof(string);
                    var fieldFormat = FieldFormats.ContainsKey(el.Key) ? FieldFormats[el.Key] : null;

                    try
                    {
                        var exprValue = el.Value.Compile().DynamicInvoke(Model);
                        fieldType = exprValue?.GetType() ?? fieldType;
                        value = FieldMapper.ConvertValue(exprValue, fieldFormat);
                    }
                    catch (Exception)
                    {
                        value = string.Empty;
                    }

                    var fieldModel = new Field {Name = el.Key, PrevValue = value};

                    fieldModel.SetFieldType(fieldType);
                    fieldModel.SetFieldFormat(fieldFormat);
                    
                    return fieldModel;
                }).ToList();

                return res;
            }

            public Row ToRowModel(string rowKey, List<Table> linkedTables = null)
            {
                var res = new Row
                {
                    Key = rowKey,
                    Fields = ToList()
                };

                if (linkedTables != null && linkedTables.Any())
                    res.Tables = linkedTables;

                return res;
            }
        }

        private static bool IsSimpleType(this Type type)
        {
            return type.IsPrimitive ||
                   type.IsEnum ||
                   type == typeof(string) ||
                   type == typeof(decimal) ||
                   type == typeof(DateTime) ||
                   type == typeof(DateTimeOffset) ||
                   type == typeof(TimeSpan) ||
                   type == typeof(Guid) ||
                   IsNullableSimpleType(type);

            bool IsNullableSimpleType(Type t)
            {
                var underlyingType = Nullable.GetUnderlyingType(t);
                return underlyingType != null && IsSimpleType(underlyingType);
            }
        }

        private static LambdaExpression CreateGenerator<TType>(TType model, PropertyInfo propertyInfo)
        {
            var parameter = Expression.Parameter(model.GetType(), "x");
            var property = Expression.Property(parameter, propertyInfo);

            var funcType = Expression.GetFuncType(typeof(TType), propertyInfo.PropertyType);

            return Expression.Lambda(funcType, property, parameter);
        }

        private static ModelTrackerAttribute GetModelTrackerAttibute(PropertyInfo property)
        {
            return property.GetCustomAttributes(typeof(ModelTrackerAttribute))
                .Select(el => (ModelTrackerAttribute) el).FirstOrDefault();
        }
    }

    /// <summary>
    /// Use <c>ModelTrackerAttribute</c> to define specific tracker mapping on a property.
    /// You can use <c>Mapping</c> property to navigate through nested objects using a dot separated punctuation (i.e.: "SomeProp.SomeSubProp").
    /// Using <c>Name</c> prop you can define a specific prop name on the tracking model
    /// Use <c>Ignore</c> to exclude the property from the tracking model
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ModelTrackerAttribute : Attribute
    {
        public string Name { get; set; }
        public string Mapping { get; set; }
        public bool Ignore { get; set; }
        public string Format { get; set; }
    }
}