// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator
{
    using System;
    using Mono.Cecil;

    internal partial class WellKnownTypes
    {
        private sealed class RequiresUnreferencedCodeAttributeProvidedType : ProvidedAttributeType
        {
            public RequiresUnreferencedCodeAttributeProvidedType()
                : base("System.Diagnostics.CodeAnalysis", "RequiresUnreferencedCodeAttribute")
            {
            }

            protected override void ImplementAttribute(ModuleDefinition module, TypeDefinition attribute, WellKnownTypes wellKnownTypes, CustomAttributeFactory attributeFactory)
            {
                attribute.CustomAttributes.Add(attributeFactory.AttributeUsage(AttributeTargets.Class | AttributeTargets.Constructor | AttributeTargets.Method, inherited: false));

                var constructor = MethodFactory.Constructor(wellKnownTypes.TypeSystem);
                constructor.Parameters.Add(new ParameterDefinition("message", ParameterAttributes.None, wellKnownTypes.TypeSystem.String));
                attribute.Methods.Add(constructor);
            }
        }
    }
}
