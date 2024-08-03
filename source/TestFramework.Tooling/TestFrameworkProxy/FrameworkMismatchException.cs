using System;

namespace nanoFramework.TestFramework.Tooling.TestFrameworkProxy
{
    /// <summary>
    /// Exception thrown if a mismatch has been detected between the
    /// type definitions used by the assembly based on the nanoCLR and
    /// the ones in this assembly. The cause is most likely that the
    /// nanoCLR code and the application using this code are created for
    /// different versions of the nanoFramework.
    /// </summary>
    public class FrameworkMismatchException : Exception
    {
        public FrameworkMismatchException(string message)
            : base(message)
        {
        }
    }
}
