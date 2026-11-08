// Copyright (c) Tunnel Vision Laboratories, LLC. All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace TunnelVisionLabs.ReferenceAssemblyAnnotator
{
    using System.Collections.Generic;
    using System.Linq;
    using Mono.Cecil;

    internal static partial class EquivalenceComparers
    {
        private sealed class TypeReferenceEquivalenceComparer : IEqualityComparer<TypeReference?>
        {
            public static readonly TypeReferenceEquivalenceComparer Instance = new TypeReferenceEquivalenceComparer();

            private TypeReferenceEquivalenceComparer()
            {
            }

            public bool Equals(TypeReference? x, TypeReference? y)
            {
                if (x is null || y is null)
                    return ReferenceEquals(x, y);

                // GetElementType() returns the innermost type (dropping generic arguments), so compare ElementType.
                if (x.IsByReference)
                {
                    return y.IsByReference && Equals(((TypeSpecification)x).ElementType, ((TypeSpecification)y).ElementType);
                }
                else if (x.IsPointer)
                {
                    return y.IsPointer && Equals(((TypeSpecification)x).ElementType, ((TypeSpecification)y).ElementType);
                }
                else if (x.IsGenericInstance)
                {
                    var xInstance = (GenericInstanceType)x;

                    return y.IsGenericInstance
                        && Equals(x.GetElementType(), y.GetElementType())
                        && xInstance.GenericArguments.SequenceEqual(((GenericInstanceType)y).GenericArguments, TypeReference);
                }
                else if (x.IsArray)
                {
                    var xArray = (ArrayType)x;

                    return y.IsArray
                        && xArray.Dimensions.SequenceEqual(((ArrayType)y).Dimensions, ArrayDimensionEqualityComparer.Instance)
                        && Equals(xArray.ElementType, ((ArrayType)y).ElementType);
                }

                if (x.IsNested)
                {
                    if (!y.IsNested || !Equals(x.DeclaringType, y.DeclaringType))
                        return false;
                }
                else if (y.IsNested)
                {
                    return false;
                }

                if (x.Namespace != y.Namespace || x.Name != y.Name)
                    return false;

                if (x.GenericParameters.Count != y.GenericParameters.Count)
                    return false;

                return true;
            }

            public int GetHashCode(TypeReference? obj)
            {
                if (obj is null)
                    return 0;

                if (obj.IsNested)
                {
                    return GetHashCode(obj.DeclaringType)
                        ^ obj.Name.GetHashCode();
                }

                return obj.Name.GetHashCode();
            }
        }
    }
}
