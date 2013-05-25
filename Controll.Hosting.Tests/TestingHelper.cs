using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Common;
using Controll.Common.ViewModels;
using Controll.Hosting.Models;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class TestingHelper
    {
        public class Comparer<T> : IEqualityComparer<T>
        {
            private readonly Func<T, T, bool> _comparer;

            public Comparer(Func<T, T, bool> comparer)
            {
                if (comparer == null)
                    throw new ArgumentNullException("comparer");

                _comparer = comparer;
            }

            public bool Equals(T x, T y)
            {
                return _comparer(x, y);
            }

            public int GetHashCode(T obj)
            {
                return obj.ToString().ToLower().GetHashCode();
            }
        }

        internal static IList<Zombie> GetListOfZombies(int count = 3)
        {
            IList<ParameterDescriptor> parameters =
                Builder<ParameterDescriptor>.CreateListOfSize(count)
                                            .All()
                                            .Do(p => p.PickerValues = Builder<PickerValue>.CreateListOfSize(count)/*.All().Do(x => x.Id = Guid.NewGuid())*/.Build())
                                            .Build();

            IList<ActivityCommand> commands =
                Builder<ActivityCommand>.CreateListOfSize(count)
                                        .All()
                                        .Do(c => c.ParameterDescriptors = parameters.ToList())
                                        .Build();

            IList<Zombie> zombies =
                Builder<Zombie>.CreateListOfSize(count).All().Do(x => x.Activities = Builder<Activity>.CreateListOfSize(count)
                                                                                                  .All()
                                                                                                  .Do(
                                                                                                      a =>
                                                                                                      a.Commands =
                                                                                                      commands.ToList())
                                                                                                      .And(a => a.Id = Guid.NewGuid())
                                                                                                  .Build()).Build();
            return zombies;
        }
        internal static Func<Zombie, ZombieViewModel, bool> ZombieViewModelComparer
        {
            get
            {
                return (z, vm) => 
                    vm.Name == z.Name 
                    && vm.IsOnline == z.IsOnline()
                    && AssertionHelper.IsEnumerableItemsEqual(z.Activities, vm.Activities, ActivityViewModelComparer);
            }
        }

        internal static Func<Activity, ActivityViewModel, bool> ActivityViewModelComparer
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

        internal static Func<ActivityCommand, ActivityCommandViewModel, bool> ActivityCommandViewModelComparer
        {
            get
            {
                return (c, vm) => 
                                  vm.Label == c.Label &&
                                  vm.Name == c.Name &&
                                  AssertionHelper.IsEnumerableItemsEqual(c.ParameterDescriptors, vm.ParameterDescriptors,
                                                                         ParameterDescriptorViewModelComparer);
            }
        }

        internal static Func<ParameterDescriptor, ParameterDescriptorViewModel, bool> ParameterDescriptorViewModelComparer
        {
            get
            {
                return (p, vm) => vm.Description == p.Description &&
                                  vm.Label == p.Label &&
                                  vm.IsBoolean == p.IsBoolean &&
                                  vm.Name == p.Name &&
                                  AssertionHelper.IsEnumerableItemsEqual(p.PickerValues, vm.PickerValues, PickerValueViewModelComparer);
            }
        }

        internal static Func<PickerValue, PickerValueViewModel, bool> PickerValueViewModelComparer
        {
            get { return ComparePickerValueToViewModel; }
        } 
        private static bool ComparePickerValueToViewModel(PickerValue pv, PickerValueViewModel vm)
        {
            var result =  vm.CommandName == pv.CommandName &&
                                   vm.Description == pv.Description &&
                                   vm.Identifier == pv.Identifier &&
                                   vm.IsCommand == pv.IsCommand &&
                                   vm.Label == pv.Label &&
                                   AssertionHelper.IsDictionariesEqual(vm.Parameters, pv.Parameters);

            return result;
        }
    }
    
}
