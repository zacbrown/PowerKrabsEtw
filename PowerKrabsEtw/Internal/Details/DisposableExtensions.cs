// Copyright (c) Zac Brown. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace PowerKrabsEtw.Internal.Details
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
