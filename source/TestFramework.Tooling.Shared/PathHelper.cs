// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;

namespace nanoFramework.TestFramework.Tooling
{
    internal static class PathHelper
    {
        /// <summary>
        /// Returns a relative path from one path to another.
        /// </summary>
        /// <param name="relativeTo">The source path the result should be relative to. This path is always considered to be a directory.</param>
        /// <param name="path">The destination path.</param>
        /// <returns>The relative path, or path if the paths don't share the same root.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="path"/> or <paramref name="relativeTo"/> are <c>null</c> or whitespace.</exception>
        /// <remarks>
        /// This method is present in .NET but not in .NET Framework
        /// </remarks>
        public static string GetRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrWhiteSpace(relativeTo))
            {
                throw new ArgumentNullException(nameof(relativeTo));
            }
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            if (!Path.IsPathRooted(path))
            {
                return path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            else
            {
                string fromAbsolutePath = Path.Combine(Path.GetFullPath(relativeTo), "dummy");
                string toAbsolutePath = Path.GetFullPath(path);
                if (!Path.GetPathRoot(fromAbsolutePath).Equals(Path.GetPathRoot(toAbsolutePath), StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
                return new Uri(fromAbsolutePath).MakeRelativeUri(new Uri(toAbsolutePath)).ToString().Replace('/', Path.DirectorySeparatorChar);
            }
        }
    }
}
