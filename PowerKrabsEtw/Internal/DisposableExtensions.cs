using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
