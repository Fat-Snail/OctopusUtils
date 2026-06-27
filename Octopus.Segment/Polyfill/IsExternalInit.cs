// netstandard2.1 不带 IsExternalInit，记录类型与 init 属性需要此 polyfill
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    using System.ComponentModel;

    [EditorBrowsable(EditorBrowsableState.Never)]
    internal static class IsExternalInit { }
}
#endif
