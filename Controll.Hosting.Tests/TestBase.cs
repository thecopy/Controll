using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.NHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate;

namespace Controll.Hosting.Tests
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


        [TestInitialize]
        public void InitializeTestBase()
        {
        }
    }
}
