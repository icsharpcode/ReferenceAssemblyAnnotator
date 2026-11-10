// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace ICSharpCode.ReferenceAssemblyAnnotator.Tests
{
    using System.Linq;
    using Mono.Cecil;
    using Mono.Cecil.Rocks;

    /// <summary>
    /// Builds small in-memory Cecil modules for the tests.
    /// </summary>
    internal static class CecilBuilder
    {
        public const string NullableAttribute = "System.Runtime.CompilerServices.NullableAttribute";
        public const string DisallowNullAttribute = "System.Diagnostics.CodeAnalysis.DisallowNullAttribute";

        public static ModuleDefinition CreateModule(string name)
            => ModuleDefinition.CreateModule(name, ModuleKind.Dll);

        public static TypeDefinition AddType(ModuleDefinition module, string @namespace, string name, int genericArity = 0)
        {
            var type = new TypeDefinition(@namespace, name, TypeAttributes.Public | TypeAttributes.Class, module.TypeSystem.Object);
            for (int i = 0; i < genericArity; i++)
                type.GenericParameters.Add(new GenericParameter("T" + i, type));

            module.Types.Add(type);
            return type;
        }

        public static MethodDefinition AddMethod(TypeDefinition type, string name, params TypeReference[] parameterTypes)
        {
            var method = new MethodDefinition(name, MethodAttributes.Public, type.Module.TypeSystem.Void);
            foreach (var parameterType in parameterTypes)
                method.Parameters.Add(new ParameterDefinition(parameterType));

            type.Methods.Add(method);
            return method;
        }

        // Adds an attribute type with a constructor taking the given parameter types.
        public static TypeDefinition AddAttributeType(ModuleDefinition module, string fullName, params TypeReference[] constructorParameterTypes)
        {
            int dot = fullName.LastIndexOf('.');
            var type = AddType(module, fullName.Substring(0, dot), fullName.Substring(dot + 1));
            var constructor = new MethodDefinition(
                ".ctor",
                MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                module.TypeSystem.Void);
            foreach (var parameterType in constructorParameterTypes)
                constructor.Parameters.Add(new ParameterDefinition(parameterType));

            type.Methods.Add(constructor);
            return type;
        }

        public static void AddAttribute(ICustomAttributeProvider provider, TypeDefinition attributeType, params CustomAttributeArgument[] arguments)
        {
            var attribute = new CustomAttribute(attributeType.Methods.Single(method => method.IsConstructor));
            foreach (var argument in arguments)
                attribute.ConstructorArguments.Add(argument);

            provider.CustomAttributes.Add(attribute);
        }

        public static TypeReference Nullable(ModuleDefinition module, TypeReference typeArgument)
            => Generic(module, "System", "Nullable`1", isValueType: true, typeArgument);

        public static TypeReference Generic(ModuleDefinition module, string @namespace, string name, bool isValueType, params TypeReference[] typeArguments)
        {
            var open = new TypeReference(@namespace, name, module, module.TypeSystem.CoreLibrary, isValueType);
            for (int i = 0; i < typeArguments.Length; i++)
                open.GenericParameters.Add(new GenericParameter(open));

            return open.MakeGenericInstanceType(typeArguments);
        }
    }
}
