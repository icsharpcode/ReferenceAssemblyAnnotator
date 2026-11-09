using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;

namespace Sample1
{
    public class Class1
    {
        public static void M(SqlConnection connection, [AllowNull] string? x)
        {
            if (!string.IsNullOrEmpty(x))
            {
                x.ToString();
            }
            
#if !NETCOREAPP3_0_OR_GREATER
            // The annotator removes [DisallowNull] from EqualityComparer<T>.GetHashCode. .NET Core 3.0+ is not annotated,
            // so its own [DisallowNull] applies there.
            _ = EqualityComparer<string?>.Default.GetHashCode(x);
#endif
        }
    }
}
