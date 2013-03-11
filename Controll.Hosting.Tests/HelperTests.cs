using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Helpers;
using Controll.Hosting.Models;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class HelperTests
    {
        [TestMethod]
        public void ShouldBeAbleToCreateActivityViewModel()
        {
            var lastUpdate = DateTime.Now;
            var activity = Builder<Activity>.CreateNew()
                                            .With(x => x.Id = Guid.NewGuid())
                                            .And(x => x.Version = new Version(1, 1, 1, 1))
                                            .And(x => x.Commands = Builder<ActivityCommand>.CreateListOfSize(10).Build())
                                            .And(x => x.LastUpdated = lastUpdate)
                                            .Build();

            var vm = ViewModelHelper.CreateViewModel(activity);

            Assert.AreEqual(activity.Name, vm.Name);
            Assert.AreEqual(activity.Description, vm.Description);
            Assert.AreEqual(activity.CreatorName, vm.CreatorName);
            Assert.AreEqual(activity.Version, vm.Version);
            Assert.AreEqual(activity.LastUpdated, vm.LastUpdated);
            Assert.AreEqual(activity.Id, vm.Key);

            Assert.AreEqual(activity.Commands.Count, vm.Commands.Count());
        }

        [TestMethod]
        public void ShouldBeAbleToCreateZombieViewModel()
        {
            var lastUpdate = DateTime.Now;
            var zombie = Builder<Zombie>.CreateNew()
                                            .With(x => x.Activities = Builder<Activity>.CreateListOfSize(10).Build())
                                            .Build();

            var vm = ViewModelHelper.CreateViewModel(zombie);

            Assert.AreEqual(zombie.Name, vm.Name);
            Assert.AreEqual(true, vm.IsOnline);
            Assert.AreEqual(zombie.Activities.Count, vm.Activities.Count());
        }

        [TestMethod]
        public void ShouldSetKeyOnActivityAttribute()
        {
            var key = Guid.NewGuid();
            var attrib = new ActivityAttribute(key.ToString());

            Assert.AreEqual(key, attrib.Key);
        }
    }
}
