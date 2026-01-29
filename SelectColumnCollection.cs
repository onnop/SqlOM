using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Reeb.SqlOM
{
    /// <summary>
    /// A collection of elements of type SelectColumn
    /// </summary>
    public class SelectColumnCollection : CollectionBase
    {
        /// <summary>
        /// Initializes a new empty instance of the SelectColumnCollection class.
        /// </summary>
        public SelectColumnCollection()
        {
            // empty
        }

        /// <summary>
        /// Initializes a new instance of the SelectColumnCollection class, containing elements
        /// copied from an array.
        /// </summary>
        /// <param name="items">
        /// The array whose elements are to be added to the new SelectColumnCollection.
        /// </param>
        public SelectColumnCollection(SelectColumn[] items)
        {
            this.AddRange(items);
        }

        /// <summary>
        /// Initializes a new instance of the SelectColumnCollection class, containing elements
        /// copied from another instance of SelectColumnCollection
        /// </summary>
        /// <param name="items">
        /// The SelectColumnCollection whose elements are to be added to the new SelectColumnCollection.
        /// </param>
        public SelectColumnCollection(SelectColumnCollection items)
        {
            this.AddRange(items);
        }

        /// <summary>
        /// Adds the elements of an array to the end of this SelectColumnCollection.
        /// </summary>
        /// <param name="items">
        /// The array whose elements are to be added to the end of this SelectColumnCollection.
        /// </param>
        public virtual void AddRange(SelectColumn[] items)
        {
            foreach (SelectColumn item in items)
            {
                this.List.Add(item);
            }
        }

        /// <summary>
        /// Adds the elements of another SelectColumnCollection to the end of this SelectColumnCollection.
        /// </summary>
        /// <param name="items">
        /// The SelectColumnCollection whose elements are to be added to the end of this SelectColumnCollection.
        /// </param>
        public virtual void AddRange(SelectColumnCollection items)
        {
            foreach (SelectColumn item in items)
            {
                this.List.Add(item);
            }
        }

        /// <summary>
        /// Adds an instance of type SelectColumn to the end of this SelectColumnCollection.
        /// </summary>
        /// <param name="value">
        /// The SelectColumn to be added to the end of this SelectColumnCollection.
        /// </param>
        public virtual void Add(SelectColumn value)
        {
            this.List.Add(value);
        }

        /// <summary>
        /// Adds a column using a property expression.
        /// Auto-aliases when [ColumnName] differs from property name (for Dapper mapping).
        /// </summary>
        public void Add<T>(Expression<Func<T, object?>> expression, FromTerm table)
        {
            var propertyInfo = GetPropertyInfo(expression);
            var columnAttr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
            var columnName = columnAttr?.Name ?? propertyInfo.Name;

            if (columnAttr != null && columnAttr.Name != propertyInfo.Name)
            {
                this.Add(new SelectColumn(columnName, table, propertyInfo.Name));
            }
            else
            {
                this.Add(new SelectColumn(columnName, table));
            }
        }

        /// <summary>
        /// Adds a column with an explicit alias (overrides auto-alias).
        /// </summary>
        public void Add<T>(Expression<Func<T, object?>> expression, FromTerm table, string alias)
        {
            var propertyInfo = GetPropertyInfo(expression);
            var columnAttr = propertyInfo.GetCustomAttribute<ColumnNameAttribute>();
            var columnName = columnAttr?.Name ?? propertyInfo.Name;
            this.Add(new SelectColumn(columnName, table, alias));
        }

        /// <summary>
        /// Adds all non-ignored properties from a type as columns.
        /// Automatically ignores: [IgnoreColumn], [NotMapped], navigation properties.
        /// </summary>
        public void AddAllColumns<T>(FromTerm table)
        {
            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !ShouldIgnoreProperty(p))
                .GroupBy(p => p.Name)
                .Select(g => g.First());

            foreach (var prop in properties)
            {
                var columnAttr = prop.GetCustomAttribute<ColumnNameAttribute>();
                var columnName = columnAttr?.Name ?? prop.Name;
                this.Add(new SelectColumn(columnName, table));
            }
        }

        private static PropertyInfo GetPropertyInfo<T>(Expression<Func<T, object?>> expression)
        {
            MemberExpression? member = expression.Body as MemberExpression;
            if (member == null && expression.Body is UnaryExpression unary)
            {
                member = unary.Operand as MemberExpression;
            }
            return member?.Member as PropertyInfo ?? throw new ArgumentException("Expression must be a property access");
        }

        private static bool ShouldIgnoreProperty(PropertyInfo prop)
        {
            if (prop.GetCustomAttribute<IgnoreColumnAttribute>() != null) return true;
            if (prop.CustomAttributes.Any(a => a.AttributeType.Name == "NotMappedAttribute")) return true;
            return IsNavigationProperty(prop.PropertyType);
        }

        private static bool IsNavigationProperty(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type);
            if (underlyingType != null) type = underlyingType;
            if (type.IsPrimitive || type.IsEnum || type.IsValueType) return false;
            if (type == typeof(string) || type == typeof(byte[])) return false;
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset) ||
                type == typeof(TimeSpan) || type == typeof(Guid) || type == typeof(decimal)) return false;
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string)) return true;
            return type.IsClass;
        }

        /// <summary>
        /// Determines whether a specfic SelectColumn value is in this SelectColumnCollection.
        /// </summary>
        /// <param name="value">
        /// The SelectColumn value to locate in this SelectColumnCollection.
        /// </param>
        /// <returns>
        /// true if value is found in this SelectColumnCollection;
        /// false otherwise.
        /// </returns>
        public virtual bool Contains(SelectColumn value)
        {
            return this.List.Contains(value);
        }

        /// <summary>
        /// Return the zero-based index of the first occurrence of a specific value
        /// in this SelectColumnCollection
        /// </summary>
        /// <param name="value">
        /// The SelectColumn value to locate in the SelectColumnCollection.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of the _ELEMENT value if found;
        /// -1 otherwise.
        /// </returns>
        public virtual int IndexOf(SelectColumn value)
        {
            return this.List.IndexOf(value);
        }

        /// <summary>
        /// Inserts an element into the SelectColumnCollection at the specified index
        /// </summary>
        /// <param name="index">
        /// The index at which the SelectColumn is to be inserted.
        /// </param>
        /// <param name="value">
        /// The SelectColumn to insert.
        /// </param>
        public virtual void Insert(int index, SelectColumn value)
        {
            this.List.Insert(index, value);
        }

        /// <summary>
        /// Gets or sets the SelectColumn at the given index in this SelectColumnCollection.
        /// </summary>
        public virtual SelectColumn this[int index]
        {
            get
            {
                return (SelectColumn)this.List[index];
            }
            set
            {
                this.List[index] = value;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific SelectColumn from this SelectColumnCollection.
        /// </summary>
        /// <param name="value">
        /// The SelectColumn value to remove from this SelectColumnCollection.
        /// </param>
        public virtual void Remove(SelectColumn value)
        {
            this.List.Remove(value);
        }

        /// <summary>
        /// Type-specific enumeration class, used by SelectColumnCollection.GetEnumerator.
        /// </summary>
        public class Enumerator : IEnumerator
        {
            private IEnumerator wrapped;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="collection"></param>
            public Enumerator(SelectColumnCollection collection)
            {
                this.wrapped = ((CollectionBase)collection).GetEnumerator();
            }
            /// <summary>
            /// 
            /// </summary>
            public SelectColumn Current
            {
                get
                {
                    return (SelectColumn)(this.wrapped.Current);
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    return (SelectColumn)(this.wrapped.Current);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public bool MoveNext()
            {
                return this.wrapped.MoveNext();
            }

            /// <summary>
            /// 
            /// </summary>
            public void Reset()
            {
                this.wrapped.Reset();
            }
        }

        /// <summary>
        /// Returns an enumerator that can iterate through the elements of this SelectColumnCollection.
        /// </summary>
        /// <returns>
        /// An object that implements System.Collections.IEnumerator.
        /// </returns>        
        public new virtual Enumerator GetEnumerator()
        {
            return new Enumerator(this);
        }
    }
}
