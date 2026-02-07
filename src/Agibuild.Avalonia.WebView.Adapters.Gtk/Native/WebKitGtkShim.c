/*
 * Native WebKitGTK shim
 * Exposes a stable C ABI for use from net10.0 via P/Invoke.
 *
 * Build requirements:
 * - gcc/clang with pkg-config
 * - libwebkit2gtk-4.1-dev (or webkit2gtk-4.1 devel package)
 *
 * Build command:
 *   gcc -shared -fPIC -o libAgibuildWebViewGtk.so WebKitGtkShim.c \
 *       $(pkg-config --cflags --libs webkit2gtk-4.1 gtk+-3.0)
 */

#include <gtk/gtk.h>
#include <gtk/gtkx.h>
#include <webkit2/webkit2.h>
#include <string.h>
#include <stdlib.h>
#include <stdint.h>
#include <stdbool.h>
#include <stdatomic.h>

/* ========== Callback typedefs ========== */

typedef void (*ag_gtk_policy_request_cb)(
    void* user_data,
    uint64_t request_id,
    const char* url_utf8,
    bool is_main_frame,
    bool is_new_window,
    int navigation_type);

typedef void (*ag_gtk_nav_completed_cb)(
    void* user_data,
    const char* url_utf8,
    int status, /* 0=Success, 1=Failure, 2=Canceled, 3=Timeout, 4=Network, 5=Ssl */
    int64_t error_code,
    const char* error_message_utf8);

typedef void (*ag_gtk_script_result_cb)(
    void* user_data,
    uint64_t request_id,
    const char* result_utf8,
    const char* error_message_utf8);

typedef void (*ag_gtk_message_cb)(
    void* user_data,
    const char* body_utf8,
    const char* origin_utf8);

struct ag_gtk_callbacks
{
    ag_gtk_policy_request_cb on_policy_request;
    ag_gtk_nav_completed_cb on_navigation_completed;
    ag_gtk_script_result_cb on_script_result;
    ag_gtk_message_cb on_message;
};

/* ========== Cookie operation callbacks ========== */

typedef void (*ag_gtk_cookies_get_cb)(void* context, const char* json_utf8);
typedef void (*ag_gtk_cookie_op_cb)(void* context, bool success, const char* error_utf8);

/* ========== Shim state ========== */

typedef struct
{
    struct ag_gtk_callbacks callbacks;
    void* user_data;

    GtkWidget* plug;         /* GtkPlug embedding container */
    WebKitWebView* web_view;
    WebKitUserContentManager* content_manager;
    WebKitWebsiteDataManager* data_manager;

    atomic_uint_fast64_t next_request_id;
    atomic_bool detached;
    gboolean gtk_initialized;

    /* Pending policy decisions: request_id -> WebKitPolicyDecision* */
    GHashTable* pending_policy;

    /* Options — set before attach. */
    gboolean opt_enable_dev_tools;
    gboolean opt_ephemeral;
    char* opt_user_agent; /* owned, NULL if not set */

} shim_state;

typedef void* ag_gtk_handle;

/* Forward declarations */
void ag_gtk_detach(ag_gtk_handle handle);

/* ========== GTK thread safety ========== */

static void ensure_gtk_init(void)
{
    static gboolean initialized = FALSE;
    if (!initialized)
    {
        if (!gtk_init_check(NULL, NULL))
        {
            /* GTK could not initialize — probably no display. */
            return;
        }
        initialized = TRUE;
    }
}

typedef struct
{
    void (*func)(void* data);
    void* data;
    gboolean done;
    GMutex mutex;
    GCond cond;
} sync_call;

static gboolean sync_call_idle(gpointer user_data)
{
    sync_call* sc = (sync_call*)user_data;
    sc->func(sc->data);
    g_mutex_lock(&sc->mutex);
    sc->done = TRUE;
    g_cond_signal(&sc->cond);
    g_mutex_unlock(&sc->mutex);
    return G_SOURCE_REMOVE;
}

