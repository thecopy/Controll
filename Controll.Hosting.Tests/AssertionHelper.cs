using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Controll.Hosting.Tests
{
    public static class AssertionHelper
    {
       
        public static void AssertEnumerableItemsAreEqual<T1, T2>(IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2, Func<T1, T2, bool> comparer = null)
            where T1 : class
            where T2 : class
        {
            Assert.True(IsEnumerableItemsEqual(enumerable1, enumerable2, comparer));
        }

        public static void AssertDictionariesAreEqual<T1, T2, T3, T4>(IDictionary<T1, T2> dictionary1, IDictionary<T3, T4> dictionary2)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            Assert.True(IsDictionariesEqual(dictionary1, dictionary2));
        }

        public static bool IsEnumerableItemsEqual<T1, T2>(IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2, Func<T1, T2, bool> comparer = null) 
            where T1:class 
            where T2:class
        {
            var list1 = enumerable1 as IList<T1> ?? enumerable1.ToList();
            var list2 = enumerable2 as IList<T2> ?? enumerable2.ToList();

            if (list1.Count != list2.Count)
                throw new Exception(String.Format("Not equal number of list items"));

            for (int i = 0; i < list1.Count; i++)
            {
                if (comparer == null)
                {
                    if (typeof(T1) != typeof(T2) || !Equals(list1[i], list2[i]))
                        return false;
                }
                else if (false == comparer.Invoke(list1[i], list2[i]))
                {
                    return false;
                }

            }

            return true;
        }

        public static bool IsDictionariesEqual<T1, T2, T3, T4>(IDictionary<T1, T2> dictionary1, IDictionary<T3, T4> dictionary2)
            where T1 : class
            where T2 : class
            where T3 : class
            where T4 : class
        {
            if (dictionary1 == null && dictionary2 == null)
                return true;
            if (dictionary1 == null || dictionary2 == null)
                return false;

            if (dictionary1.Count != dictionary2.Count)
                throw new Exception(String.Format("Not equal number of list items"));

            if (typeof (T1) != typeof (T3) || typeof (T2) != typeof (T4))
                return false;

            for (int i = 0; i < dictionary2.Count; i++)
            {
                var pair1 = dictionary1.ElementAt(i);
                var pair2 = dictionary2.ElementAt(i);
                    if (typeof(T1) != typeof(T2) || !(pair1.Key.Equals(pair2.Key) && pair1.Value.Equals(pair2.Value)))
                        return false;
            }

            return true;
        }
    }

}
