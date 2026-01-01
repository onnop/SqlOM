namespace Reeb.SqlOM
{
    /// <summary>
    /// A collection of elements of type InsertQuery
    /// </summary>
    public class InsertQueryCollection : System.Collections.CollectionBase
    {
        /// <summary>
        /// Initializes a new empty instance of the InsertQueryCollection class.
        /// </summary>
        public InsertQueryCollection()
        {
            // empty
        }

        /// <summary>
        /// Initializes a new instance of the InsertQueryCollection class, containing elements
        /// copied from an array.
        /// </summary>
        /// <param name="items">
        /// The array whose elements are to be added to the new InsertQueryCollection.
        /// </param>
        public InsertQueryCollection(InsertQuery[] items)
        {
            this.AddRange(items);
        }

        /// <summary>
        /// Initializes a new instance of the InsertQueryCollection class, containing elements
        /// copied from another instance of InsertQueryCollection
        /// </summary>
        /// <param name="items">
        /// The InsertQueryCollection whose elements are to be added to the new InsertQueryCollection.
        /// </param>
        public InsertQueryCollection(InsertQueryCollection items)
        {
            this.AddRange(items);
        }

        /// <summary>
        /// Adds the elements of an array to the end of this InsertQueryCollection.
        /// </summary>
        /// <param name="items">
        /// The array whose elements are to be added to the end of this InsertQueryCollection.
        /// </param>
        public virtual void AddRange(InsertQuery[] items)
        {
            foreach (InsertQuery item in items)
            {
                this.List.Add(item);
            }
        }

        /// <summary>
        /// Adds the elements of another InsertQueryCollection to the end of this InsertQueryCollection.
        /// </summary>
        /// <param name="items">
        /// The InsertQueryCollection whose elements are to be added to the end of this InsertQueryCollection.
        /// </param>
        public virtual void AddRange(InsertQueryCollection items)
        {
            foreach (InsertQuery item in items)
            {
                this.List.Add(item);
            }
        }

        /// <summary>
        /// Adds an instance of type InsertQuery to the end of this InsertQueryCollection.
        /// </summary>
        /// <param name="value">
        /// The InsertQuery to be added to the end of this InsertQueryCollection.
        /// </param>
        public virtual void Add(InsertQuery value)
        {
            this.List.Add(value);
        }

        /// <summary>
        /// Determines whether a specific InsertQuery value is in this InsertQueryCollection.
        /// </summary>
        /// <param name="value">
        /// The InsertQuery value to locate in this InsertQueryCollection.
        /// </param>
        /// <returns>
        /// true if value is found in this InsertQueryCollection;
        /// false otherwise.
        /// </returns>
        public virtual bool Contains(InsertQuery value)
        {
            return this.List.Contains(value);
        }

        /// <summary>
        /// Return the zero-based index of the first occurrence of a specific value
        /// in this InsertQueryCollection
        /// </summary>
        /// <param name="value">
        /// The InsertQuery value to locate in the InsertQueryCollection.
        /// </param>
        /// <returns>
        /// The zero-based index of the first occurrence of the _ELEMENT value if found;
        /// -1 otherwise.
        /// </returns>
        public virtual int IndexOf(InsertQuery value)
        {
            return this.List.IndexOf(value);
        }

        /// <summary>
        /// Inserts an element into the InsertQueryCollection at the specified index
        /// </summary>
        /// <param name="index">
        /// The index at which the InsertQuery is to be inserted.
        /// </param>
        /// <param name="value">
        /// The InsertQuery to insert.
        /// </param>
        public virtual void Insert(int index, InsertQuery value)
        {
            this.List.Insert(index, value);
        }

        /// <summary>
        /// Gets or sets the InsertQuery at the given index in this InsertQueryCollection.
        /// </summary>
        public virtual InsertQuery this[int index]
        {
            get
            {
                return (InsertQuery)this.List[index];
            }
            set
            {
                this.List[index] = value;
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific InsertQuery from this InsertQueryCollection.
        /// </summary>
        /// <param name="value">
        /// The InsertQuery value to remove from this InsertQueryCollection.
        /// </param>
        public virtual void Remove(InsertQuery value)
        {
            this.List.Remove(value);
        }

        /// <summary>
        /// Returns the total number of terms in the bulk collection.
        /// </summary>
        /// <returns>
        /// The total number of terms in the bulk collection
        /// </returns>
        public int TotalTermCount()
        {
            return List.Cast<InsertQuery>().Sum(query => query.Terms.Count);
        }

        /// <summary>
        /// Validates InsertQueryCollection
        /// </summary>
        public void Validate()
        {
            if (!List.Cast<InsertQuery>().All(query => query.Terms.Cast<UpdateTerm>().All(update => update.Value.Type == SqlExpressionType.Constant
                                                                                                    || update.Value.Type == SqlExpressionType.Null)))
                throw new InvalidQueryException("Invalid SqlExpressionType.");

            // Next check if all queries contain the same fields
            InsertQuery first = this[0];
            var otherTerms = first.Terms.Cast<UpdateTerm>();
            if (List.Cast<InsertQuery>().Any(query => !query.Terms.Cast<UpdateTerm>().SequenceEqual(otherTerms, new FieldComparer())))
                throw new InvalidQueryException("Each insert query in a bulk insert query must have the same fields.");
        }

        private class FieldComparer : IEqualityComparer<UpdateTerm>
        {
            public bool Equals(UpdateTerm x, UpdateTerm y)
            {
                return x != null && y != null && string.Equals(x.FieldName, y.FieldName);
            }

            public int GetHashCode(UpdateTerm obj)
            {
                return obj == null ? 0 : obj.FieldName.GetHashCode();
            }
        }

        /// <summary>
        /// Type-specific enumeration class, used by InsertQueryCollection.GetEnumerator.
        /// </summary>
        public class Enumerator : System.Collections.IEnumerator
        {
            private System.Collections.IEnumerator wrapped;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="collection"></param>
            public Enumerator(InsertQueryCollection collection)
            {
                this.wrapped = ((System.Collections.CollectionBase)collection).GetEnumerator();
            }

            /// <summary>
            /// 
            /// </summary>
            public InsertQuery Current
            {
                get
                {
                    return (InsertQuery)(this.wrapped.Current);
                }
            }

            /// <summary>
            /// 
            /// </summary>
            object System.Collections.IEnumerator.Current
            {
                get
                {
                    return (InsertQuery)(this.wrapped.Current);
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
        /// Returns an enumerator that can iterate through the elements of this InsertQueryCollection.
        /// </summary>
        /// <returns>
        /// An object that implements System.Collections.IEnumerator.
        /// </returns>        
        public new virtual InsertQueryCollection.Enumerator GetEnumerator()
        {
            return new InsertQueryCollection.Enumerator(this);
        }

    }
}
