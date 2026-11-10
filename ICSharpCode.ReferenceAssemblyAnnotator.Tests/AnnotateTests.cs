// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator.Tests
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Rocks;
    using Xunit;
    using static CecilBuilder;

    public class AnnotateTests
    {
        private readonly ModuleDefinition _module = CreateModule("Unannotated");
        private readonly ModuleDefinition _annotatedModule = CreateModule("Annotated");
        private readonly TypeDefinition _nullable;
        private readonly TypeDefinition _annotatedNullable;
        private readonly TypeDefinition _disallowNull;
        private readonly TypeDefinition _annotatedDisallowNull;
        private readonly Dictionary<string, TypeDefinition> _attributesOfInterest;

        public AnnotateTests()
        {
            // As in Program.Main, the attributes of interest are types the unannotated module can reference.
            _nullable = AddAttributeType(_module, NullableAttribute, _module.TypeSystem.Byte);
            _annotatedNullable = AddAttributeType(_annotatedModule, NullableAttribute, _annotatedModule.TypeSystem.Byte);
            _disallowNull = AddAttributeType(_module, DisallowNullAttribute);
            _annotatedDisallowNull = AddAttributeType(_annotatedModule, DisallowNullAttribute);
            _attributesOfInterest = new Dictionary<string, TypeDefinition>
            {
                [NullableAttribute] = _nullable,
                [DisallowNullAttribute] = _disallowNull,
            };
        }

        [Fact]
        public void AnnotateType_MatchesByRefGenericOverloads()
        {
            // Regression: these overloads used to compare equal, so FindMatchingMethod threw.
            var type = AddType(_module, "N", "C");
            var boolOverload = AddMethod(type, "M", Nullable(_module, _module.TypeSystem.Boolean).MakeByReferenceType());
            var byteOverload = AddMethod(type, "M", Nullable(_module, _module.TypeSystem.Byte).MakeByReferenceType());

            var annotatedType = AddType(_annotatedModule, "N", "C");
            var annotatedBool = AddMethod(annotatedType, "M", Nullable(_annotatedModule, _annotatedModule.TypeSystem.Boolean).MakeByReferenceType());
            var annotatedByte = AddMethod(annotatedType, "M", Nullable(_annotatedModule, _annotatedModule.TypeSystem.Byte).MakeByReferenceType());
            AddAttribute(annotatedBool.Parameters[0], _annotatedNullable, NullableFlag(1));
            AddAttribute(annotatedByte.Parameters[0], _annotatedNullable, NullableFlag(2));

            Program.AnnotateType(null, _module, type, _annotatedModule, _attributesOfInterest);

            Assert.Equal((byte)1, SingleNullableFlag(boolOverload.Parameters[0]));
            Assert.Equal((byte)2, SingleNullableFlag(byteOverload.Parameters[0]));
        }

        [Fact]
        public void Annotate_ReplacesAttributesOfInterestAndKeepsOthers()
        {
            var keep = AddAttributeType(_module, "N.KeepAttribute");
            var parameter = AddMethod(AddType(_module, "N", "C"), "M", _module.TypeSystem.String).Parameters[0];
            AddAttribute(parameter, _nullable, NullableFlag(1));
            AddAttribute(parameter, keep);

            var annotatedParameter = AddMethod(AddType(_annotatedModule, "N", "C"), "M", _annotatedModule.TypeSystem.String).Parameters[0];
            AddAttribute(annotatedParameter, _annotatedNullable, NullableFlag(2));

            Program.Annotate(_module, parameter, annotatedParameter, _attributesOfInterest);

            Assert.Equal((byte)2, SingleNullableFlag(parameter));
            Assert.Contains(parameter.CustomAttributes, attribute => attribute.AttributeType == keep);
            Assert.Equal(2, parameter.CustomAttributes.Count);
        }

        [Theory]
        [InlineData("EqualityComparer`1")]
        [InlineData("IEqualityComparer`1")]
        public void AnnotateType_DropsDisallowNullOnGetHashCode(string typeName)
        {
            var type = AddType(_module, "System.Collections.Generic", typeName, genericArity: 1);
            var getHashCode = AddMethod(type, "GetHashCode", type.GenericParameters[0]);
            AddAttribute(getHashCode.Parameters[0], _disallowNull);

            var annotatedType = AddType(_annotatedModule, "System.Collections.Generic", typeName, genericArity: 1);
            var annotatedParameter = AddMethod(annotatedType, "GetHashCode", annotatedType.GenericParameters[0]).Parameters[0];
            AddAttribute(annotatedParameter, _annotatedDisallowNull);
            AddAttribute(annotatedParameter, _annotatedNullable, NullableFlag(1));

            Program.AnnotateType(null, _module, type, _annotatedModule, _attributesOfInterest);

            Assert.DoesNotContain(getHashCode.Parameters[0].CustomAttributes, attribute => attribute.AttributeType.FullName == DisallowNullAttribute);
            Assert.Equal((byte)1, SingleNullableFlag(getHashCode.Parameters[0]));
        }

        private static byte SingleNullableFlag(ICustomAttributeProvider provider)
            => (byte)provider.CustomAttributes.Single(attribute => attribute.AttributeType.FullName == NullableAttribute).ConstructorArguments[0].Value;

        private CustomAttributeArgument NullableFlag(byte value)
            => new CustomAttributeArgument(_annotatedModule.TypeSystem.Byte, value);
    }
}
