// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Agibuild

using Agibuild.Fulora.Platforms.Macios.Interop;

namespace Agibuild.Fulora.Platforms.Macios.Interop.WebKit;

internal sealed class WKDownload : NSObject
{
    private static readonly IntPtr s_originalRequest = Libobjc.sel_getUid("originalRequest");
    private static readonly IntPtr s_progress = Libobjc.sel_getUid("progress");

    internal WKDownload(IntPtr handle, bool owns) : base(handle, owns)
    {
    }

    public IntPtr OriginalRequest => Libobjc.intptr_objc_msgSend(Handle, s_originalRequest);

    public IntPtr Progress => Libobjc.intptr_objc_msgSend(Handle, s_progress);
}
