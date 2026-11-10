// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator
{
    using System;
    using Mono.Cecil;

    internal partial class WellKnownTypes
    {
        private sealed class RequiresAssemblyFilesAttributeProvidedType : ProvidedAttributeType
        {
            public RequiresAssemblyFilesAttributeProvidedType()
                : base("System.Diagnostics.CodeAnalysis", "RequiresAssemblyFilesAttribute")
            {
            }

            protected override void ImplementAttribute(ModuleDefinition module, TypeDefinition attribute, WellKnownTypes wellKnownTypes, CustomAttributeFactory attributeFactory)
            {
                attribute.CustomAttributes.Add(attributeFactory.AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Event, inherited: false));

                attribute.AddDefaultConstructor(wellKnownTypes.TypeSystem);

                var constructor = MethodFactory.Constructor(wellKnownTypes.TypeSystem);
                constructor.Parameters.Add(new ParameterDefinition("message", ParameterAttributes.None, wellKnownTypes.TypeSystem.String));
                attribute.Methods.Add(constructor);
            }
        }
    }
}
