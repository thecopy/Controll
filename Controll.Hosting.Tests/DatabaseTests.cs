using System;
using Controll.Hosting.NHibernate;
using NHibernate;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    
    public class DatabaseTests : TestBase
    {
        [Test]
        public void ShouldBeAbleToCreateNHibernateSession()
        {
            SessionFactory.OpenSession();
        }
    }
}