/* Run a function on the GTK main thread synchronously. */
static void run_on_gtk_thread(void (*func)(void*), void* data)
{
    /* If we're on the main context's thread already, just call directly. */
    if (g_main_context_is_owner(g_main_context_default()))
    {
        func(data);
        return;
    }

    sync_call sc;
    sc.func = func;
    sc.data = data;
    sc.done = FALSE;
    g_mutex_init(&sc.mutex);
    g_cond_init(&sc.cond);

    g_idle_add(sync_call_idle, &sc);

    g_mutex_lock(&sc.mutex);
    while (!sc.done)
        g_cond_wait(&sc.cond, &sc.mutex);
    g_mutex_unlock(&sc.mutex);

    g_mutex_clear(&sc.mutex);
    g_cond_clear(&sc.cond);
}

/* ========== Error status mapping ========== */

/* Map WebKitGTK error codes to our status codes:
 * 0=Success, 1=Failure, 2=Canceled, 3=Timeout, 4=Network, 5=Ssl */
static int map_webkit_error(GError* error)
{
    if (error == NULL)
        return 1;

    if (g_error_matches(error, WEBKIT_NETWORK_ERROR, WEBKIT_NETWORK_ERROR_CANCELLED))
        return 2;

    if (g_error_matches(error, WEBKIT_NETWORK_ERROR, WEBKIT_NETWORK_ERROR_TRANSPORT))
        return 4;

    if (g_error_matches(error, WEBKIT_NETWORK_ERROR, WEBKIT_NETWORK_ERROR_UNKNOWN_PROTOCOL) ||
        g_error_matches(error, WEBKIT_NETWORK_ERROR, WEBKIT_NETWORK_ERROR_FAILED))
        return 4;

    if (error->domain == WEBKIT_POLICY_ERROR)
        return 2; /* Policy errors are typically cancellations */

    /* TLS/SSL errors */
    if (g_error_matches(error, G_TLS_ERROR, G_TLS_ERROR_BAD_CERTIFICATE) ||
        g_error_matches(error, G_TLS_ERROR, G_TLS_ERROR_NOT_TLS) ||
        g_error_matches(error, G_TLS_ERROR, G_TLS_ERROR_CERTIFICATE_REQUIRED))
        return 5;

    return 1; /* General failure */
}

/* ========== WebKitGTK signal handlers ========== */

static gboolean on_decide_policy(WebKitWebView* web_view, WebKitPolicyDecision* decision,
                                  WebKitPolicyDecisionType type, gpointer user_data)
{
    shim_state* s = (shim_state*)user_data;
    if (atomic_load(&s->detached))
    {
        webkit_policy_decision_ignore(decision);
        return TRUE;
    }

    if (type == WEBKIT_POLICY_DECISION_TYPE_NEW_WINDOW_ACTION)
    {
        WebKitNavigationPolicyDecision* nav_decision = WEBKIT_NAVIGATION_POLICY_DECISION(decision);
        WebKitNavigationAction* action = webkit_navigation_policy_decision_get_navigation_action(nav_decision);
        WebKitURIRequest* request = webkit_navigation_action_get_request(action);
        const char* url = webkit_uri_request_get_uri(request);

        if (s->callbacks.on_policy_request)
        {
            uint64_t req_id = atomic_fetch_add(&s->next_request_id, 1);
            g_object_ref(decision);
            g_hash_table_insert(s->pending_policy, GUINT_TO_POINTER((guint)req_id), decision);
            s->callbacks.on_policy_request(s->user_data, req_id, url ? url : "", FALSE, TRUE, 0);
        }
        else
        {
            webkit_policy_decision_ignore(decision);
        }
        return TRUE;
    }

    if (type == WEBKIT_POLICY_DECISION_TYPE_NAVIGATION_ACTION)
    {
        WebKitNavigationPolicyDecision* nav_decision = WEBKIT_NAVIGATION_POLICY_DECISION(decision);
        WebKitNavigationAction* action = webkit_navigation_policy_decision_get_navigation_action(nav_decision);
        WebKitURIRequest* request = webkit_navigation_action_get_request(action);
        const char* url = webkit_uri_request_get_uri(request);
        int nav_type = (int)webkit_navigation_action_get_navigation_type(action);
        gboolean is_main = webkit_navigation_policy_decision_get_frame_name(nav_decision) == NULL;

        if (s->callbacks.on_policy_request)
        {
            uint64_t req_id = atomic_fetch_add(&s->next_request_id, 1);
            g_object_ref(decision);
            g_hash_table_insert(s->pending_policy, GUINT_TO_POINTER((guint)req_id), decision);
            s->callbacks.on_policy_request(s->user_data, req_id, url ? url : "", is_main, FALSE, nav_type);
        }
        else
        {
            webkit_policy_decision_use(decision);
        }
        return TRUE;
    }

    return FALSE; /* Let WebKit handle other decision types */
}

