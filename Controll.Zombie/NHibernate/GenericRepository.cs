using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Controll.NHibernate
{
    public interface IGenericRepository<T> where T : class
    {
        void Add(T entity);
        void Update(T entity);
        T Get(object identifier);
        void Remove(T entity);
        IList<T> GetAll();
    }

    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        public void Add(T entity)
        {
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Save(entity);
                transaction.Commit();
            }
        }

        public void Update(T entity)
        {
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Update(entity);
                transaction.Commit();
            }
        }

        public T Get(object identifier)
        {
            using (var session = NHibernateHelper.OpenSession())
                return session.Get<T>(identifier);
        }

        public void Remove(T entity)
        {
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Delete(entity);
                transaction.Commit();
            }
        }

        public IList<T> GetAll()
        {
            using (var session = NHibernateHelper.OpenSession())
                return session.CreateCriteria<T>()
                    .List<T>();
        }
    }
}
