// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using nanoFramework.TestFramework.Tooling.TestFrameworkProxy;
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
        #region Fields
        /// <summary>
        /// File name to store the specification of the test cases to debug in.
        /// </summary>
        public const string SpecificationFileName = "SelectUnitTests.json";

        private string _specificationFileName;
        #endregion

        #region Properties
        /// <summary>
        /// The absolute path to the file that contains the deployment configuration for the device.
        /// </summary>
        [JsonProperty("DeploymentConfiguration")]
        public string DeploymentConfigurationFilePath
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
            /// Indicates whether to include all test methods
            /// of the test class.
            /// </summary>
            public bool AllMethods
            {
                get; set;
            }

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
            /// Indicates whether to include all <see cref="IDataRow"/>> attributes
            /// that are present for the test method.
            /// </summary>
            public bool AllDataRows
            {
                get; set;
            }

            /// <summary>
            /// The 0-based indices of the <see cref="IDataRow"/>> attributes
            /// to include.
            /// </summary>
            public List<int> DataRowAttributes
            {
                get; set;
            }
        }
        #endregion

        #region Json (de)serialization
        /// <summary>
        /// Parse the file with this specification
        /// </summary>
        /// <param name="specificationFilePath">Path to the specification file</param>
        /// <returns>The deserialized specification, or <c>null</c> if the file does not exist.</returns>
        public static DebugTestCasesSpecification Parse(string specificationFilePath)
        {
            if (specificationFilePath is null || !File.Exists(specificationFilePath))
            {
                return null;
            }
            DebugTestCasesSpecification specification = JsonConvert.DeserializeObject<DebugTestCasesSpecification>(File.ReadAllText(specificationFilePath));
            if (!(specification is null))
            {
                specification._specificationFileName = specificationFilePath;
                if (!(specification?.DeploymentConfigurationFilePath is null))
                {
                    specification.DeploymentConfigurationFilePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(specificationFilePath), specification.DeploymentConfigurationFilePath));
                }
            }
            return specification;
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
                throw new NotImplementedException();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {

                if (reader.TokenType == JsonToken.StartArray)
                {
                    var result = new TestMethodList()
                    {
                        TestMethods = new List<TestMethodSpecification>()
                    };
                    reader.Read();
                    while (reader.TokenType != JsonToken.EndArray)
                    {
                        result.TestMethods.Add(serializer.Deserialize(reader, typeof(TestMethodSpecification)) as TestMethodSpecification);
                        reader.Read();
                    }
                    return result;
                }
                else if (reader.TokenType == JsonToken.String && reader.Value.ToString() == "*")
                {
                    return new TestMethodList()
                    {
                        AllMethods = true
                    };
                }
                else
                {
                    throw new JsonException();
                }
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
                throw new NotImplementedException();
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
                    if (reader.TokenType == JsonToken.String && reader.Value.ToString() == "*")
                    {
                        result.AllDataRows = true;
                        reader.Read();
                    }
                    else if (reader.TokenType == JsonToken.StartArray)
                    {
                        result.DataRowAttributes = serializer.Deserialize(reader, typeof(List<int>)) as List<int>;
                        while (reader.TokenType != JsonToken.EndObject)
                        {
                            reader.Read();
                        }
                    }
                    else
                    {
                        throw new JsonException();
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
        /// <param name="testCases">All available test cases. Pass <c>null</c> if there are no test cases available.</param>
        /// <returns>The JSON schema</returns>
        public static string GenerateJsonSchema(TestCaseCollection testCases)
        {
            #region Group the test cases
            var allTestCases = new Dictionary<string, Dictionary<string, Dictionary<string, HashSet<int>>>>();
            if (!(testCases is null))
            {
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
                    IEnumerable<KeyValuePair<string, HashSet<int>>> dataRowMethods = from tm in perTestClass.Value
                                                                                     where tm.Value.Count > 1 || tm.Value.First() >= 0
                                                                                     select tm;
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
                    ""description"": ""Specify which data row attributes should be included. The first data row attribute in the code is 0, the next one is 1, etc. There are {dataRowMethod.Value.Max() + 1} attributes, so the numbers must be between 0 and {dataRowMethod.Value.Max()}. To select all, specify \""*\"" instead of an array of indices."",
                    ""type"": ""array"", ""items"": {{ ""anyOf"": [ {{ ""type"": ""string"", ""enum"": [ ""*"" ] }}, {{ ""type"": ""integer"", ""minimum"": 0, ""maximum"": {dataRowMethod.Value.Max()} }} ] }}
                }}");
                            comma = ",";
                        }
                        typeDefinitions.Append(@"
            }
        },");
                    }
                    #endregion

                    #region Methods in the class
                    bool hasTestMethods = (from tm in perTestClass.Value
                                           where tm.Value.Count == 1 || tm.Value.First() < 0
                                           select tm).Any();
                    string arrayItemsType;
                    if (dataRowMethodsType is null)
                    {
                        arrayItemsType = $@"""type"": ""string"", ""enum"": [ {string.Join(", ", from tm in perTestClass.Value
                                                                                                 orderby tm.Key
                                                                                                 select $"\"{tm.Key}\"")} ]";
                    }
                    else if (hasTestMethods)
                    {
                        arrayItemsType = $@"""anyOf"": [
                        {{ ""type"": ""string"", ""enum"": [ {string.Join(", ", from tm in perTestClass.Value
                                                                                where tm.Value.Count == 1 || tm.Value.First() < 0
                                                                                orderby tm.Key
                                                                                select $"\"{tm.Key}\"")} ] }},
                        {{ ""$ref"": ""#/definitions/{dataRowMethodsType}"" }}
                    ]";
                    }
                    else
                    {
                        arrayItemsType = $@"""$ref"": ""#/definitions/{dataRowMethodsType}""";
                    }
                    classTypes[perTestClass.Key] = $@"{(classTypes.Count == 0 ? "" : ",")}
                ""{perTestClass.Key}"": {{
                    ""description"": ""Specify which test methods should be included. List the names (and data row attributes) of the methods in an array, or specify \""*\"" to include all test methods."",
                    ""anyOf"": [ {{ ""type"": ""string"", ""enum"": [ ""*"" ] }}, {{ ""type"": ""array"", ""items"": {{ {arrayItemsType} }} ]
                }}";
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
        ""DeploymentConfiguration"": {{
            ""description"": ""The path to the file that contains the deployment configuration. The path can be absolute or relative to the directory this specification file resides in."",
            ""type"": ""string""
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

        #region Select test cases
        /// <summary>
        /// Select the test cases from all available test cases according to this specification,
        /// to be used as input for <see cref="UnitTestLauncherGenerator.UnitTestLauncherGenerator(IEnumerable{TestCaseSelection}, bool, LogMessenger)"/>.
        /// </summary>
        /// <param name="testCases">All available test cases.</param>
        /// <param name="logger">Logger to report test methods that have not been found. Pass <c>null</c> if that is nor required.</param>
        /// <param name="logForMSBuild">Indicates whether MSBuild-style messages should be logged.</param>
        /// <returns>The selection of test cases. If two test cases for the same test method/data row attribute
        /// match the criterion (= same test method, but for different devices), only one is included in the selection. As the <see cref="UnitTestLauncherGenerator"/>
        /// does not use the device dependent information, it does not matter which one is selected.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="deviceSpecification"/> is <c>null</c>.</exception>
        public IEnumerable<TestCaseSelection> SelectTestCases(TestCaseCollection testCases, LogMessenger logger, bool logForMSBuild)
        {
            if (!(TestCases is null) && !(testCases is null))
            {
                #region Determine which methods to select
                var testClassFQN = new Dictionary<string, bool>();
                var testMethodFQN = new Dictionary<string, bool>();
                var dataRowFQN = new Dictionary<string, Dictionary<int, bool>>();
                foreach (KeyValuePair<string, Dictionary<string, TestMethodList>> ns in TestCases)
                {
                    foreach (KeyValuePair<string, TestMethodList> cls in ns.Value)
                    {
                        if (cls.Value.AllMethods)
                        {
                            testClassFQN[$"{ns.Key}.{cls.Key}"] = false;
                        }
                        else
                        {
                            foreach (TestMethodSpecification testMethod in cls.Value.TestMethods)
                            {
                                string fqn = $"{ns.Key}.{cls.Key}.{testMethod.MethodName}";
                                if (testMethod.DataRowAttributes is null || testMethod.AllDataRows)
                                {
                                    testMethodFQN[fqn] = false;
                                }
                                else
                                {
                                    if (!dataRowFQN.TryGetValue(fqn, out Dictionary<int, bool> indices))
                                    {
                                        dataRowFQN[fqn] = indices = new Dictionary<int, bool>();
                                    }
                                    foreach (int index in testMethod.DataRowAttributes)
                                    {
                                        indices[index] = false;
                                    }
                                }
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
                    var selectorCache = new Dictionary<TestOnRealHardwareProxy, bool>();

                    foreach (TestCase testCase in selection.Value)
                    {
                        string testCaseFQN = $"{testCase.FullyQualifiedName}#{testCase.DataRowIndex}";
                        if (fqnIncluded.Add(testCaseFQN))
                        {
                            bool include = false;
                            if (testClassFQN.ContainsKey(testCase.Group.FullyQualifiedName))
                            {
                                include = true;
                                testClassFQN[testCase.Group.FullyQualifiedName] = true;
                            }
                            if (testMethodFQN.ContainsKey(testCase.FullyQualifiedName))
                            {
                                include = true;
                                testMethodFQN[testCase.FullyQualifiedName] = true;
                            }
                            else if (testCase.DataRowIndex >= 0
                                    && dataRowFQN.TryGetValue(testCase.FullyQualifiedName, out Dictionary<int, bool> indices)
                                    && indices.ContainsKey(testCase.DataRowIndex))
                            {
                                include = true;
                                indices[testCase.DataRowIndex] = true;
                            }
                            if (include)
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

                #region Report missing test cases
                if (!(logger is null))
                {
                    string prefix = !logForMSBuild || _specificationFileName is null
                        ? ""
                        : $"{Path.GetFullPath(_specificationFileName)}(0,0): Error: ";

                    foreach (string fqn in from tc in testClassFQN
                                           where !tc.Value
                                           orderby tc.Key
                                           select tc.Key)
                    {
                        int idx = fqn.LastIndexOf('.');
                        logger(LoggingLevel.Error, $"{prefix}No test cases found for test class '{fqn.Substring(idx + 1)}' in namespace '{(idx < 0 ? "" : fqn.Substring(0, idx))}'.");
                    }

                    foreach (string fqn in from tc in testMethodFQN
                                           where !tc.Value
                                           orderby tc.Key
                                           select tc.Key)
                    {
                        int idx = fqn.LastIndexOf('.');
                        int idx2 = idx < 1 ? -1 : fqn.LastIndexOf('.', idx - 1);
                        logger(LoggingLevel.Error, $"{prefix}No test case found for test method '{fqn.Substring(idx + 1)}' of test class '{fqn.Substring(idx2 + 1, idx - idx2 - 1)}' in namespace '{(idx2 < 0 ? "" : fqn.Substring(0, idx2))}'.");
                    }

                    foreach (KeyValuePair<string, Dictionary<int, bool>> tm in from tc in dataRowFQN
                                                                               where (from i in tc.Value
                                                                                      where !i.Value
                                                                                      select i).Any()
                                                                               orderby tc.Key
                                                                               select tc)
                    {
                        int idx = tm.Key.LastIndexOf('.');
                        int idx2 = idx < 1 ? -1 : tm.Key.LastIndexOf('.', idx - 1);
                        string indices = string.Join(", ", from i in tm.Value
                                                           where !i.Value
                                                           orderby i.Key
                                                           select $"#{i.Key}");
                        logger(LoggingLevel.Error, $"{prefix}No test case found for data attributes {indices} of test method '{tm.Key.Substring(idx + 1)}' of test class '{tm.Key.Substring(idx2 + 1, idx - idx2 - 1)}' in namespace '{(idx2 < 0 ? "" : tm.Key.Substring(0, idx2))}'.");
                    }
                }
                #endregion
            }
        }
        #endregion

    }
}
