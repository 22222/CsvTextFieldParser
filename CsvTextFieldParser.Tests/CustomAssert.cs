using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Sdk;

namespace NotVisualBasic.FileIO
{
    internal static class CustomAssert
    {
        public static void Equal(object expected, object actual, string message)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (EqualException ex)
            {
                throw new CustomXUnitException(message, ex);
            }
        }

        public static void Equal(string expected, string actual, string message)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (EqualException ex)
            {
                throw new CustomXUnitException(message, ex);
            }
        }

        public static void Equal<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
        {
            try
            {
                Assert.Equal(expected, actual);
            }
            catch (EqualException ex)
            {
                throw new CustomXUnitException(message, ex);
            }
        }

        public static void NotNull(object obj, string message)
        {
            try
            {
                Assert.NotNull(obj);
            }
            catch (NotNullException ex)
            {
                throw new CustomXUnitException(message, ex);
            }
        }

        private sealed class CustomXUnitException : XunitException
        {
            public CustomXUnitException()
            {
            }

            public CustomXUnitException(string userMessage)
                : base(userMessage)
            {
            }

            public CustomXUnitException(string userMessage, Exception innerException)
                : base(userMessage, innerException)
            {
            }
        }
    }
}
