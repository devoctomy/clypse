namespace clypse.core.Data;

public class DataPrefetchService : IDataPrefetchService
{
    private readonly Dictionary<string, List<string>> prefetchedData = [];

    public List<string> GetPrefetchedLines(string key)
    {
        return this.prefetchedData.ContainsKey(key) ? this.prefetchedData[key] : [];
    }

    public bool HasPrefetchedLines(string key)
    {
        return this.prefetchedData.ContainsKey(key) && this.prefetchedData[key].Count > 0;
    }

    public void PrefetchLines(
        string key,
        IEnumerable<string> lines)
    {
        this.prefetchedData.Add(key, lines.ToList());
    }
}
