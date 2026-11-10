// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator.Tests
{
    using Mono.Cecil;
    using Mono.Cecil.Rocks;
    using Xunit;
    using static CecilBuilder;

    public class EquivalenceComparersTests
    {
        private readonly ModuleDefinition _module = CreateModule("A");
        private readonly ModuleDefinition _otherModule = CreateModule("B");

        [Fact]
        public void ByReference_DistinguishesGenericArguments()
        {
            // Regression: GetElementType() on Nullable<bool>& returns the open Nullable`1.
            var x = Nullable(_module, _module.TypeSystem.Boolean).MakeByReferenceType();
            var y = Nullable(_module, _module.TypeSystem.Byte).MakeByReferenceType();

            Assert.False(EquivalenceComparers.TypeReference.Equals(x, y));
        }

        [Fact]
        public void Pointer_DistinguishesGenericArguments()
        {
            var x = Nullable(_module, _module.TypeSystem.Boolean).MakePointerType();
            var y = Nullable(_module, _module.TypeSystem.Byte).MakePointerType();

            Assert.False(EquivalenceComparers.TypeReference.Equals(x, y));
        }

        [Fact]
        public void Array_DistinguishesGenericArguments()
        {
            var x = Nullable(_module, _module.TypeSystem.Boolean).MakeArrayType();
            var y = Nullable(_module, _module.TypeSystem.Byte).MakeArrayType();

            Assert.False(EquivalenceComparers.TypeReference.Equals(x, y));
        }

        [Fact]
        public void Array_DistinguishesRank()
        {
            var x = _module.TypeSystem.Int32.MakeArrayType(1);
            var y = _module.TypeSystem.Int32.MakeArrayType(2);

            Assert.False(EquivalenceComparers.TypeReference.Equals(x, y));
        }

        [Fact]
        public void SameShapeFromDifferentModules_AreEqual()
        {
            var x = Nullable(_module, _module.TypeSystem.Boolean).MakeByReferenceType().MakeArrayType();
            var y = Nullable(_otherModule, _otherModule.TypeSystem.Boolean).MakeByReferenceType().MakeArrayType();

            Assert.True(EquivalenceComparers.TypeReference.Equals(x, y));
            Assert.Equal(EquivalenceComparers.TypeReference.GetHashCode(x), EquivalenceComparers.TypeReference.GetHashCode(y));
        }

        [Fact]
        public void Nested_DistinguishedFromTopLevel()
        {
            var outer = AddType(_module, "N", "Outer");
            var nested = new TypeDefinition(string.Empty, "Inner", TypeAttributes.NestedPublic, _module.TypeSystem.Object);
            outer.NestedTypes.Add(nested);
            var topLevel = AddType(_otherModule, string.Empty, "Inner");

            Assert.False(EquivalenceComparers.TypeReference.Equals(nested, topLevel));
            Assert.False(EquivalenceComparers.TypeReference.Equals(topLevel, nested));
        }

        [Fact]
        public void GenericArity_Matters()
        {
            var x = AddType(_module, "N", "C");
            var y = AddType(_otherModule, "N", "C", genericArity: 1);

            Assert.False(EquivalenceComparers.TypeReference.Equals(x, y));
            Assert.False(EquivalenceComparers.TypeDefinition.Equals(x, y));
        }

        [Fact]
        public void MethodDefinition_DistinguishesByRefGenericOverloads()
        {
            var type = AddType(_module, "N", "C");
            var boolOverload = AddMethod(type, "M", Nullable(_module, _module.TypeSystem.Boolean).MakeByReferenceType());
            var byteOverload = AddMethod(type, "M", Nullable(_module, _module.TypeSystem.Byte).MakeByReferenceType());

            var otherType = AddType(_otherModule, "N", "C");
            var otherBoolOverload = AddMethod(otherType, "M", Nullable(_otherModule, _otherModule.TypeSystem.Boolean).MakeByReferenceType());

            Assert.True(EquivalenceComparers.MethodDefinition.Equals(boolOverload, otherBoolOverload));
            Assert.False(EquivalenceComparers.MethodDefinition.Equals(byteOverload, otherBoolOverload));
        }
    }
}
