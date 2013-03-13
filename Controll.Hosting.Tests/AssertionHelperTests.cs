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
        public void ShouldNotAssertFailWhenExpectedExceptionWithCorrectMessageIsThrown()
        {
            try
            {
                AssertionHelper.Throws<ArgumentException>(() => { throw new ArgumentException("i expect this message"); }, "i expect this message");
            }
            catch (AssertFailedException)
            {
                Assert.Fail("Did fail when correct exception with correct message was thrown");
            }

        }

        [TestMethod]
        public void ShouldNotAssertFailWhenExpectedExceptionIsThrown()
        {
            try
            {
                AssertionHelper.Throws<ArgumentException>(() => { throw new ArgumentException(); });
            }
            catch (AssertFailedException)
            {
                Assert.Fail("Did fail when correct exception was thrown");
            }

        }

        [TestMethod]
        public void ShouldNotAssertFailWhenExpectedInnerExceptionIsThrown()
        {
            try
            {
                AssertionHelper.Throws<ArgumentException>(() => { throw new InvalidCastException("", new ArgumentException()); }, innerException:true);
            }
            catch (AssertFailedException)
            {
                Assert.Fail("Did fail when correct inner exception was thrown");
            }

        }

        [TestMethod]
        public void ShouldAssertFailWhenExceptionWithWrongMessageIsThrown()
        {
            try
            {
                AssertionHelper.Throws<ArgumentException>(() => { throw new ArgumentException("message"); }, "i expect this message");
            }
            catch (AssertFailedException)
            {
                // OK
                return;
            }

            Assert.Fail("Did not fail when exception with wrong message was thrown");
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
            AssertionHelper.Throws<InvalidCastException>(() => { throw new InvalidCastException(); });
            AssertionHelper.Throws<InvalidCastException>(() => { throw new ArgumentException("", new InvalidCastException()); }, innerException:true);
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

            AssertionHelper.Throws<AssertFailedException>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list1, (a,b) => a.Name != b.Name)); // Exactly equal lists but different according to custom compare
        }

        [TestMethod]
        public void ShouldNotAssertFailIfTwoListsAreEqual()
        {
            var list1 = Builder<Zombie>.CreateListOfSize(10).Build();
            var list2 = list1.ToList();

            AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2);
            AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2, (zombie, zombie1) => zombie.Name == zombie1.Name);
        }

    }
}
