using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Controll.Hosting.Tests
{
    public static class AssertionHelper
    {
        public static void Throws<T>(Action action) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }
            catch (Exception ex)
            {
                Assert.Fail("Expected exception of type {0}. But another exception was thrown: {1}", typeof (T), ex.GetType());
            }

            Assert.Fail("Expected exception of type {0} but no exception was thrown", typeof(T));
        }

        public static void AssertEnumerableItemsAreEqual(IEnumerable<object> enumerable1, IEnumerable<object> enumerable2)
        {
            var list1 = enumerable1 as IList<object> ?? enumerable1.ToList();
            var list2 = enumerable2 as IList<object> ?? enumerable2.ToList();

            if(list1.Count != list2.Count)
                Assert.Fail("Not equal number of list items");

            for (int i = 0; i < list1.Count(); i++)
            {
                Assert.AreEqual(list1[i], list2[i]);
            }
        }
    }

}
