using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Repositories;

namespace Controll.Hosting.Tests
{
    internal class InMemoryRepository<T> : IGenericRepository<T> where T:class
    {
        protected readonly ICollection<T> Collection;
        internal bool GenerateIdentityOnAdd;
        public InMemoryRepository() : base()
        {
            Collection = new Collection<T>();
        }

        public void Add(T entity)
        {
            if (GenerateIdentityOnAdd)
            {
                var propertyInfo = typeof (T).GetProperty("Id") ?? typeof (T).GetProperty("Ticket");
                if (typeof (T) == typeof (int))
                    propertyInfo.SetValue(entity, Collection.Count);
                if (typeof (T) == typeof (Guid))
                    propertyInfo.SetValue(entity, Guid.NewGuid());
            }

            Collection.Add(entity);
        }

        public void Update(T entity)
        {
            // -
        }

        public T Get(object identifier)
        {
            var propertyInfo = typeof (T).GetProperty("Id") ?? typeof (T).GetProperty("Ticket");

            return Collection.SingleOrDefault(e => propertyInfo.GetValue(e).Equals(identifier));
        }

        public void Remove(T entity)
        {
            Collection.Remove(entity);
        }

        public IList<T> GetAll(int maxResults = 100)
        {
            return Collection.Take(maxResults).ToList();
        }

        public IQueryable<T> Query { get; private set; }
    }
}
