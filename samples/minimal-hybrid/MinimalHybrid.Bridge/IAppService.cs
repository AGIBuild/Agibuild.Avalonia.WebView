using Agibuild.Fulora;

namespace MinimalHybrid.Bridge;

[JsExport]
public interface IAppService
{
    Task<UserProfile> GetCurrentUser();
    Task SaveSettings(AppSettings settings);
    Task<List<Item>> SearchItems(string query, int limit);
}
