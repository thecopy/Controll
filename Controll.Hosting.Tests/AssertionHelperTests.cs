using System;
using System.Collections.Generic;
using System.Linq;
using Controll.Hosting.Models;
using FizzWare.NBuilder;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public class AssertionHelperTests
    {
        [Test]
        public void ShouldAssertFailIfTwoListsAreNotEqual()
        {
            var list1 = Builder<Zombie>.CreateListOfSize(10).Build();
            var list2 = Builder<Zombie>.CreateListOfSize(5).Build();

            Assert.Throws<Exception>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2)); // Different count

            var list3 = Builder<Zombie>.CreateListOfSize(10).Random(3).Do(z => z.Name = "hurr").Build();
            Assert.Throws<AssertionException>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list3)); // Different items

            Assert.Throws<AssertionException>(() => AssertionHelper.AssertEnumerableItemsAreEqual(list1, list1, (a, b) => a.Name != b.Name)); // Exactly equal lists but different according to custom compare
        }

        [Test]
        public void ShouldNotAssertFailIfTwoListsAreEqual()
        {
            var list1 = Builder<Zombie>.CreateListOfSize(10).Build();
            var list2 = list1.ToList();

            AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2);
            AssertionHelper.AssertEnumerableItemsAreEqual(list1, list2, (zombie, zombie1) => zombie.Name == zombie1.Name);
        }

        [Test]
        public void ShouldNotAssertFailIfTwoDictionariesAreEqual()
        {
            var dict1 = new Dictionary<string, string>();
            var dict2 = new Dictionary<string, string>();

            dict1.Add("k1", "v1");
            dict1.Add("k2", "v2");

            dict2.Add("k1", "v1");
            dict2.Add("k2", "v2");

            Assert.True(AssertionHelper.IsDictionariesEqual(dict1, dict2));
        }

        [Test]
        public void ShouldAssertFailIfTwoDictionariesAreNotEqual()
        {
            var dict1 = new Dictionary<string, string>();
            var dict2 = new Dictionary<string, string>();

            dict1.Add("k1", "v1");
            dict1.Add("k2", "v2");

            dict2.Add("k1", "v1AAAAAA");
            dict2.Add("k2", "v2");
            
            Assert.False(AssertionHelper.IsDictionariesEqual(dict1, dict2));

            var dict3 = new Dictionary<string, Exception> {{"k1", null}, {"k2", null}};

            Assert.False(AssertionHelper.IsDictionariesEqual(dict1, dict3));
        }

    }
}
