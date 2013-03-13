using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;
using FizzWare.NBuilder;

namespace Controll.Hosting.Tests
{
    public static class TestingHelper
    {
        public static IList<Zombie> GetListOfZombies()
        {
            IList<ParameterDescriptor> parameters =
                Builder<ParameterDescriptor>.CreateListOfSize(5)
                                            .All()
                                            .Do(p => p.PickerValues = new List<string> {"string1", "string2"})
                                            .Build();

            IList<ActivityCommand> commands =
                Builder<ActivityCommand>.CreateListOfSize(5)
                                        .All()
                                        .Do(c => c.ParameterDescriptors = parameters.ToList())
                                        .Build();

            IList<Zombie> zombies =
                Builder<Zombie>.CreateListOfSize(3).All().Do(x => x.Activities = Builder<Activity>.CreateListOfSize(5)
                                                                                                  .All()
                                                                                                  .Do(
                                                                                                      a =>
                                                                                                      a.Commands =
                                                                                                      commands.ToList())
                                                                                                  .Build()).Build();
            return zombies;
        } 
        public static Func<Zombie, ZombieViewModel, bool> ZombieViewModelComparer
        {
            get
            {
                return (z, vm) => 
                    vm.Name == z.Name 
                    && vm.IsOnline == z.IsOnline()
                    && AssertionHelper.IsEnumerableItemsEqual(z.Activities, vm.Activities, ActivityViewModelComparer);
            }
        }

        public static Func<Activity, ActivityViewModel, bool> ActivityViewModelComparer
        {
            get
            {
                return (a, vm) => a.CreatorName == vm.CreatorName &&
                                  a.Description == vm.Description &&
                                  a.Name == vm.Name &&
                                  a.LastUpdated == vm.LastUpdated &&
                                  a.Version == vm.Version &&
                                  AssertionHelper.IsEnumerableItemsEqual(a.Commands, vm.Commands, ActivityCommandViewModelComparer);
            }
        }

        public static Func<ActivityCommand, ActivityCommandViewModel, bool> ActivityCommandViewModelComparer
        {
            get
            {
                return (c, vm) => vm.IsQuickCommand == c.IsQuickCommand &&
                                  vm.Label == c.Label &&
                                  vm.Name == c.Name &&
                                  AssertionHelper.IsEnumerableItemsEqual(c.ParameterDescriptors, vm.ParameterDescriptors,
                                                                         ParameterDescriptorViewModelComparer);
            }
        }

        public static Func<ParameterDescriptor, ParameterDescriptorViewModel, bool> ParameterDescriptorViewModelComparer
        {
            get
            {
                return (p, vm) => vm.Description == p.Description &&
                                  vm.Label == p.Label &&
                                  vm.Name == p.Name &&
                                  AssertionHelper.IsEnumerableItemsEqual(vm.PickerValues, p.PickerValues);
            }
        }
    }
    
}
