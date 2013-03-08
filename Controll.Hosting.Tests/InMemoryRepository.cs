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
        private readonly ICollection<T> _collection;
 
        public InMemoryRepository() : base()
        {
            _collection = new Collection<T>();
        } 

        public void Add(T entity)
        {
             _collection.Add(entity);
        }

        public void Update(T entity)
        {
            // -
        }

        public T Get(object identifier)
        {
            var propertyInfo = typeof (T).GetProperty("Id");

            return _collection.SingleOrDefault(e => propertyInfo.GetValue(e).Equals(identifier));
        }

        public void Remove(T entity)
        {
            _collection.Remove(entity);
        }

        public IList<T> GetAll(int maxResults = 100)
        {
            return _collection.Take(maxResults).ToList();
        }
    }
}
