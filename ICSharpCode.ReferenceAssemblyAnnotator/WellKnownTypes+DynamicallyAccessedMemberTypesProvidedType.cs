// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator
{
    using Mono.Cecil;

    internal partial class WellKnownTypes
    {
        // Only the underlying type matters: attribute blobs store the enum value as int32.
        private sealed class DynamicallyAccessedMemberTypesProvidedType : ProvidedType
        {
            public DynamicallyAccessedMemberTypesProvidedType()
                : base("System.Diagnostics.CodeAnalysis", "DynamicallyAccessedMemberTypes")
            {
            }

            protected override TypeReference DefineType(ModuleDefinition module, WellKnownTypes wellKnownTypes, CustomAttributeFactory attributeFactory)
            {
                var type = new TypeDefinition(
                    @namespace: NamespaceName,
                    name: TypeName,
                    TypeAttributes.NotPublic | TypeAttributes.Sealed,
                    module.ImportReference(wellKnownTypes.SystemEnum));

                type.Fields.Add(new FieldDefinition(
                    name: "value__",
                    attributes: FieldAttributes.Public | FieldAttributes.SpecialName | FieldAttributes.RTSpecialName,
                    fieldType: module.TypeSystem.Int32));

                module.Types.Add(type);

                return type;
            }
        }
    }
}