static void on_load_changed(WebKitWebView* web_view, WebKitLoadEvent event, gpointer user_data)
{
    shim_state* s = (shim_state*)user_data;
    if (atomic_load(&s->detached))
        return;

    if (event == WEBKIT_LOAD_FINISHED)
    {
        if (s->callbacks.on_navigation_completed)
        {
            const char* url = webkit_web_view_get_uri(web_view);
            s->callbacks.on_navigation_completed(s->user_data, url ? url : "about:blank", 0, 0, "");
        }
    }
}

static gboolean on_load_failed(WebKitWebView* web_view, WebKitLoadEvent event,
                                const char* failing_uri, GError* error, gpointer user_data)
{
    shim_state* s = (shim_state*)user_data;
    if (atomic_load(&s->detached))
        return TRUE;

    if (s->callbacks.on_navigation_completed)
    {
        int status = map_webkit_error(error);
        int64_t code = error ? (int64_t)error->code : 0;
        const char* msg = error ? error->message : "Unknown error";
        s->callbacks.on_navigation_completed(s->user_data, failing_uri ? failing_uri : "about:blank", status, code, msg);
    }

    return TRUE; /* We handled it */
}

static gboolean on_load_failed_tls(WebKitWebView* web_view, const char* failing_uri,
                                    GTlsCertificate* certificate, GTlsCertificateFlags errors,
                                    gpointer user_data)
{
    shim_state* s = (shim_state*)user_data;
    if (atomic_load(&s->detached))
        return TRUE;

    if (s->callbacks.on_navigation_completed)
    {
        s->callbacks.on_navigation_completed(s->user_data,
            failing_uri ? failing_uri : "about:blank",
            5 /* SSL */, (int64_t)errors, "TLS certificate error");
    }

    return TRUE;
}

static void on_script_message(WebKitUserContentManager* manager, WebKitJavascriptResult* result,
                               gpointer user_data)
{
    shim_state* s = (shim_state*)user_data;
    if (atomic_load(&s->detached))
        return;

    if (s->callbacks.on_message)
    {
        JSCValue* value = webkit_javascript_result_get_js_value(result);
        char* body = jsc_value_to_string(value);
        const char* url = webkit_web_view_get_uri(s->web_view);

        /* Extract origin from current URI */
        char origin[512] = "";
        if (url)
        {
            /* Simple origin extraction: scheme://host[:port] */
            const char* scheme_end = strstr(url, "://");
            if (scheme_end)
            {
                const char* host_start = scheme_end + 3;
                const char* path_start = strchr(host_start, '/');
                size_t origin_len = path_start ? (size_t)(path_start - url) : strlen(url);
                if (origin_len < sizeof(origin))
                {
                    memcpy(origin, url, origin_len);
                    origin[origin_len] = '\0';
                }
            }
        }

        s->callbacks.on_message(s->user_data, body ? body : "", origin);
        g_free(body);
    }
}

/* ========== Script evaluation callback data ========== */

typedef struct
{
    shim_state* state;
    uint64_t request_id;
} eval_js_data;

