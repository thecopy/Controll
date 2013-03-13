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
        public static void Throws<T>(Action action, string message = null, bool innerException = false) where T : Exception
        {
            if(message != null && innerException)
                throw new NotImplementedException("InnerException with message assertion is not supported.");

            try
            {
                action();
            }
            catch (T ex)
            {
                if(message == null || ex.Message == message)
                    return;

                Assert.Fail("Correct type of exception but message differ. Expected: \"{0}\", but got: \"{1}\"", message, ex.Message);
            }
            catch (Exception ex)
            {
                if (innerException)
                {
                    if (ex.InnerException != null && ex.InnerException.GetType() == typeof (T))
                    {
                        return;
                    }

                    var theInnerException = ex.InnerException;
                    if (theInnerException == null)
                        Assert.Fail("Expected inner exception of type {0}. But was <null>", typeof (T));

                    Assert.Fail("Expected inner exception of type {0}. But another exception was thrown: {1}\n\n{2}",
                                typeof (T), theInnerException.GetType(), theInnerException);
                }
                

                Assert.Fail("Expected exception of type {0}. But another exception was thrown: {1}\n\n{2}", typeof (T), ex.GetType(), ex);
            }

            Assert.Fail("Expected exception of type {0} but no exception was thrown", typeof(T));
        }


        public static void AssertEnumerableItemsAreEqual<T1, T2>(IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2, Func<T1, T2, bool> comparer = null)
            where T1 : class
            where T2 : class
        {
            Assert.IsTrue(IsEnumerableItemsEqual(enumerable1, enumerable2, comparer));
        }

        public static bool IsEnumerableItemsEqual<T1, T2>(IEnumerable<T1> enumerable1, IEnumerable<T2> enumerable2, Func<T1, T2, bool> comparer = null) 
            where T1:class 
            where T2:class
        {
            var list1 = enumerable1 as IList<T1> ?? enumerable1.ToList();
            var list2 = enumerable2 as IList<T2> ?? enumerable2.ToList();

            if (list1.Count != list2.Count)
                Assert.Fail("Not equal number of list items");

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
    }

}
