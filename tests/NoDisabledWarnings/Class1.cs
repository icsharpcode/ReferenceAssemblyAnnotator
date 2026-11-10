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

#if !NET35 && !NETCOREAPP3_0 && !NETCOREAPP3_1
        // Task.IsFaulted carries [MemberNotNullWhen(true, nameof(Exception))]; without it this warns CS8602.
        public static string F(System.Threading.Tasks.Task task) => task.IsFaulted ? task.Exception.Message : "";
#endif
    }
}
