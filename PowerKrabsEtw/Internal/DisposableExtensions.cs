using System;

namespace PowerKrabsEtw.Internal
{
    internal static class DisposableExtensions
    {
        internal static void TryDispose(this object @this)
        {
            var disposable = @this as IDisposable;
            disposable?.Dispose();
        }
    }
}
