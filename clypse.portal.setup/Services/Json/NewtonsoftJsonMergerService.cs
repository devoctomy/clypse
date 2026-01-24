using Newtonsoft.Json.Linq;

namespace clypse.portal.setup.Services.Json;

public class NewtonsoftJsonMergerService : IJsonMergerService
{
    public string MergeJsonStrings(
        string baseJsonString,
        string overrideJsonString)
    {
        var baseJson = JObject.Parse(baseJsonString);
        var overrideJson = JObject.Parse(overrideJsonString);
        baseJson.Merge(overrideJson, new JsonMergeSettings
        {
            MergeArrayHandling = MergeArrayHandling.Replace
        });

        return baseJson.ToString(Newtonsoft.Json.Formatting.Indented);
    }
}
