// Native WKWebView shim (Objective-C++)
// Exposes a stable C ABI for use from net10.0 via P/Invoke.
//
// Build requirements:
// - clang++ with -fobjc-arc
// - links against AppKit + WebKit + Foundation

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#import <WebKit/WebKit.h>
#import <dispatch/dispatch.h>

#include <atomic>
#include <cstdint>

extern "C" {

typedef void (*ag_wk_policy_request_cb)(
    void* user_data,
    uint64_t request_id,
    const char* url_utf8,
    bool is_main_frame,
    bool is_new_window,
    int navigation_type);

typedef void (*ag_wk_nav_completed_cb)(
    void* user_data,
    const char* url_utf8,
    int status, // 0=Success, 1=Failure, 2=Canceled
    int64_t error_code,
    const char* error_message_utf8);

typedef void (*ag_wk_script_result_cb)(
    void* user_data,
    uint64_t request_id,
    const char* result_utf8,
    const char* error_message_utf8);

typedef void (*ag_wk_message_cb)(
    void* user_data,
    const char* body_utf8,
    const char* origin_utf8);

struct ag_wk_callbacks
{
    ag_wk_policy_request_cb on_policy_request;
    ag_wk_nav_completed_cb on_navigation_completed;
    ag_wk_script_result_cb on_script_result;
    ag_wk_message_cb on_message;
};

typedef void* ag_wk_handle;

ag_wk_handle ag_wk_create(const ag_wk_callbacks* callbacks, void* user_data);
void ag_wk_destroy(ag_wk_handle handle);

bool ag_wk_attach(ag_wk_handle handle, void* nsview_ptr);
void ag_wk_detach(ag_wk_handle handle);

void ag_wk_policy_decide(ag_wk_handle handle, uint64_t request_id, bool allow);

void ag_wk_navigate(ag_wk_handle handle, const char* url_utf8);
void ag_wk_load_html(ag_wk_handle handle, const char* html_utf8, const char* base_url_utf8_or_null);
void ag_wk_eval_js(ag_wk_handle handle, uint64_t request_id, const char* script_utf8);

bool ag_wk_go_back(ag_wk_handle handle);
bool ag_wk_go_forward(ag_wk_handle handle);
bool ag_wk_reload(ag_wk_handle handle);
void ag_wk_stop(ag_wk_handle handle);

bool ag_wk_can_go_back(ag_wk_handle handle);
bool ag_wk_can_go_forward(ag_wk_handle handle);

} // extern "C"

static inline const char* utf8_or_empty(NSString* str)
{
    if (str == nil) return "";
    const char* utf8 = [str UTF8String];
    return utf8 ? utf8 : "";
}

static inline NSString* make_origin_string(WKScriptMessage* message)
{
    if (message == nil) return @"";

    WKFrameInfo* frame = message.frameInfo;
    if (frame == nil) return @"";

    WKSecurityOrigin* origin = frame.securityOrigin;
    if (origin == nil) return @"";

    NSString* proto = origin.protocol ?: @"";
    NSString* host = origin.host ?: @"";
    NSInteger port = origin.port;

    if (proto.length == 0 && host.length == 0)
    {
        return @"";
    }

    if (port > 0)
    {
        return [NSString stringWithFormat:@"%@://%@:%ld", proto, host, (long)port];
    }

    return [NSString stringWithFormat:@"%@://%@", proto, host];
}

typedef void (^policy_decision_block)(WKNavigationActionPolicy);

struct shim_state;

@interface ShimNavigationDelegate : NSObject <WKNavigationDelegate>
@property(nonatomic, assign) shim_state* state;
@end

@interface ShimMessageHandler : NSObject <WKScriptMessageHandler>
@property(nonatomic, assign) shim_state* state;
@end

struct shim_state
{
    ag_wk_callbacks callbacks {};
    void* user_data { nullptr };

    __strong NSView* parent_view { nil };
    __strong WKWebView* web_view { nil };
    __strong WKUserContentController* user_content_controller { nil };

    __strong ShimNavigationDelegate* nav_delegate { nil };
    __strong ShimMessageHandler* msg_handler { nil };

