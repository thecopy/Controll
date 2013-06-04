using Controll.Hosting.NHibernate;
using NHibernate;

namespace Controll.IntegrationTests
{
    public class TestBase
    {
        private ISessionFactory _sessionFactory;

        protected ISessionFactory SessionFactory
        {
            get
            {
                if(_sessionFactory == null)
                    _sessionFactory = NHibernateHelper.GetSessionFactoryForTesting();

                return _sessionFactory;
            }
            set { _sessionFactory = value; }
        }
    }
}
