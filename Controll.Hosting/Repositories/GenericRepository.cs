using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.NHibernate;
using NHibernate;
using NHibernate.Criterion;

namespace Controll.Hosting.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        T Get(object identifier);
        void Remove(T entity);
        IList<T> GetAll(int maxResults = 100);
    }

    public class GenericRepository<T> : IGenericRepository<T> where T: class
    {
        protected readonly ISession Session;
        
        public GenericRepository(ISession session)
        {
            Session = session;
        }

        public void Add(T entity)
        {
                Session.Save(entity);
        }

        public void Update(T entity)
        {
                Session.Update(entity);
        }

        public T Get(object identifier)
        {
            return Session.Get<T>(identifier);
        }

        public void Remove(T entity)
        {
            Session.Delete(entity);
        }

        public IList<T> GetAll(int maxResult = 100)
        {
            return Session.CreateCriteria<T>()
                          .SetMaxResults(maxResult)
                          .List<T>();
        }
    }
}
