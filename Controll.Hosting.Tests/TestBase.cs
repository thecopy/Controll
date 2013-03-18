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
        protected ISessionFactory SessionFactory;

        [TestInitialize]
        public void InitializeTestBase()
        {
            SessionFactory = NHibernateHelper.GetSessionFactoryForTesting();
        }
    }
}
