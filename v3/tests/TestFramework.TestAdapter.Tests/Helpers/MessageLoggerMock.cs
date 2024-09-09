// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestFramework.TestAdapter.Tests.Helpers
{
    public class MessageLoggerMock : IMessageLogger
    {
        #region Properties
        /// <summary>
        /// Get the logged messages
        /// </summary>
        public IReadOnlyList<(TestMessageLevel level, string message)> Messages
            => _messages;
        private readonly List<(TestMessageLevel level, string message)> _messages = new List<(TestMessageLevel level, string message)>();
        #endregion

        #region Helpers
        /// <summary>
        /// Get all messages as a string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Join("\n",
                        from m in Messages
                        select $"{m.level}: {m.message}"
                    ) + '\n';
        }

        /// <summary>
        /// Assert the messages
        /// </summary>
        /// <param name="expectedMessages">Expected messages in the format of <see cref="ToString"/></param>
        /// <param name="minimalLevel">Minimal level of messages to include</param>
        public void AssertEqual(string expectedMessages, TestMessageLevel minimalLevel = TestMessageLevel.Informational)
        {
            Assert.AreEqual(
               (expectedMessages?.Trim() ?? "").Replace("\r\n", "\n") + '\n',
               string.Join("\n",
                       from m in Messages
                       where m.level >= minimalLevel
                       select $"{m.level}: {m.message}"
                   ) + '\n'
           );
        }
        #endregion

        #region IMessageLogger implementation
        void IMessageLogger.SendMessage(TestMessageLevel testMessageLevel, string message)
        {
            lock (_messages)
            {
                _messages.Add((testMessageLevel, message));
            }
        }
        #endregion
    }
}
