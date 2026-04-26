// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Agibuild

using Agibuild.Fulora.Platforms.Macios.Interop;
using Agibuild.Fulora.Platforms.Macios.Interop.Foundation;

namespace Agibuild.Fulora.Platforms.Macios.Interop.WebKit;

internal abstract unsafe class WkDelegateBase(IntPtr classHandle) : NSManagedObjectBase(classHandle)
{
    protected static void AddProtocol(IntPtr cls, string protocolName)
    {
        var protocol = WKWebKit.objc_getProtocol(protocolName);
        if (protocol == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Objective-C protocol was not found: {protocolName}");
        }

        if (Libobjc.class_addProtocol(cls, protocol) != 1)
        {
            throw new InvalidOperationException($"Failed to add Objective-C protocol: {protocolName}");
        }
    }

    protected static void AddMethod(IntPtr cls, string selector, void* implementation, string typeEncoding)
    {
        if (Libobjc.class_addMethod(cls, Libobjc.sel_getUid(selector), implementation, typeEncoding) != 1)
        {
            throw new InvalidOperationException($"Failed to add Objective-C selector: {selector}");
        }
    }
}

internal enum WKNavigationActionPolicy : long
{
    Cancel = 0,
    Allow = 1,
    Download = 2
}

internal enum WKNavigationResponsePolicy : long
{
    Cancel = 0,
    Allow = 1,
    BecomeDownload = 2
}

internal sealed class DecidePolicyForNavigationActionEventArgs : EventArgs
{
    private static readonly IntPtr s_request = Libobjc.sel_getUid("request");

    internal DecidePolicyForNavigationActionEventArgs(IntPtr navigationAction, IntPtr decisionHandler)
    {
        NavigationAction = navigationAction;
        DecisionHandler = decisionHandler;
        Request = NSURLRequest.FromHandle(Libobjc.intptr_objc_msgSend(navigationAction, s_request));
    }

    public IntPtr NavigationAction { get; }

    public NSURLRequest Request { get; }

    public WKNavigationActionPolicy Policy { get; set; } = WKNavigationActionPolicy.Allow;

    private IntPtr DecisionHandler { get; }

    internal void Complete() => InvokeDecisionHandler(DecisionHandler, (long)Policy);

    private static unsafe void InvokeDecisionHandler(IntPtr block, long policy)
    {
        var callback = (delegate* unmanaged[Cdecl]<IntPtr, long, void>)BlockLiteral.GetCallback(block);
        callback(block, policy);
    }
}

internal sealed class DecidePolicyForNavigationResponseEventArgs : EventArgs
{
    private static readonly IntPtr s_response = Libobjc.sel_getUid("response");

    internal DecidePolicyForNavigationResponseEventArgs(IntPtr navigationResponse, IntPtr decisionHandler)
    {
        NavigationResponse = navigationResponse;
        DecisionHandler = decisionHandler;
        Response = new NSURLResponse(Libobjc.intptr_objc_msgSend(navigationResponse, s_response), owns: false);
    }

    public IntPtr NavigationResponse { get; }

    public NSURLResponse Response { get; }

    public WKNavigationResponsePolicy Policy { get; set; } = WKNavigationResponsePolicy.Allow;

    private IntPtr DecisionHandler { get; }

    internal void Complete() => InvokeDecisionHandler(DecisionHandler, (long)Policy);

    private static unsafe void InvokeDecisionHandler(IntPtr block, long policy)
    {
        var callback = (delegate* unmanaged[Cdecl]<IntPtr, long, void>)BlockLiteral.GetCallback(block);
        callback(block, policy);
    }
}