static void on_eval_js_finish(GObject* source, GAsyncResult* result, gpointer user_data)
{
    eval_js_data* data = (eval_js_data*)user_data;
    shim_state* s = data->state;
    uint64_t req_id = data->request_id;
    free(data);

    if (atomic_load(&s->detached))
        return;

    if (!s->callbacks.on_script_result)
        return;

    GError* error = NULL;
    WebKitJavascriptResult* js_result = webkit_web_view_run_javascript_finish(
        WEBKIT_WEB_VIEW(source), result, &error);

    if (error != NULL)
    {
        s->callbacks.on_script_result(s->user_data, req_id, NULL, error->message);
        g_error_free(error);
        return;
    }

    if (js_result == NULL)
    {
        s->callbacks.on_script_result(s->user_data, req_id, NULL, NULL);
        return;
    }

    JSCValue* value = webkit_javascript_result_get_js_value(js_result);
    if (jsc_value_is_undefined(value) || jsc_value_is_null(value))
    {
        s->callbacks.on_script_result(s->user_data, req_id, NULL, NULL);
        webkit_javascript_result_unref(js_result);
        return;
    }

    char* str = jsc_value_to_string(value);
    s->callbacks.on_script_result(s->user_data, req_id, str ? str : NULL, NULL);
    g_free(str);
    webkit_javascript_result_unref(js_result);
}

/* ========== Attach helper ========== */

typedef struct
{
    shim_state* state;
    gulong x11_window_id;
    gboolean result;
} attach_data;

static void do_attach(void* data)
{
    attach_data* ad = (attach_data*)data;
    shim_state* s = ad->state;

    if (atomic_load(&s->detached))
    {
        ad->result = FALSE;
        return;
    }

    /* Create a GtkPlug to embed into the X11 window provided by Avalonia NativeControlHost */
    s->plug = gtk_plug_new((Window)ad->x11_window_id);
    if (s->plug == NULL)
    {
        ad->result = FALSE;
        return;
    }

    /* Create content manager for script message handling */
    s->content_manager = webkit_user_content_manager_new();
    g_signal_connect(s->content_manager, "script-message-received::agibuildWebView",
                     G_CALLBACK(on_script_message), s);
    webkit_user_content_manager_register_script_message_handler(s->content_manager, "agibuildWebView");

    /* Create WebKitWebView */
    if (s->opt_ephemeral)
    {
        WebKitWebContext* ctx = webkit_web_context_new_ephemeral();
        s->web_view = WEBKIT_WEB_VIEW(g_object_new(WEBKIT_TYPE_WEB_VIEW,
            "web-context", ctx,
            "user-content-manager", s->content_manager,
            NULL));
        g_object_unref(ctx);
    }
    else
    {
        s->web_view = WEBKIT_WEB_VIEW(g_object_new(WEBKIT_TYPE_WEB_VIEW,
            "user-content-manager", s->content_manager,
            NULL));
    }

    /* Apply DevTools setting */
    WebKitSettings* settings = webkit_web_view_get_settings(s->web_view);
    webkit_settings_set_enable_developer_extras(settings, s->opt_enable_dev_tools);

    /* Apply custom user agent */
    if (s->opt_user_agent != NULL)
    {
        webkit_settings_set_user_agent(settings, s->opt_user_agent);
    }

    /* Enable JavaScript */
    webkit_settings_set_enable_javascript(settings, TRUE);

    /* Connect signals */
    g_signal_connect(s->web_view, "decide-policy", G_CALLBACK(on_decide_policy), s);
    g_signal_connect(s->web_view, "load-changed", G_CALLBACK(on_load_changed), s);
    g_signal_connect(s->web_view, "load-failed", G_CALLBACK(on_load_failed), s);
    g_signal_connect(s->web_view, "load-failed-with-tls-errors", G_CALLBACK(on_load_failed_tls), s);

    /* Add WebView to the plug */
    gtk_container_add(GTK_CONTAINER(s->plug), GTK_WIDGET(s->web_view));
    gtk_widget_show_all(s->plug);

    ad->result = TRUE;
}

/* ========== Detach helper ========== */

