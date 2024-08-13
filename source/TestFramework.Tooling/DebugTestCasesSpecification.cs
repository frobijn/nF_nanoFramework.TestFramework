// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace nanoFramework.TestFramework.Tooling
{
    /// <summary>
    /// Specification of a selection of test cases via JSON. It is intended
    /// to be specified by a developer as part of a nanoFramework application
    /// project, created to debug one or more unit tests.
    /// </summary>
    public sealed class DebugTestCasesSpecification
    {
        #region Properties
        [JsonProperty("$schema")]
        public string SchemaUri
        {
            get; set;
        }

        /// <summary>
        /// Indicates whether to include tests that should be run on the virtual device.
        /// The default is <c>true</c>.
        /// </summary>
        public bool? ToBeRunOnVirtualDevice
        {
            get; set;
        }

        /// <summary>
        /// Indicates whether to include tests that should be run on real hardware.
        /// The default is <c>true</c>.
        /// </summary>
        public bool? ToBeRunOnRealHardware
        {
            get; set;
        }

        /// <summary>
        /// Selection of test cases
        /// </summary>
        public Dictionary<string, Dictionary<string, TestMethodList>> TestCases
        {
            get; set;
        }

        /// <summary>
        /// List of the test methods
        /// </summary>
        [JsonConverter(typeof(TestMethodListConverter))]
        public sealed class TestMethodList
        {
            /// <summary>
            /// Get the methods in the list
            /// </summary>
            public List<TestMethodSpecification> TestMethods
            {
                get; set;
            }
        }

        /// <summary>
        /// Specification of test cases per test method
        /// </summary>
        [JsonConverter(typeof(TestMethodSpecificationConverter))]
        public sealed class TestMethodSpecification
        {
            /// <summary>
            /// Name of the test method
            /// </summary>
            public string MethodName
            {
                get; set;
            }

            /// <summary>
            /// The 0-based indices of the <see cref="IDataRow"/>> attributes
            /// to include. If not specified, all attributes are included
            /// if they are present.
            /// </summary>
            public List<int> DataRowAttributes
            {
                get; set;
            }
        }
        #endregion

        #region Select test cases
        /// <summary>
        /// Select the test cases from all available test cases according to this specification,
        /// to be used as input for <see cref="UnitTestLauncherGenerator.UnitTestLauncherGenerator(IEnumerable{TestCaseSelection}, bool, LogMessenger)"/>.
        /// </summary>
        /// <param name="testCases">All available test cases</param>
        /// <returns>The selection of test cases. If two test cases for the same test method/data row attribute
        /// match the criterion, only one is included in the selection. As the <see cref="UnitTestLauncherGenerator"/>
        /// does not use the device dependent information, it does not matter which one is selected.</returns>
        public IEnumerable<TestCaseSelection> SelectTestCases(TestCaseCollection testCases)
        {
            #region Determine which methods to select
            var testMethodFQN = new HashSet<string>();
            var dataRowFQN = new Dictionary<string, HashSet<int>>();
            foreach (KeyValuePair<string, Dictionary<string, TestMethodList>> ns in TestCases)
            {
                foreach (KeyValuePair<string, TestMethodList> cls in ns.Value)
                {
                    foreach (TestMethodSpecification testMethod in cls.Value.TestMethods)
                    {
                        string fqn = $"{ns.Key}.{cls.Key}.{testMethod.MethodName}";
                        if (testMethod.DataRowAttributes is null)
                        {
                            testMethodFQN.Add(fqn);
                        }
                        else
                        {
                            if (!dataRowFQN.TryGetValue(fqn, out HashSet<int> indices))
                            {
                                dataRowFQN[fqn] = indices = new HashSet<int>();
                            }
                            indices.UnionWith(testMethod.DataRowAttributes);
                        }
                    }
                }
            }
            #endregion

            #region Find the methods in the test cases
            int selectionIndex = 0;
            foreach (KeyValuePair<string, List<TestCase>> selection in testCases.TestCases
                                                                                    .GroupBy(tc => tc.AssemblyFilePath)
                                                                                    .ToDictionary(
                                                                                        g => g.Key,
                                                                                        g => g.ToList()))
            {
                TestCaseSelection result = null;
                var fqnIncluded = new HashSet<string>();

                foreach (TestCase testCase in selection.Value)
                {
                    if (!(ToBeRunOnVirtualDevice ?? true) && testCase.ShouldRunOnVirtualDevice)
                    {
                        continue;
                    }
                    if (!(ToBeRunOnRealHardware ?? true) && testCase.ShouldRunOnRealHardware)
                    {
                        continue;
                    }

                    string testCaseFQN = $"{testCase.FullyQualifiedName}#{testCase.DataRowIndex}";
                    if (fqnIncluded.Add(testCaseFQN))
                    {
                        if (testMethodFQN.Contains(testCase.FullyQualifiedName))
                        {
                            result ??= new TestCaseSelection(selection.Key);
                            result._testCases.Add((++selectionIndex, testCase));
                        }
                        else if (dataRowFQN.TryGetValue(testCase.FullyQualifiedName, out HashSet<int> indices)
                                && indices.Contains(testCase.DataRowIndex))
                        {
                            result ??= new TestCaseSelection(selection.Key);
                            result._testCases.Add((++selectionIndex, testCase));
                        }
                    }
                }

                if (!(result is null))
                {
                    yield return result;
                }
            }
            #endregion
        }
        #endregion

        #region Json (de)serialization
        /// <summary>
        /// Parse the JSON representation of a <see cref="DebugTestCasesSpecification"/>.
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static DebugTestCasesSpecification Parse(string json)
        {
            return JsonConvert.DeserializeObject<DebugTestCasesSpecification>(json) as DebugTestCasesSpecification;
        }

        /// <summary>
        /// Convert the specification into JSON
        /// </summary>
        /// <returns></returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, new JsonSerializerSettings()
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
            });
        }



        /// <summary>
        /// Converter so that a test method can be specified either by its name
        /// or by its name and data row attribute indices.
        /// </summary>
        private class TestMethodListConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(TestMethodList);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var list = value as TestMethodList;
                writer.WriteStartArray();

                if ((list.TestMethods?.Count ?? 0) > 0)
                {
                    foreach (TestMethodSpecification method in list.TestMethods)
                    {
                        serializer.Serialize(writer, method);
                    }
                }

                writer.WriteEndArray();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var result = new TestMethodList()
                {
                    TestMethods = new List<TestMethodSpecification>()
                };
                if (reader.TokenType == JsonToken.StartArray)
                {
                    reader.Read();
                    while (reader.TokenType != JsonToken.EndArray)
                    {
                        result.TestMethods.Add(serializer.Deserialize(reader, typeof(TestMethodSpecification)) as TestMethodSpecification);
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

        /// <summary>
        /// Converter so that a test method can be specified either by its name
        /// or by its name and data row attribute indices.
        /// </summary>
        private class TestMethodSpecificationConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(TestMethodSpecification);
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var perMethod = value as TestMethodSpecification;

                if ((perMethod.DataRowAttributes?.Count ?? 0) == 0)
                {
                    serializer.Serialize(writer, perMethod.MethodName);
                }
                else
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName(perMethod.MethodName);
                    serializer.Serialize(writer, perMethod.DataRowAttributes);
                    writer.WriteEndObject();
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    return new TestMethodSpecification()
                    {
                        MethodName = reader.Value.ToString(),
                    };
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    reader.Read();
                    if (reader.TokenType != JsonToken.PropertyName)
                    {
                        throw new JsonException();
                    }
                    var result = new TestMethodSpecification()
                    {
                        MethodName = reader.Value.ToString(),
                    };
                    reader.Read();
                    if (reader.TokenType != JsonToken.StartArray)
                    {
                        throw new JsonException();
                    }
                    result.DataRowAttributes = serializer.Deserialize(reader, typeof(List<int>)) as List<int>;
                    while (reader.TokenType != JsonToken.EndObject)
                    {
                        reader.Read();
                    }
                    return result;
                }
                else
                {
                    throw new JsonException();
                }
            }
        }
        #endregion

        #region JSON schema
        /// <summary>
        /// Generate a JSON schema for the test cases, to assist the developer
        /// entering the correct values.
        /// </summary>
        /// <param name="testCases"></param>
        /// <returns></returns>
        public static string GenerateJsonSchema(TestCaseCollection testCases)
        {
            #region Group the test cases
            var allTestCases = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<int>>>>();
            foreach (TestCase testCase in from t in testCases.TestCases
                                          orderby t.Group.FullyQualifiedName
                                          select t)
            {
                int idx = testCase.Group.FullyQualifiedName.LastIndexOf('.');
                string namespaceName = idx < 0 ? "(none)" : testCase.Group.FullyQualifiedName.Substring(0, idx);
                if (!allTestCases.TryGetValue(namespaceName, out Dictionary<string, Dictionary<string, HashSet<int>>> classList))
                {
                    allTestCases[namespaceName] = classList = new Dictionary<string, Dictionary<string, HashSet<int>>>();
                }

                string className = idx < 0 ? testCase.Group.FullyQualifiedName : testCase.Group.FullyQualifiedName.Substring(idx + 1);
                if (!classList.TryGetValue(className, out Dictionary<string, HashSet<int>> methodList))
                {
                    classList[className] = methodList = new Dictionary<string, HashSet<int>>();
                }

                string methodName = testCase.FullyQualifiedName.Substring(testCase.FullyQualifiedName.LastIndexOf('.') + 1);
                if (!methodList.TryGetValue(methodName, out HashSet<int> dataRowIndexList))
                {
                    methodList[methodName] = dataRowIndexList = new HashSet<int>();
                }
                dataRowIndexList.Add(testCase.DataRowIndex);
            }
            #endregion


            #region Create the schema types for namespaces and methods
            int typeIndex = 0;
            var typeDefinitions = new StringBuilder();
            var namespaceTypes = new Dictionary<string, string>();
            foreach (KeyValuePair<string, Dictionary<string, Dictionary<string, HashSet<int>>>> perNamespace in allTestCases)
            {
                var classTypes = new Dictionary<string, string>();

                foreach (KeyValuePair<string, Dictionary<string, HashSet<int>>> perTestClass in perNamespace.Value)
                {
                    #region Test methods with data row attributes
                    IEnumerable<KeyValuePair<string, HashSet<int>>> dataRowMethods = from pm in perTestClass.Value
                                                                                     where pm.Value.Count > 1 || pm.Value.First() >= 0
                                                                                     select pm;
                    string dataRowMethodsType = null;
                    if (dataRowMethods.Any())
                    {
                        dataRowMethodsType = $"TestMethod_{++typeIndex}";
                        typeDefinitions.Append($@",
        ""{dataRowMethodsType}"": {{
            ""type"": ""object"",
            ""properties"": {{");

                        string comma = "";
                        foreach (KeyValuePair<string, HashSet<int>> dataRowMethod in dataRowMethods)
                        {
                            typeDefinitions.Append($@"{comma}
                ""{dataRowMethod.Key}"": {{
                    ""type"": ""array"", ""items"": {{ ""type"": ""integer"", ""minimum"": 0, ""maximum"": {dataRowMethod.Value.Max()} }},
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are {dataRowMethod.Value.Max() + 1} attributes, so the numbers must be between 0 and {dataRowMethod.Value.Max()}. To select all, list the method name instead of this object."",
                }}");
                            comma = ",";
                        }
                        typeDefinitions.Append(@"
            }
        },");
                    }
                    #endregion

                    #region Methods in the class
                    if (dataRowMethodsType is null)
                    {
                        classTypes[perTestClass.Key] = $@"
                ""{perTestClass.Key}"": {{
                    ""type"": ""array"", ""items"":
                        {{ ""type"": ""string"", ""enum"": [ {string.Join(", ", from tm in perTestClass.Value
                                                                                orderby tm.Key
                                                                                select $"\"{tm.Key}\"")} ] }},
                    ""description"": ""Specify which test methods should be included."",
                }}";
                    }
                    else
                    {
                        classTypes[perTestClass.Key] = $@"{(classTypes.Count == 0 ? "" : ",")}
                ""{perTestClass.Key}"": {{
                    ""type"": ""array"", ""items"": {{ ""anyOf"": [
                        {{ ""type"": ""string"", ""enum"": [ {string.Join(", ", from tm in perTestClass.Value
                                                                                orderby tm.Key
                                                                                select $"\"{tm.Key}\"")} ] }},
                        {{ ""$ref"": ""#/definitions/{dataRowMethodsType}"" }}
                    ] }},
                    ""description"": ""Specify which test methods should be included."",
                }}";
                    }
                    #endregion
                }

                #region Classes in the namespace
                string namespaceType = $"Namespace_{++typeIndex}";
                namespaceTypes[perNamespace.Key] = namespaceType;
                typeDefinitions.Append($@",
        ""{namespaceType}"": {{
            ""type"": ""object"",
            ""description"": ""Test cases for classes within the namespace. Select the classes to include."",
            ""properties"": {{{string.Join(",", from c in classTypes
                                                orderby c.Key
                                                select c.Value)}
            }}
        }}");
                #endregion
            }
            #endregion

            #region Generate the schema
            return $@"{{
    ""$schema"": ""http://json-schema.org/draft-07/schema"",
    ""type"": ""object"",
    ""description"": ""Specification of the test cases to be run."",
    ""properties"": {{
        ""{nameof(ToBeRunOnVirtualDevice)}"": {{
            ""description"": ""Indicates whether to include tests that should be run on the virtual device. If omitted, the tests are included."",
            ""type"": ""boolean""
        }},
        ""{nameof(ToBeRunOnRealHardware)}"": {{
            ""description"": ""Indicates whether to include tests that should be run on real hardware. If omitted, the tests are included."",
            ""type"": ""boolean""
        }},
        ""TestCases"": {{
            ""$ref"": ""#/definitions/Namespaces_0""
        }}
    }},
    ""required"": [ ""TestCases"" ],
    ""definitions"": {{
        ""Namespaces_0"": {{
            ""type"": ""object"",
            ""description"": ""The selection of test cases, grouped by namespace and test class name."",
            ""properties"": {{{string.Join(",", from ns in namespaceTypes
                                                orderby ns.Key
                                                select $@"
                ""{ns.Key}"": {{ ""$ref"": ""#/definitions/{ns.Value}"" }}")}
            }}
        }}{typeDefinitions}
    }}
}}";
            #endregion
        }
        #endregion
    }
}
