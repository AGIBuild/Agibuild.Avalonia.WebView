// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Agibuild

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Agibuild.Fulora.Platforms.Macios.Interop;
using Agibuild.Fulora.Platforms.Macios.Interop.Foundation;

namespace Agibuild.Fulora.Platforms.Macios.Interop.WebKit;

internal sealed unsafe class WKNavigationDelegate : WkDelegateBase
{
    private static readonly void* s_didFinish =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, void>)&DidFinishNavigationCallback;
    private static readonly void* s_decidePolicyForNavigationAction =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)&DecidePolicyForNavigationActionCallback;
    private static readonly void* s_didFailProvisional =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)&DidFailProvisionalCallback;
    private static readonly void* s_didFail =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)&DidFailCallback;
    private static readonly void* s_decidePolicyForNavigationResponse =
        (delegate* unmanaged[Cdecl]<IntPtr, IntPtr, IntPtr, IntPtr, IntPtr, void>)&DecidePolicyForNavigationResponseCallback;

    private static readonly IntPtr s_class;

    static WKNavigationDelegate()
    {
        var cls = AllocateClassPair("ManagedWKNavigationDelegate");
        AddProtocol(cls, "WKNavigationDelegate");

        AddMethod(cls, "webView:didFinishNavigation:", s_didFinish, "v@:@@");
        AddMethod(cls, "webView:decidePolicyForNavigationAction:decisionHandler:", s_decidePolicyForNavigationAction, "v@:@@@");
        AddMethod(cls, "webView:didFailProvisionalNavigation:withError:", s_didFailProvisional, "v@:@@@");
        AddMethod(cls, "webView:didFailNavigation:withError:", s_didFail, "v@:@@@");
        AddMethod(cls, "webView:decidePolicyForNavigationResponse:decisionHandler:", s_decidePolicyForNavigationResponse, "v@:@@@");

        if (!RegisterManagedMembers(cls))
        {
            throw new InvalidOperationException("Failed to register managed-self storage for WKNavigationDelegate.");
        }

        Libobjc.objc_registerClassPair(cls);
        s_class = cls;
    }

    public WKNavigationDelegate() : base(s_class)
    {
    }

    public event EventHandler? DidFinishNavigation;

    public event EventHandler<NSError>? DidFailProvisionalNavigation;

    public event EventHandler<NSError>? DidFailNavigation;

    public event EventHandler<DecidePolicyForNavigationActionEventArgs>? DecidePolicyForNavigationAction;

    public event EventHandler<DecidePolicyForNavigationResponseEventArgs>? DecidePolicyForNavigationResponse;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DidFinishNavigationCallback(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigation)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        managed?.DidFinishNavigation?.Invoke(managed, EventArgs.Empty);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DecidePolicyForNavigationActionCallback(
        IntPtr self,
        IntPtr sel,
        IntPtr webView,
        IntPtr navigationAction,
        IntPtr decisionHandler)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        var args = new DecidePolicyForNavigationActionEventArgs(navigationAction, decisionHandler);
        managed?.DecidePolicyForNavigationAction?.Invoke(managed, args);
        args.Complete();
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DidFailProvisionalCallback(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigation, IntPtr error)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        managed?.DidFailProvisionalNavigation?.Invoke(managed, new NSError(error));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DidFailCallback(IntPtr self, IntPtr sel, IntPtr webView, IntPtr navigation, IntPtr error)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        managed?.DidFailNavigation?.Invoke(managed, new NSError(error));
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DecidePolicyForNavigationResponseCallback(
        IntPtr self,
        IntPtr sel,
        IntPtr webView,
        IntPtr navigationResponse,
        IntPtr decisionHandler)
    {
        var managed = ReadManagedSelf<WKNavigationDelegate>(self);
        var args = new DecidePolicyForNavigationResponseEventArgs(navigationResponse, decisionHandler);
        managed?.DecidePolicyForNavigationResponse?.Invoke(managed, args);
        args.Complete();
    }
}