static void do_detach(void* data)
{
    shim_state* s = (shim_state*)data;
    gboolean was_detached = atomic_exchange(&s->detached, TRUE);
    if (was_detached)
        return;

    /* Unregister script message handler */
    if (s->content_manager != NULL)
    {
        webkit_user_content_manager_unregister_script_message_handler(s->content_manager, "agibuildWebView");
    }

    /* Destroy the plug (and its children including web_view) */
    if (s->plug != NULL)
    {
        gtk_widget_destroy(s->plug);
        s->plug = NULL;
    }

    /* Cancel all pending policy decisions */
    if (s->pending_policy != NULL)
    {
        GHashTableIter iter;
        gpointer key, value;
        g_hash_table_iter_init(&iter, s->pending_policy);
        while (g_hash_table_iter_next(&iter, &key, &value))
        {
            WebKitPolicyDecision* decision = (WebKitPolicyDecision*)value;
            webkit_policy_decision_ignore(decision);
            g_object_unref(decision);
        }
        g_hash_table_remove_all(s->pending_policy);
    }

    s->web_view = NULL;
    s->content_manager = NULL;
}

/* ========== Public API ========== */

ag_gtk_handle ag_gtk_create(const struct ag_gtk_callbacks* callbacks, void* user_data)
{
    ensure_gtk_init();

    shim_state* s = (shim_state*)calloc(1, sizeof(shim_state));
    if (s == NULL)
        return NULL;

    if (callbacks)
    {
        s->callbacks = *callbacks;
    }
    s->user_data = user_data;
    atomic_init(&s->next_request_id, 1);
    atomic_init(&s->detached, FALSE);
    s->pending_policy = g_hash_table_new(g_direct_hash, g_direct_equal);

    return (ag_gtk_handle)s;
}

void ag_gtk_destroy(ag_gtk_handle handle)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;
    ag_gtk_detach(handle);

    if (s->pending_policy != NULL)
    {
        g_hash_table_destroy(s->pending_policy);
        s->pending_policy = NULL;
    }

    free(s->opt_user_agent);
    free(s);
}

bool ag_gtk_attach(ag_gtk_handle handle, unsigned long x11_window_id)
{
    if (!handle || x11_window_id == 0) return false;
    shim_state* s = (shim_state*)handle;

    attach_data ad;
    ad.state = s;
    ad.x11_window_id = x11_window_id;
    ad.result = FALSE;

    run_on_gtk_thread(do_attach, &ad);
    return ad.result;
}

void ag_gtk_detach(ag_gtk_handle handle)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;
    run_on_gtk_thread(do_detach, s);
}

void ag_gtk_policy_decide(ag_gtk_handle handle, uint64_t request_id, bool allow)
{
    if (!handle || request_id == 0) return;
    shim_state* s = (shim_state*)handle;

    WebKitPolicyDecision* decision = (WebKitPolicyDecision*)g_hash_table_lookup(
        s->pending_policy, GUINT_TO_POINTER((guint)request_id));

    if (decision == NULL)
        return;

    g_hash_table_remove(s->pending_policy, GUINT_TO_POINTER((guint)request_id));

    if (allow)
        webkit_policy_decision_use(decision);
    else
        webkit_policy_decision_ignore(decision);

    g_object_unref(decision);
}

void ag_gtk_navigate(ag_gtk_handle handle, const char* url_utf8)
{
    if (!handle || !url_utf8) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return;

    webkit_web_view_load_uri(s->web_view, url_utf8);
}

void ag_gtk_load_html(ag_gtk_handle handle, const char* html_utf8, const char* base_url_utf8_or_null)
{
    if (!handle || !html_utf8) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return;

    webkit_web_view_load_html(s->web_view, html_utf8, base_url_utf8_or_null);
}

void ag_gtk_eval_js(ag_gtk_handle handle, uint64_t request_id, const char* script_utf8)
{
    if (!handle || request_id == 0 || !script_utf8) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return;

    eval_js_data* data = (eval_js_data*)malloc(sizeof(eval_js_data));
    if (data == NULL) return;
    data->state = s;
    data->request_id = request_id;

    webkit_web_view_run_javascript(s->web_view, script_utf8, NULL, on_eval_js_finish, data);
}

bool ag_gtk_go_back(ag_gtk_handle handle)
{
    if (!handle) return false;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return false;
    if (!webkit_web_view_can_go_back(s->web_view)) return false;
    webkit_web_view_go_back(s->web_view);
    return true;
}

