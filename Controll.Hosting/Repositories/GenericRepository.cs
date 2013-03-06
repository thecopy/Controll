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
<<<<<<< HEAD
        protected readonly ISession Session;
        
        public GenericRepository(ISession session)
        {
            Session = session;
        }

        public void Add(T entity)
        {
                Session.Save(entity);
=======
        public void Add(T entity)
        {
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Save(entity);
                transaction.Commit();
            }
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        public void Update(T entity)
        {
<<<<<<< HEAD
                Session.Update(entity);
=======
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Update(entity);
                transaction.Commit();
            }
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        public T Get(object identifier)
        {
<<<<<<< HEAD
            return Session.Get<T>(identifier);
=======
            using (var session = NHibernateHelper.OpenSession())
                return session.Get<T>(identifier);
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        public void Remove(T entity)
        {
<<<<<<< HEAD
            Session.Delete(entity);
=======
            using (var session = NHibernateHelper.OpenSession())
            using (var transaction = session.BeginTransaction())
            {
                session.Delete(entity);
                transaction.Commit();
            }
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }

        public IList<T> GetAll(int maxResult = 100)
        {
<<<<<<< HEAD
            return Session.CreateCriteria<T>()
                          .SetMaxResults(maxResult)
                          .List<T>();
=======
            using (var session = NHibernateHelper.OpenSession())
                return session.CreateCriteria<T>()
                    .SetMaxResults(maxResult)
                    .List<T>();
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }
    }
}
