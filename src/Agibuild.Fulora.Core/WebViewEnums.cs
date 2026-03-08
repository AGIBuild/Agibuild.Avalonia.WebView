namespace Agibuild.Fulora;

#pragma warning disable CS1591

public enum NavigationCompletedStatus
{
    Success,
    Failure,
    Canceled,
    Superseded
}

public enum WebAuthStatus
{
    Success,
    UserCancel,
    Timeout,
    Error
}

public enum WebMessageDropReason
{
    OriginNotAllowed,
    ProtocolMismatch,
    ChannelMismatch
}

public enum WebViewOperationFailureCategory
{
    Disposed,
    NotReady,
    DispatchFailed,
    AdapterFailed
}

public enum ContextMenuMediaType
{
    None,
    Image,
    Video,
    Audio
}

public enum WebViewCommand
{
    Copy,
    Cut,
    Paste,
    SelectAll,
    Undo,
    Redo
}

public enum WebViewPermissionKind
{
    Unknown = 0,
    Camera,
    Microphone,
    Geolocation,
    Notifications,
    ClipboardRead,
    ClipboardWrite,
    Midi,
    Sensors,
    Other
}

public enum PermissionState
{
    Default = 0,
    Allow,
    Deny
}

#pragma warning restore CS1591