    std::atomic<uint64_t> next_request_id { 1 };
    __strong NSMutableDictionary<NSNumber*, policy_decision_block>* pending_policy { nil };

    std::atomic<bool> detached { false };
};

static void run_on_main(void (^block)(void))
{
    if ([NSThread isMainThread])
    {
        block();
        return;
    }

    // Most AppKit/WebKit APIs must be called on the main thread.
    // We use a synchronous hop to preserve call ordering and avoid use-after-free
    // when callers dispose immediately after invoking exported functions.
    dispatch_sync(dispatch_get_main_queue(), block);
}

static int map_error_status(NSError* error)
{
    if (error == nil) return 1;

    // NSURLErrorCancelled == -999 under NSURLErrorDomain
    if ([[error domain] isEqualToString:NSURLErrorDomain] && [error code] == NSURLErrorCancelled)
    {
        return 2; // Canceled
    }

    return 1; // Failure
}

@implementation ShimNavigationDelegate

- (void)webView:(WKWebView*)webView decidePolicyForNavigationAction:(WKNavigationAction*)navigationAction decisionHandler:(void (^)(WKNavigationActionPolicy))decisionHandler
{
    shim_state* s = self.state;
    if (s == nullptr)
    {
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

    if (s->detached.load())
    {
        decisionHandler(WKNavigationActionPolicyCancel);
        return;
    }

    // New window request: targetFrame == nil.
    bool is_new_window = (navigationAction.targetFrame == nil);
    bool is_main_frame = false;
    if (!is_new_window)
    {
        is_main_frame = navigationAction.targetFrame.mainFrame;
    }

    NSURL* url = navigationAction.request.URL;
    NSString* abs = url.absoluteString;
    const char* url_utf8 = utf8_or_empty(abs);

    uint64_t request_id = s->next_request_id.fetch_add(1);
    if (s->pending_policy == nil)
    {
        s->pending_policy = [[NSMutableDictionary alloc] init];
    }

    policy_decision_block copied = [decisionHandler copy];
    s->pending_policy[@(request_id)] = copied;

    if (s->callbacks.on_policy_request)
    {
        int nav_type = (int)navigationAction.navigationType;
        s->callbacks.on_policy_request(s->user_data, request_id, url_utf8, is_main_frame, is_new_window, nav_type);
    }
    else
    {
        // Default allow when no callback is configured.
        decisionHandler(WKNavigationActionPolicyAllow);
    }
}

- (void)webView:(WKWebView*)webView didFinishNavigation:(WKNavigation*)navigation
{
    shim_state* s = self.state;
    if (s == nullptr || s->detached.load())
    {
        return;
    }

    if (s->callbacks.on_navigation_completed)
    {
        NSString* abs = webView.URL.absoluteString ?: @"about:blank";
        s->callbacks.on_navigation_completed(s->user_data, utf8_or_empty(abs), 0, 0, "");
    }
}

- (void)webView:(WKWebView*)webView didFailProvisionalNavigation:(WKNavigation*)navigation withError:(NSError*)error
{
    shim_state* s = self.state;
    if (s == nullptr || s->detached.load())
    {
        return;
    }

    if (s->callbacks.on_navigation_completed)
    {
        NSString* abs = webView.URL.absoluteString ?: @"about:blank";
        int status = map_error_status(error);
        int64_t code = (int64_t)[error code];
        const char* msg = utf8_or_empty(error.localizedDescription);
        s->callbacks.on_navigation_completed(s->user_data, utf8_or_empty(abs), status, code, msg);
    }
}

- (void)webView:(WKWebView*)webView didFailNavigation:(WKNavigation*)navigation withError:(NSError*)error
{
    [self webView:webView didFailProvisionalNavigation:navigation withError:error];
}

@end

@implementation ShimMessageHandler

- (void)userContentController:(WKUserContentController*)userContentController didReceiveScriptMessage:(WKScriptMessage*)message
{
    shim_state* s = self.state;
    if (s == nullptr || s->detached.load())
    {
        return;
    }

    if (s->callbacks.on_message)
    {
        NSString* body = @"";
        if (message.body != nil)
        {
            // Normalize to a stable string.
            body = [message.body description] ?: @"";
        }

        NSString* origin = make_origin_string(message);
        s->callbacks.on_message(s->user_data, utf8_or_empty(body), utf8_or_empty(origin));
    }
}

@end

extern "C" {

ag_wk_handle ag_wk_create(const ag_wk_callbacks* callbacks, void* user_data)
{
    auto* s = new shim_state();
    if (callbacks)
    {
        s->callbacks = *callbacks;
    }
    s->user_data = user_data;
    return (ag_wk_handle)s;
}

void ag_wk_destroy(ag_wk_handle handle)
{
    if (!handle) return;
    auto* s = (shim_state*)handle;
    ag_wk_detach(handle);
    delete s;
}

bool ag_wk_attach(ag_wk_handle handle, void* nsview_ptr)
{
    if (!handle || !nsview_ptr) return false;
    auto* s = (shim_state*)handle;

    __block bool ok = false;
    run_on_main(^{
        @autoreleasepool
        {
            if (s->detached.load())
            {
                ok = false;
                return;
            }

            NSView* parent = (__bridge NSView*)nsview_ptr;
            if (parent == nil)
            {
                ok = false;
                return;
            }

            s->parent_view = parent;

            s->user_content_controller = [[WKUserContentController alloc] init];
            s->msg_handler = [[ShimMessageHandler alloc] init];
            s->msg_handler.state = s;
            [s->user_content_controller addScriptMessageHandler:s->msg_handler name:@"agibuildWebView"];

            WKWebViewConfiguration* cfg = [[WKWebViewConfiguration alloc] init];
            cfg.userContentController = s->user_content_controller;

            s->web_view = [[WKWebView alloc] initWithFrame:parent.bounds configuration:cfg];
            s->web_view.autoresizingMask = NSViewWidthSizable | NSViewHeightSizable;

            s->nav_delegate = [[ShimNavigationDelegate alloc] init];
            s->nav_delegate.state = s;
            s->web_view.navigationDelegate = s->nav_delegate;

            [parent addSubview:s->web_view];
            ok = true;
        }
    });

    return ok;
}

void ag_wk_detach(ag_wk_handle handle)
{
    if (!handle) return;
    auto* s = (shim_state*)handle;

    run_on_main(^{
        @autoreleasepool
        {
            if (s->detached.exchange(true))
            {
                return;
            }

            // Cancel all pending policy decisions by allowing (best-effort).
            if (s->pending_policy != nil)
            {
                for (NSNumber* key in [s->pending_policy allKeys])
                {
                    policy_decision_block blk = s->pending_policy[key];
                    if (blk)
                    {
                        blk(WKNavigationActionPolicyAllow);
                    }
                }
                [s->pending_policy removeAllObjects];
            }

            if (s->user_content_controller != nil)
            {
                [s->user_content_controller removeAllUserScripts];
                [s->user_content_controller removeScriptMessageHandlerForName:@"agibuildWebView"];
            }

            if (s->web_view != nil)
            {
                s->web_view.navigationDelegate = nil;
                [s->web_view removeFromSuperview];
            }

            s->nav_delegate = nil;
            s->msg_handler = nil;
            s->user_content_controller = nil;
            s->web_view = nil;
            s->parent_view = nil;
        }
    });
}

void ag_wk_policy_decide(ag_wk_handle handle, uint64_t request_id, bool allow)
{
    if (!handle || request_id == 0) return;
    auto* s = (shim_state*)handle;

    run_on_main(^{
        @autoreleasepool
        {
            if (s->pending_policy == nil)
            {
                return;
            }

            NSNumber* key = @(request_id);
            policy_decision_block blk = s->pending_policy[key];
            if (!blk)
            {
                return;
            }

            [s->pending_policy removeObjectForKey:key];
            blk(allow ? WKNavigationActionPolicyAllow : WKNavigationActionPolicyCancel);
        }
    });
}

void ag_wk_navigate(ag_wk_handle handle, const char* url_utf8)
{
    if (!handle || !url_utf8) return;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return;

    run_on_main(^{
        @autoreleasepool
        {
            if (s->web_view == nil) return;
            NSString* urlStr = [NSString stringWithUTF8String:url_utf8];
            if (urlStr == nil) return;
            NSURL* url = [NSURL URLWithString:urlStr];
            if (url == nil) return;
            NSURLRequest* req = [NSURLRequest requestWithURL:url];
            [s->web_view loadRequest:req];
        }
    });
}

void ag_wk_load_html(ag_wk_handle handle, const char* html_utf8, const char* base_url_utf8_or_null)
{
    if (!handle || !html_utf8) return;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return;

    run_on_main(^{
        @autoreleasepool
        {
            if (s->web_view == nil) return;
            NSString* html = [NSString stringWithUTF8String:html_utf8];
            if (html == nil) return;

            NSURL* baseUrl = nil;
            if (base_url_utf8_or_null != nullptr)
            {
                NSString* baseStr = [NSString stringWithUTF8String:base_url_utf8_or_null];
                if (baseStr != nil && baseStr.length > 0)
                {
                    baseUrl = [NSURL URLWithString:baseStr];
                }
            }

            [s->web_view loadHTMLString:html baseURL:baseUrl];
        }
    });
}

void ag_wk_eval_js(ag_wk_handle handle, uint64_t request_id, const char* script_utf8)
{
    if (!handle || request_id == 0 || !script_utf8) return;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return;

    run_on_main(^{
        @autoreleasepool
        {
            if (s->web_view == nil) return;
            NSString* script = [NSString stringWithUTF8String:script_utf8];
            if (script == nil) return;

            [s->web_view evaluateJavaScript:script completionHandler:^(id _Nullable result, NSError* _Nullable error) {
                if (s->detached.load())
                {
                    return;
                }

                if (!s->callbacks.on_script_result)
                {
                    return;
                }

                if (error != nil)
                {
                    const char* msg = utf8_or_empty(error.localizedDescription);
                    s->callbacks.on_script_result(s->user_data, request_id, nullptr, msg);
                    return;
                }

                if (result == nil || result == [NSNull null])
                {
                    s->callbacks.on_script_result(s->user_data, request_id, nullptr, nullptr);
                    return;
                }

                NSString* str = [[result description] copy];
                s->callbacks.on_script_result(s->user_data, request_id, utf8_or_empty(str), nullptr);
            }];
        }
    });
}

bool ag_wk_go_back(ag_wk_handle handle)
{
    if (!handle) return false;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return false;

    __block bool ok = false;
    run_on_main(^{
        if (s->web_view == nil) { ok = false; return; }
        if (s->web_view.canGoBack)
        {
            [s->web_view goBack];
            ok = true;
        }
    });
    return ok;
}

bool ag_wk_go_forward(ag_wk_handle handle)
{
    if (!handle) return false;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return false;

    __block bool ok = false;
    run_on_main(^{
        if (s->web_view == nil) { ok = false; return; }
        if (s->web_view.canGoForward)
        {
            [s->web_view goForward];
            ok = true;
        }
    });
    return ok;
}

bool ag_wk_reload(ag_wk_handle handle)
{
    if (!handle) return false;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return false;

    __block bool ok = false;
    run_on_main(^{
        if (s->web_view == nil) { ok = false; return; }
        [s->web_view reload];
        ok = true;
    });
    return ok;
}

void ag_wk_stop(ag_wk_handle handle)
{
    if (!handle) return;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return;

    run_on_main(^{
        if (s->web_view == nil) return;
        [s->web_view stopLoading];
    });
}

bool ag_wk_can_go_back(ag_wk_handle handle)
{
    if (!handle) return false;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return false;

    __block bool value = false;
    run_on_main(^{
        value = (s->web_view != nil) ? (bool)s->web_view.canGoBack : false;
    });
    return value;
}

bool ag_wk_can_go_forward(ag_wk_handle handle)
{
    if (!handle) return false;
    auto* s = (shim_state*)handle;
    if (s->detached.load()) return false;

    __block bool value = false;
    run_on_main(^{
        value = (s->web_view != nil) ? (bool)s->web_view.canGoForward : false;
    });
    return value;
}

} // extern "C"

