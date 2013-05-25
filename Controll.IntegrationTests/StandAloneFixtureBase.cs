using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using NUnit.Framework;

namespace Controll.IntegrationTests
{
    public class StandAloneFixtureBase
    {
        private StandAloneServerProvider _standAloneServerProvider;
        public Activity Activity { get { return _standAloneServerProvider.Activity; } }

        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            Console.WriteLine("ALRIGHT");
            _standAloneServerProvider = new StandAloneServerProvider();
            _standAloneServerProvider.Start();

        }
        [TestFixtureTearDown]
        public void FixtureTearDown()
        {
            _standAloneServerProvider.Dispose();
        }
        
        [SetUp]
        public void SetUp()
        {
            //Console.WriteLine("Sweeping connected clients...");
            //_standAloneServerProvider.Sweep();
        }
    }
}