bool ag_gtk_go_forward(ag_gtk_handle handle)
{
    if (!handle) return false;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return false;
    if (!webkit_web_view_can_go_forward(s->web_view)) return false;
    webkit_web_view_go_forward(s->web_view);
    return true;
}

bool ag_gtk_reload(ag_gtk_handle handle)
{
    if (!handle) return false;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return false;
    webkit_web_view_reload(s->web_view);
    return true;
}

void ag_gtk_stop(ag_gtk_handle handle)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return;
    webkit_web_view_stop_loading(s->web_view);
}

bool ag_gtk_can_go_back(ag_gtk_handle handle)
{
    if (!handle) return false;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return false;
    return webkit_web_view_can_go_back(s->web_view);
}

bool ag_gtk_can_go_forward(ag_gtk_handle handle)
{
    if (!handle) return false;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL) return false;
    return webkit_web_view_can_go_forward(s->web_view);
}

void* ag_gtk_get_webview_handle(ag_gtk_handle handle)
{
    if (!handle) return NULL;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached)) return NULL;
    return (void*)s->web_view;
}

/* ========== Cookie management ========== */

typedef struct
{
    shim_state* state;
    ag_gtk_cookies_get_cb callback;
    void* context;
    char* url;
} cookies_get_data;

static void on_cookies_get_finish(WebKitCookieManager* manager, GAsyncResult* result, gpointer user_data)
{
    cookies_get_data* data = (cookies_get_data*)user_data;

    GError* error = NULL;
    GList* cookies = webkit_cookie_manager_get_cookies_finish(manager, result, &error);

    if (error != NULL)
    {
        data->callback(data->context, "[]");
        g_error_free(error);
        free(data->url);
        free(data);
        return;
    }

    /* Build JSON array */
    GString* json = g_string_new("[");
    gboolean first = TRUE;
    for (GList* l = cookies; l != NULL; l = l->next)
    {
        SoupCookie* c = (SoupCookie*)l->data;
        if (!first) g_string_append_c(json, ',');
        first = FALSE;

        const char* name = soup_cookie_get_name(c);
        const char* value = soup_cookie_get_value(c);
        const char* domain = soup_cookie_get_domain(c);
        const char* path = soup_cookie_get_path(c);
        gboolean secure = soup_cookie_get_secure(c);
        gboolean http_only = soup_cookie_get_http_only(c);

        SoupDate* expires = soup_cookie_get_expires(c);
        double expires_unix = expires ? (double)soup_date_to_time_t(expires) : -1.0;

        g_string_append_printf(json,
            "{\"name\":\"%s\",\"value\":\"%s\",\"domain\":\"%s\",\"path\":\"%s\","
            "\"expires\":%.3f,\"isSecure\":%s,\"isHttpOnly\":%s}",
            name ? name : "", value ? value : "", domain ? domain : "", path ? path : "/",
            expires_unix,
            secure ? "true" : "false",
            http_only ? "true" : "false");
    }
    g_string_append_c(json, ']');

    data->callback(data->context, json->str);

    g_string_free(json, TRUE);
    g_list_free_full(cookies, (GDestroyNotify)soup_cookie_free);
    free(data->url);
    free(data);
}

void ag_gtk_cookies_get(ag_gtk_handle handle, const char* url_utf8,
                         ag_gtk_cookies_get_cb callback, void* context)
{
    if (!handle || !callback) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL)
    {
        callback(context, "[]");
        return;
    }

    WebKitWebContext* web_ctx = webkit_web_view_get_context(s->web_view);
    WebKitCookieManager* cookie_mgr = webkit_web_context_get_cookie_manager(web_ctx);

    cookies_get_data* data = (cookies_get_data*)calloc(1, sizeof(cookies_get_data));
    data->state = s;
    data->callback = callback;
    data->context = context;
    data->url = url_utf8 ? strdup(url_utf8) : NULL;

    webkit_cookie_manager_get_cookies(cookie_mgr, url_utf8 ? url_utf8 : "",
                                       NULL, (GAsyncReadyCallback)on_cookies_get_finish, data);
}

