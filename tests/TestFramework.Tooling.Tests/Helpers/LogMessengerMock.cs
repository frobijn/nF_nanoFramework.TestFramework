// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using nanoFramework.TestFramework.Tooling;

namespace TestFramework.Tooling.Tests.Helpers
{
    public sealed class LogMessengerMock
    {
        public IReadOnlyList<(LoggingLevel level, string message)> Messages
            => _messages;
        private readonly List<(LoggingLevel level, string message)> _messages = new List<(LoggingLevel level, string message)>();

        public static implicit operator LogMessenger(LogMessengerMock mock)
        {
            return (level, message) =>
                    mock._messages.Add((level, message));
        }
    }
}
