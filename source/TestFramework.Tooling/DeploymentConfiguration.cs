// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Specification of the deployment configuration: the (hardware and software) environment the tests
    /// are (about to be) executed on.
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
        public Dictionary<string, (string value, ConfigurationFile file)> Values
        {
            get;
            set;
        }

        /// <summary>
        /// Information about the file that contains the configuration data. This element can only be used for
        /// reading data from a configuration file if it is created via <see cref="Parse(string, string)"/>.
        /// </summary>
        public sealed class ConfigurationFile
        {
            #region Fields
            /// <summary>
            /// Absolute path of the configuration file.
            /// This field is assigned in <see cref="Parse(string, string)"/> and is not updated if
            /// <see cref="FilePath"/> is assigned.
            /// </summary>
            [JsonIgnore]
            internal string _absolutePath;
            #endregion

            #region Properties
            /// <summary>
            /// Path to the file. This can be a path relative to the directory the JSON specification resides in.
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
                if (_absolutePath is null || !File.Exists(_absolutePath))
                {
                    return null;
                }
                return File.ReadAllBytes(_absolutePath);
            }

            /// <summary>
            /// Read the content of the file as string.
            /// </summary>
            /// <returns>The text in the file, or <c>null</c> if the file does not exist.</returns>
            public string ReadAsText()
            {
                if (_absolutePath is null || !File.Exists(_absolutePath))
                {
                    return null;
                }
                return File.ReadAllText(_absolutePath);
            }
            #endregion
        }
        #endregion

        #region Json (de)serialization
        /// <summary>
        /// Parse the JSON representation of a <see cref="DeploymentConfiguration"/>.
        /// </summary>
        /// <param name="json">JSON configuration</param>
        /// <param name="jsonDirectoryPath">The path of the directory with the file the <paramref name="json"/>
        /// was read from. The path is used to resolve relative paths as value of <see cref="ConfigurationFile.FilePath"/>.</param>
        /// <param name="defaultConfiguration">The default configuration. If not <c>null</c>, it provides the
        /// initial configuration of which the properties are overwritten with the data in <paramref name="json"/>.</param>
        /// <returns></returns>
        public static DeploymentConfiguration Parse(string json, string jsonDirectoryPath, DeploymentConfiguration defaultConfiguration)
        {
            DeploymentConfiguration configuration = JsonConvert.DeserializeObject<DeploymentConfiguration>(json);
            if (!(configuration?.Values is null))
            {
                foreach ((string _, ConfigurationFile file) in configuration.Values.Values)
                {
                    if (!(file is null))
                    {
                        file._absolutePath = Path.Combine(jsonDirectoryPath ?? ".", file.FilePath);
                    }
                }
            }
            if (!(defaultConfiguration is null))
            {
                configuration.DisplayName ??= defaultConfiguration.DisplayName;
                if (!(defaultConfiguration.Values is null))
                {
                    foreach (KeyValuePair<string, (string value, ConfigurationFile file)> value in defaultConfiguration.Values)
                    {
                        configuration.Values ??= new Dictionary<string, (string value, ConfigurationFile file)>();
                        if (!configuration.Values.ContainsKey(value.Key))
                        {
                            configuration.Values[value.Key] = value.Value;
                        }
                    }
                }
            }
            return configuration;
        }

        /// <summary>
        /// Convert the configuration into JSON
        /// </summary>
        /// <returns>The JSON for this configuration</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            });
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
                var values = value as Dictionary<string, (string value, ConfigurationFile file)>;
                writer.WriteStartObject();

                foreach (KeyValuePair<string, (string value, ConfigurationFile file)> configurationValue in from v in values
                                                                                                            orderby v.Value.value is null ? 1 : 0, v.Key
                                                                                                            select v)
                {
                    writer.WritePropertyName(configurationValue.Key);

                    if (!(configurationValue.Value.value is null))
                    {
                        serializer.Serialize(writer, configurationValue.Value.value);
                    }
                    else if (!(configurationValue.Value.file?.FilePath is null))
                    {
                        serializer.Serialize(writer, configurationValue.Value.file);
                    }
                }
                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var result = new Dictionary<string, (string value, ConfigurationFile file)>();

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
        /// The result is either a string value, if the configuration is specified as key = value pair,
        /// or the textual content of a configuration file.
        /// </summary>
        /// <param name="configurationKey"></param>
        /// <returns>Returns the content of a text file or a string value if the deployment configuration
        /// contains data for the <paramref name="configurationKey"/>. Returns <c>null</c> if no configuration
        /// data is specified or if the <paramref name="configurationKey"/> is <c>null</c>.</returns>
        public string GetDeploymentConfigurationValue(string configurationKey)
        {
            if (!(configurationKey is null))
            {
                if (Values?.TryGetValue(configurationKey, out (string value, ConfigurationFile file) value) ?? false)
                {
                    if (!(value.value is null))
                    {
                        return value.value;
                    }
                    else
                    {
                        return value.file.ReadAsText();
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Get the part of the deployment configuration identified by a key.
        /// The result is binary data if a file has been specified for the key in the deployment configuration.
        /// </summary>
        /// <param name="configurationKey"></param>
        /// <returns>Returns the content of a binary file if the deployment configuration has specified a file
        /// for the <paramref name="configurationKey"/>. Returns <c>null</c> otherwise.</returns>
        public byte[] GetDeploymentConfigurationFile(string configurationKey)
        {
            if (!(configurationKey is null))
            {
                if (Values?.TryGetValue(configurationKey, out (string value, ConfigurationFile file) value) ?? false)
                {
                    if (!(value.file is null))
                    {
                        return value.file.ReadAsBinary();
                    }
                }
            }
            return null;
        }
        #endregion
    }
}
