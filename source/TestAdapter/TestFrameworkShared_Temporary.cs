using System;

namespace nanoFramework.TestFramework
{
    /// <summary>
    /// Clean up attribute typically used to clean up after the tests, it will always been called the last after all the Test Method run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class CleanupAttribute : Attribute
    {
    }

    /// <summary>
    /// Setup attribute, will always be launched first by the launcher, typically used to setup hardware or classes that has to be used in all the tests.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal class SetupAttribute : Attribute
    {
    }

    /// <summary>
    /// Data row attribute. Used for passing multiple parameters into same test method.
    /// </summary>
    internal
        class DataRowAttribute : Attribute
    {
        /// <summary>
        /// Array containing all passed parameters
        /// </summary>
        public object[] MethodParameters { get; }

        /// <summary>
        /// Initializes a new instance of the DataRowAttribute class.
        /// </summary>
        /// <param name="methodParameters">Parameters which should be stored for future execution of test method</param>
        /// <exception cref="ArgumentNullException">Thrown when methodParameters is null</exception>
        /// <exception cref="ArgumentException">Thrown when methodParameters is empty</exception>
        public DataRowAttribute(params object[] methodParameters)
        {
            if (methodParameters == null)
            {
                throw new ArgumentNullException($"{nameof(methodParameters)} can not be null");
            }

            if (methodParameters.Length == 0)
            {
                throw new ArgumentException($"{nameof(methodParameters)} can not be empty");
            }

            MethodParameters = methodParameters;
        }
    }
    /// <summary>
    /// The test class attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal
        class TestClassAttribute : Attribute
    {
    }

    /// <summary>
    /// The attribute marks a method as being a test method. The attribute is optional
    /// if any of the other test-related attributes is present.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    internal
    class TestMethodAttribute : Attribute
    {
    }
}
