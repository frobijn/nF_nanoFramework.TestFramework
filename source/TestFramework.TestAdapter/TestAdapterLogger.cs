// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using nanoFramework.TestFramework.Tooling;

namespace nanoFramework.TestFramework.TestAdapter
{
    /// <summary>
    /// Helper to send log messages to the caller of the test adapter interfaces.
    /// </summary>
    public sealed class TestAdapterLogger
    {
        #region Fields
        private readonly IMessageLogger _logger;
        #endregion

        #region Construction
        /// <summary>
        /// Create the helper.
        /// </summary>
        /// <param name="loggers"></param>
        public TestAdapterLogger(IMessageLogger logger)
        {
            _logger = logger;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="level">Level.</param>
        /// <param name="message">Text of the message.</param>
        public void LogMessage(LoggingLevel level, string message)
        {
            TestMessageLevel testMessageLevel =
                level == LoggingLevel.Error ? TestMessageLevel.Error
                : level == LoggingLevel.Warning ? TestMessageLevel.Warning
                : TestMessageLevel.Informational;

            _logger.SendMessage(testMessageLevel, message);
        }
        #endregion
    }
}
