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

        public static void AssertEnumerableItemsAreEqual<T1,T2>(IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2, Func<T1,T2, bool> comparer = null)
        {
            var list1 = enumerable1 as IList<T1> ?? enumerable1.ToList();
            var list2 = enumerable2 as IList<T2> ?? enumerable2.ToList();

            if (list1.Count != list2.Count)
                Assert.Fail("Not equal number of list items");

            for (int i = 0; i < list1.Count; i++)
            {
                if(comparer == null)
                    Assert.AreEqual(list1[i], list2[i]);
                else
                    Assert.IsTrue(comparer.Invoke(list1[i], list2[i]), "Items differ according to custom comparision");
            }
        }
    }

}
