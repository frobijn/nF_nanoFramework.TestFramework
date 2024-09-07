// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using nanoFramework.TestFramework;

namespace TestFramework.Tooling.Tests.Helpers
{
    /// <summary>
    /// Assumptions for the test: all serial ports used by real hardware devices
    /// have a name "COM#" with # < 30, and when the real hardware tests are run,
    /// any serial port named "COM#" with # < 30 is a real hardware device.
    /// It is also assumed that virtual devices use numbers between 30 - 50.
    /// </summary>
    public static class RealHardwareSerialPorts
    {
        #region Fields
        private const int MaxRealHardwareCOMNumber = 30;
        private const int MaxOtherCOMNumber = 50;
        #endregion

        /// <summary>
        /// Get serial port names that are assumed to be connected with
        /// a real hardware device.
        /// </summary>
        /// <param name="numberExisting">Number of ports to return. If there are less available,
        /// the test is marked as inconclusive.</param>
        /// <param name="numberNonExisting">Number of nonexisting ports to return</param>
        /// <returns></returns>
        public static IEnumerable<string> GetSerialPortNames(int numberExisting, int numberNonExisting)
        {
            int numberFound = 0;
            int highestPortNumber = MaxOtherCOMNumber;
            foreach (string name in SerialPort.GetPortNames())
            {
                Match match = s_splitPortName.Match(name);
                if (match.Success && int.TryParse(match.Groups["number"].Value, out int number))
                {
                    if (number < MaxRealHardwareCOMNumber && numberFound < numberExisting)
                    {
                        ++numberFound;
                        yield return name;

                        if (numberNonExisting == 0)
                        {
                            break;
                        }
                    }
                    if (number > highestPortNumber)
                    {
                        highestPortNumber = number;
                    }
                }
            }
            if (numberFound < numberExisting)
            {
                if (numberExisting == 1)
                {
                    Assert.Inconclusive($"The test requires a {Constants.RealHardware_Description} that does not seem to be connected: no COM# port with # < 30 is present.");
                }
                else
                {
                    Assert.Inconclusive($"The test requires more ({numberExisting}) {Constants.RealHardware_Description}s than available; only {numberFound} COM# port with # < 30 present.");
                }
            }
            for (int i = 0; i < numberNonExisting; i++)
            {
                yield return $"COM{highestPortNumber + i + 1}";
            }
        }

        /// <summary>
        /// Get serial port names that are assumed to be connected with
        /// a real hardware device.
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<string> GetAllSerialPortNames()
        {
            foreach (string name in SerialPort.GetPortNames())
            {
                Match match = s_splitPortName.Match(name);
                if (match.Success && int.TryParse(match.Groups["number"].Value, out int number))
                {
                    if (number < MaxRealHardwareCOMNumber)
                    {
                        yield return name;
                    }
                }
            }
        }
        private static readonly Regex s_splitPortName = new Regex(@"^(?<name>[A-Z]+)(?<number>[0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Get the ports excluded by the assumption about the ports of virtual devices 
        /// </summary>
        public static IEnumerable<string> ExcludeSerialPorts
        {
            get
            {
                for (int i = MaxRealHardwareCOMNumber; i <= MaxOtherCOMNumber; i++)
                {
                    yield return $"COM{i}";
                }
            }
        }
    }
}
