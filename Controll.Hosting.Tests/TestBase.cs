using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.NHibernate;
using Microsoft.VisualStudio.TestTools.UnitTesting;
<<<<<<< HEAD
using NHibernate;
=======
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class TestBase
    {
<<<<<<< HEAD
        protected ISessionFactory SessionFactory;

        [TestInitialize]
        public void InitializeTestBase()
        {
            SessionFactory = NHibernateHelper.GetSessionFactoryForMockedData();
=======
        [ClassInitialize]
        public void InitializeTest()
        {
            Console.WriteLine("Settings NHibernateHelper.IsInTesting -> True");
            NHibernateHelper.IsInTesting = true;
>>>>>>> dd2c3d7dfe81074e7c5a73f8e4ca2584481a74f1
        }
    }
}
