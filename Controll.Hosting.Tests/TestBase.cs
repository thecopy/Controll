using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.NHibernate;
using NUnit.Framework;
using NHibernate;

namespace Controll.Hosting.Tests
{
    public class TestBase
    {
        private ISessionFactory _sessionFactory;

        protected ISessionFactory SessionFactory
        {
            get { return _sessionFactory ?? (_sessionFactory = NHibernateHelper.GetSessionFactoryForTesting()); }
            set { _sessionFactory = value; }
        }
        
    }
}
