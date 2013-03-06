using System;
using Controll.Hosting.NHibernate;
using NHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class DatabaseTests : TestBase
    {
<<<<<<< HEAD
        [TestMethod]
        public void ShouldBeAbleToCreateNHibernateSession()
        {
            SessionFactory.OpenSession();
=======

        [TestMethod]
        public void ShouldBeAbleToCreateNHibernateSession()
        {
            NHibernateHelper.OpenSession();
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }
    }
}
