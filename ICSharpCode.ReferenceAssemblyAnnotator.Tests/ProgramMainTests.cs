// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator.Tests
{
    using System;
    using System.Collections.Immutable;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Mono.Cecil;
    using Xunit;

    /// <summary>
    /// Runs <see cref="Program.Main"/> end to end: annotates netstandard2.0's netstandard.dll and reads the output back.
    /// </summary>
    public class ProgramMainTests
    {
        private const string CodeAnalysis = "System.Diagnostics.CodeAnalysis";

        private static readonly Lazy<ModuleDefinition> Annotated = new Lazy<ModuleDefinition>(AnnotateNetStandard);

        [Fact]
        public void CopiesMemberNotNullWhen()
        {
            var isFaulted = GetType("System.Threading.Tasks", "Task").Properties.Single(property => property.Name == "IsFaulted");

            var attribute = GetAttribute(isFaulted, "MemberNotNullWhenAttribute");

            Assert.Equal(new object[] { true, "Exception" }, attribute.ConstructorArguments.Select(argument => argument.Value));
        }

        [Fact]
        public void CopiesDynamicallyAccessedMembersWithEnumArgument()
        {
            var getMethods = GetType("System", "Type").Methods.Single(method => method.Name == "GetMethods" && method.Parameters.Count == 1);

            var attribute = GetAttribute(getMethods, "DynamicallyAccessedMembersAttribute");

            // PublicMethods | NonPublicMethods
            Assert.Equal(8 | 16, (int)attribute.ConstructorArguments.Single().Value);
        }

        [Theory]
        [InlineData("System.Reflection", "Assembly", "GetTypes", "RequiresUnreferencedCodeAttribute")]
        [InlineData("System", "Type", "MakeGenericType", "RequiresDynamicCodeAttribute")]
        [InlineData("System.Reflection", "Assembly", "GetFiles", "RequiresAssemblyFilesAttribute")]
        public void CopiesRequiresAttributes(string @namespace, string typeName, string methodName, string attributeName)
        {
            var method = GetType(@namespace, typeName).Methods.First(method => method.Name == methodName);

            Assert.NotEmpty(GetAttribute(method, attributeName).ConstructorArguments);
        }

        [Fact]
        public void AssemblyResolver_LoadsDependenciesWithItself()
        {
            string netStandard = GetMetadata("UnannotatedNetStandard");
            using var resolver = new AssemblyResolver(ImmutableArray.Create(Path.GetDirectoryName(netStandard)!));

            // The System.Runtime facade forwards Object to netstandard, which must not come from the test host's runtime.
            var facade = resolver.Resolve(AssemblyNameReference.Parse("System.Runtime"))!;

            Assert.Equal(Path.GetFullPath(netStandard), facade.MainModule.TypeSystem.Object.Resolve().Module.FileName, ignoreCase: true);
        }

        [Theory]
        [InlineData("MemberNotNullAttribute")]
        [InlineData("MemberNotNullWhenAttribute")]
        [InlineData("RequiresUnreferencedCodeAttribute")]
        [InlineData("RequiresDynamicCodeAttribute")]
        [InlineData("RequiresAssemblyFilesAttribute")]
        [InlineData("DynamicallyAccessedMembersAttribute")]
        [InlineData("DynamicallyAccessedMemberTypes")]
        public void SynthesizesInternalTypes(string typeName)
        {
            Assert.False(GetType(CodeAnalysis, typeName).IsPublic);
        }

        private static TypeDefinition GetType(string @namespace, string name)
            => Annotated.Value.GetType(@namespace, name) ?? throw new InvalidOperationException($"{@namespace}.{name} not found");

        private static CustomAttribute GetAttribute(Mono.Cecil.ICustomAttributeProvider provider, string name)
            => provider.CustomAttributes.Single(attribute => attribute.AttributeType.FullName == $"{CodeAnalysis}.{name}");

        private static string GetMetadata(string key)
            => typeof(ProgramMainTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(attribute => attribute.Key == key).Value!;

        private static ModuleDefinition AnnotateNetStandard()
        {
            string input = GetMetadata("UnannotatedNetStandard");
            string output = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
            try
            {
                Program.Main(null, input, ImmutableArray.Create(Path.GetDirectoryName(input)!), GetMetadata("AnnotatedNetStandard"), output);
                return ModuleDefinition.ReadModule(new MemoryStream(File.ReadAllBytes(output)));
            }
            finally
            {
                File.Delete(output);
            }
        }
    }
}
