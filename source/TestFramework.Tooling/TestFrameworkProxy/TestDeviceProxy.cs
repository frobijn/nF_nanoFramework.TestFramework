// Copyright (c) .NET Foundation and Contributors.
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using nanoFramework.TestFramework;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// A proxy to the <see cref="nanoFramework.TestFramework.TestDevice"/>
    /// class that can be passed to attributes of the nanoCLR-platform
    /// that take an <see cref="ITestDevice"/> interface as argument.
    /// </summary>
    public class TestDeviceProxy
    {
        #region Fields
        /// <summary>
        /// The <see cref="nanoFramework.TestFramework.Tools.TestDeviceProxy"/> type as known in the test assembly,
        /// on the nanoCLR platform.
        /// </summary>
        private static Type s_testDeviceProxyType;
        #endregion

        #region Construction / initialisation
        /// <summary>
        /// This method has to be called by an attribute proxy that accepts a <see cref="nanoFramework.TestFramework.ITestDevice"/>
        /// as argument. It is used to find the matching <see cref="nanoFramework.TestFramework.Tools.TestDeviceProxy"/> type.
        /// </summary>
        /// <param name="interfaceType"></param>
        internal static void FoundITestDevice(Type interfaceType)
        {
            s_testDeviceProxyType ??= (from type in interfaceType.Assembly.GetTypes()
                                       where type.FullName == typeof(nanoFramework.TestFramework.Tools.TestDeviceProxy).FullName
                                       select type).FirstOrDefault();
        }

        /// <summary>
        /// Create a proxy on the nanoCLR-platform that obtains its data from a device defined for the .NET platform,
        /// </summary>
        /// <param name="testDevice">Instance of a class that implements the <see cref="ITestDevice"/> interface</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="testDevice"/> is <c>null</c> or if it does not
        /// implement the <see cref="ITestDevice"/> interface.</exception>
        /// <exception cref="FrameworkMismatchException">Thrown if there is a mismatch detected in the test framework. Make sure
        /// to use matching versions of this application and the nanoFramework.TestFramework.</exception>
        public TestDeviceProxy(ITestDevice testDevice)
        {
            if (testDevice is null)
            {
                throw new ArgumentNullException(nameof(testDevice));
            }
            if (s_testDeviceProxyType is null)
            {
                throw new FrameworkMismatchException($"{nameof(TestDeviceProxy)} is not found in the nanoFramework assembly");
            }
            try
            {
                ITestDeviceProxy = Activator.CreateInstance(s_testDeviceProxyType, testDevice, typeof(ITestDevice));
            }
            catch (Exception ex)
            {
                if (ex.GetType().FullName == typeof(ArgumentException).FullName)
                {
                    // The proxy cannot find the ITestDevice properties/methods
                    throw new FrameworkMismatchException($"The definition of {nameof(ITestDevice)} in this application does not match the one in the nanoFramework assembly.");
                }
                else
                {
                    throw;
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Get the instance that implements the <see cref="ITestDevice"/> interface on the nanoCLR
        /// platform and that passes method calls to the <see cref="ITestDevice"/> implementation on the .NET platform.
        /// </summary>
        internal object ITestDeviceProxy
        {
            get;
        }
        #endregion
    }
}
