namespace MinimalHybrid.Bridge;

public record UserProfile(string Name, string Email, string Role);

public record AppSettings(string Theme, bool DarkMode);

public record Item(string Id, string Title, string Description);
