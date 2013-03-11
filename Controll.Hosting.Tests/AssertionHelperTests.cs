using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Controll.Hosting.Models;
using FizzWare.NBuilder;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    [TestClass]
    public class AssertionHelperTests
    {
        [TestMethod]
        public void ShouldAssertFailWhenNoExceptionIsThrown()
        {
            try
            {
                AssertionHelper.Throws<Exception>(() => { });
            }
            catch (AssertFailedException)
            {
                // OK
                return;
            }

            Assert.Fail("Did not fail when no exception was thrown");
        }

        [TestMethod]
        public void ShouldAssertFailWheWrongExceptionIsThrown()
        {
            try
            {
                AssertionHelper.Throws<ArgumentException>(() => { throw new InvalidCastException(); });
            }
            catch (AssertFailedException)
            {
                // OK
                return;
            }
            Assert.Fail("Did not fail when no exception was thrown");
        }

        [TestMethod]
        public void ShouldNotAssertFailWhenCorrectExceptionIsThrown()
        {
            AssertionHelper.Throws<InvalidCastException>(() => { throw new InvalidCastException();});
        }

        [TestMethod]
        public void ShouldNotAssertFailWhenAnyExceptionIsThrown()
        {
            AssertionHelper.Throws<Exception>(() => { throw new InvalidCastException(); });
        }

        [TestMethod]
        public void ShouldAssertFailIfTwoListsAreNotEqual()
        {
            var list1 = Builder<Zombie>.CreateListOfSize(10).Build();
            var list2 = Builder<Zombie>.CreateListOfSize(5).Build();

            AssertionHelper.Throws<AssertFailedException>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2)); // Different count

            var list3 = Builder<Zombie>.CreateListOfSize(10).Random(3).Do(z => z.Name = "hurr").Build();
            AssertionHelper.Throws<AssertFailedException>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list3)); // Different items
        }

        [TestMethod]
        public void ShouldNotAssertFailIfTwoListsAreEqual()
        {
            var list1 = Builder<Zombie>.CreateListOfSize(10).Build();
            var list2 = list1.ToList();

            AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2); 
        }
    }
}
