// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Specification of the deployment configuration: the (hardware) environment the test cases
    /// are (about to be) executed on. The deployment configuration should be created by a developer
    /// (or other tools), this class can only read the specification.
    /// </summary>
    public class DeploymentConfiguration
    {
        #region Properties
        /// <summary>
        /// The display name to use for the device the configuration is designed for
        /// </summary>
        public string DisplayName
        {
            get; set;
        }

        /// <summary>
        /// Configuration values of a type that is either <c>key = value</c> or <c>key = { file: [relative] path }</c>.
        /// </summary>
        [JsonProperty("Configuration")]
        [JsonConverter(typeof(ValuesConverter))]
        public Dictionary<string, (object value, ConfigurationFile file)> Values
        {
            get;
            set;
        }

        /// <summary>
        /// Information about the file that contains the configuration data.
        /// </summary>
        public sealed class ConfigurationFile
        {
            #region Properties
            /// <summary>
            /// Absolute path to the file.
            /// </summary>
            [JsonProperty("File")]
            public string FilePath
            {
                get;
                set;
            }
            #endregion

            #region Methods
            /// <summary>
            /// Read the content of the file as binary data.
            /// </summary>
            /// <returns>The data in the file, or <c>null</c> if the file does not exist.</returns>
            public byte[] ReadAsBinary()
            {
                if (FilePath is null || !File.Exists(FilePath))
                {
                    return null;
                }
                return File.ReadAllBytes(FilePath);
            }

            /// <summary>
            /// Read the content of the file as string.
            /// </summary>
            /// <returns>The text in the file, or <c>null</c> if the file does not exist.</returns>
            public string ReadAsText()
            {
                if (FilePath is null || !File.Exists(FilePath))
                {
                    return null;
                }
                return File.ReadAllText(FilePath);
            }
            #endregion
        }
        #endregion

        #region Json (de)serialization
        /// <summary>
        /// Parse the JSON representation of a <see cref="DeploymentConfiguration"/>.
        /// </summary>
        /// <param name="configurationFilePath">The path to the file with the deployment configuration.</param>
        /// <returns>The deployment configuration, or <c>null</c> if the file does not exist</returns>
        public static DeploymentConfiguration Parse(string configurationFilePath)
        {
            if (configurationFilePath is null || !File.Exists(configurationFilePath))
            {
                return null;
            }

            DeploymentConfiguration configuration = JsonConvert.DeserializeObject<DeploymentConfiguration>(File.ReadAllText(configurationFilePath));
            if (!(configuration?.Values is null))
            {
                string directoryName = Path.GetDirectoryName(configurationFilePath);
                foreach ((object _, ConfigurationFile file) in configuration.Values.Values)
                {
                    if (!(file?.FilePath is null))
                    {
                        file.FilePath = Path.GetFullPath(Path.Combine(directoryName, file.FilePath));
                    }
                }
            }

            return configuration;
        }

        /// <summary>
        /// Converter for the <see cref="Values"/> dictionary
        /// </summary>
        private class ValuesConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Dictionary<string, (string value, ConfigurationFile file)>);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var result = new Dictionary<string, (object value, ConfigurationFile file)>();

                if (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read();
                    while (reader.TokenType != JsonToken.EndObject)
                    {
                        if (reader.TokenType != JsonToken.PropertyName)
                        {
                            throw new JsonException();
                        }
                        string key = reader.Value.ToString();

                        reader.Read();
                        if (reader.TokenType == JsonToken.String)
                        {
                            result[key] = (reader.Value.ToString(), null);
                        }
                        else if (reader.TokenType == JsonToken.Integer)
                        {
                            result[key] = ((long)reader.Value, null);
                        }
                        else
                        {
                            result[key] = (null, serializer.Deserialize<ConfigurationFile>(reader));
                        }
                        reader.Read();
                    }
                }
                else
                {
                    throw new JsonException();
                }
                return result;
            }
        }
        #endregion

        #region Methods
        /// <summary>
        /// Get the part of the deployment configuration identified by a key.
        /// </summary>
        /// <param name="configurationKey">Key as used in the deployment configuration</param>
        /// <param name="resultType">Required return type. Allowed types are <c>byte[]</c>, <c>int</c>, <c>long</c> and <c>string</c></param>
        /// <returns>Returns the content of a file or (if the <paramref name="resultType"/> is not <c>byte[]</c>) a value if the deployment configuration
        /// contains data for the <paramref name="configurationKey"/>. Returns <c>null</c> (-1 for integer types) if no configuration
        /// data is specified, if the <paramref name="resultType"/> does not match the way the configuration is specified or
        /// if the <paramref name="configurationKey"/> is <c>null</c>.</returns>
        public object GetDeploymentConfigurationValue(string configurationKey, Type resultType)
        {
            if (!(configurationKey is null))
            {
                if (Values?.TryGetValue(configurationKey, out (object value, ConfigurationFile file) value) ?? false)
                {
                    if (resultType == typeof(string))
                    {
                        if (!(value.value is null))
                        {
                            return value.value?.ToString();
                        }
                        else
                        {
                            return value.file.ReadAsText();
                        }
                    }
                    else if (resultType == typeof(byte[]))
                    {
                        return value.file?.ReadAsBinary();
                    }
                    else if (resultType == typeof(int))
                    {
                        if (value.value is long longValue)
                        {
                            return (int)longValue;
                        }
                        else if (value.value is string stringValue)
                        {
                            if (int.TryParse(stringValue, out int intValue))
                            {
                                return intValue;
                            }
                        }
                    }
                    else if (resultType == typeof(long))
                    {
                        if (value.value is long longValue)
                        {
                            return longValue;
                        }
                        else if (value.value is string stringValue)
                        {
                            if (long.TryParse(stringValue, out longValue))
                            {
                                return longValue;
                            }
                        }
                    }
                }
            }
            return resultType == typeof(int) ? (object)(int)-1
                : resultType == typeof(long) ? (object)(long)-1
                : null;
        }
        #endregion
    }
}