void ag_gtk_cookie_set(ag_gtk_handle handle,
    const char* name, const char* value, const char* domain, const char* path,
    double expires_unix, bool is_secure, bool is_http_only,
    ag_gtk_cookie_op_cb callback, void* context)
{
    if (!handle || !callback) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL)
    {
        callback(context, false, "Detached");
        return;
    }

    SoupCookie* cookie = soup_cookie_new(
        name ? name : "", value ? value : "",
        domain ? domain : "", path ? path : "/",
        -1 /* max-age: session */);

    if (expires_unix > 0)
    {
        /* libsoup3 (used by webkit2gtk-4.1) replaced SoupDate with GDateTime. */
        GDateTime* date = g_date_time_new_from_unix_utc(expires_unix);
        if (date)
        {
            soup_cookie_set_expires(cookie, date);
            g_date_time_unref(date);
        }
    }

    soup_cookie_set_secure(cookie, is_secure);
    soup_cookie_set_http_only(cookie, is_http_only);

    WebKitWebContext* web_ctx = webkit_web_view_get_context(s->web_view);
    WebKitCookieManager* cookie_mgr = webkit_web_context_get_cookie_manager(web_ctx);
    webkit_cookie_manager_add_cookie(cookie_mgr, cookie, NULL, NULL, NULL);

    soup_cookie_free(cookie);
    callback(context, true, NULL);
}

void ag_gtk_cookie_delete(ag_gtk_handle handle,
    const char* name, const char* domain, const char* path,
    ag_gtk_cookie_op_cb callback, void* context)
{
    if (!handle || !callback) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL)
    {
        callback(context, false, "Detached");
        return;
    }

    SoupCookie* cookie = soup_cookie_new(
        name ? name : "", "",
        domain ? domain : "", path ? path : "/",
        0 /* expired */);

    WebKitWebContext* web_ctx = webkit_web_view_get_context(s->web_view);
    WebKitCookieManager* cookie_mgr = webkit_web_context_get_cookie_manager(web_ctx);
    webkit_cookie_manager_delete_cookie(cookie_mgr, cookie, NULL, NULL, NULL);

    soup_cookie_free(cookie);
    callback(context, true, NULL);
}

void ag_gtk_cookies_clear_all(ag_gtk_handle handle,
                               ag_gtk_cookie_op_cb callback, void* context)
{
    if (!handle || !callback) return;
    shim_state* s = (shim_state*)handle;
    if (atomic_load(&s->detached) || s->web_view == NULL)
    {
        callback(context, false, "Detached");
        return;
    }

    WebKitWebContext* web_ctx = webkit_web_view_get_context(s->web_view);
    WebKitWebsiteDataManager* data_mgr = webkit_web_context_get_website_data_manager(web_ctx);
    webkit_website_data_manager_clear(data_mgr, WEBKIT_WEBSITE_DATA_COOKIES, 0, NULL, NULL, NULL);

    callback(context, true, NULL);
}

/* ========== Environment options ========== */

void ag_gtk_set_enable_dev_tools(ag_gtk_handle handle, bool enable)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;
    s->opt_enable_dev_tools = enable;

    /* Also apply to live WebView if already attached. */
    if (s->web_view != NULL && !atomic_load(&s->detached))
    {
        WebKitSettings* settings = webkit_web_view_get_settings(s->web_view);
        webkit_settings_set_enable_developer_extras(settings, enable);
    }
}

void ag_gtk_set_ephemeral(ag_gtk_handle handle, bool ephemeral)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;
    s->opt_ephemeral = ephemeral;
}

void ag_gtk_set_user_agent(ag_gtk_handle handle, const char* ua_utf8_or_null)
{
    if (!handle) return;
    shim_state* s = (shim_state*)handle;

    free(s->opt_user_agent);
    s->opt_user_agent = ua_utf8_or_null ? strdup(ua_utf8_or_null) : NULL;

    /* Also update live WebView if already attached. */
    if (s->web_view != NULL && !atomic_load(&s->detached))
    {
        WebKitSettings* settings = webkit_web_view_get_settings(s->web_view);
        webkit_settings_set_user_agent(settings, ua_utf8_or_null);
    }
}
