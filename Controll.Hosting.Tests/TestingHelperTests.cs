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
    public class TestingHelperTests
    {
        [TestMethod]
        public void ShouldBeAbleToCompareConcreteToViewModel()
        {
            var parameters =
                Builder<ParameterDescriptor>.CreateListOfSize(5)
                                            .All()
                                            .Do(p => p.PickerValues = Builder<PickerValue>.CreateListOfSize(2).Build())
                                            .Build();

            var commands =
                Builder<ActivityCommand>.CreateListOfSize(5)
                                        .All()
                                        .Do(c => c.ParameterDescriptors = parameters.ToList())
                                        .Build();

            var zombie = Builder<Zombie>.CreateNew().Build();

            zombie.Activities = Builder<Activity>.CreateListOfSize(5)
                                                 .All().Do(a => a.Commands = commands.ToList()).Build();

            var zombies = TestingHelper.GetListOfZombies();
            var zombieVms = zombies.Select(ViewModelHelper.CreateViewModel);
            Assert.IsTrue(AssertionHelper.IsEnumerableItemsEqual(zombies, zombieVms, TestingHelper.ZombieViewModelComparer));
        }

        [TestMethod]
        public void ShouldBeAbleToCreateActivityViewModel()
        {
            var lastUpdate = DateTime.Now;
            var activity = Builder<Activity>.CreateNew()
                                            .With(x => x.Id = Guid.NewGuid())
                                            .And(x => x.Version = new Version(1, 1, 1, 1))
                                            .And(x => x.Commands = Builder<ActivityCommand>.CreateListOfSize(10)
                                                .All().Do(a => a.ParameterDescriptors = Builder<ParameterDescriptor>.CreateListOfSize(10).Build())
                                                .Build())
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
            DateTime lastUpdate = DateTime.Now;
            Zombie zombie = Builder<Zombie>.CreateNew()
                                           .With(x =>
                                               x.Activities = Builder<Activity>
                                               .CreateListOfSize(10)
                                               .All().Do(a =>a.Commands =Builder<ActivityCommand>.CreateListOfSize(10).Build())
                                               .Build())
                                           .Build();

            ZombieViewModel vm = ViewModelHelper.CreateViewModel(zombie);

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
