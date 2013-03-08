using System;
using Controll.Hosting.NHibernate;
using NHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class DatabaseTests : TestBase
    {
        [TestMethod]
        public void ShouldBeAbleToCreateNHibernateSession()
        {
            SessionFactory.OpenSession();
        }
    }
}
