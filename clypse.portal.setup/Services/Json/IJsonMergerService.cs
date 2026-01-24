namespace clypse.portal.setup.Services.Json;

public interface IJsonMergerService
{
    public string MergeJsonStrings(
        string baseJson,
        string overrideJson);
}
