// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator
{
    using System;
    using Mono.Cecil;

    internal partial class WellKnownTypes
    {
        private sealed class DynamicallyAccessedMembersAttributeProvidedType : ProvidedAttributeType
        {
            public DynamicallyAccessedMembersAttributeProvidedType()
                : base("System.Diagnostics.CodeAnalysis", "DynamicallyAccessedMembersAttribute")
            {
            }

            protected override void ImplementAttribute(ModuleDefinition module, TypeDefinition attribute, WellKnownTypes wellKnownTypes, CustomAttributeFactory attributeFactory)
            {
                attribute.CustomAttributes.Add(attributeFactory.AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, inherited: false));

                var constructor = MethodFactory.Constructor(wellKnownTypes.TypeSystem);
                constructor.Parameters.Add(new ParameterDefinition("memberTypes", ParameterAttributes.None, wellKnownTypes.SystemDiagnosticsCodeAnalysisDynamicallyAccessedMemberTypes.Value));
                attribute.Methods.Add(constructor);
            }
        }
    }
}
