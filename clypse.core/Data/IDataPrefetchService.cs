namespace clypse.core.Data;

public interface IDataPrefetchService
{
    public bool HasPrefetchedLines(string key);

    public List<string> GetPrefetchedLines(string key);

    public void PrefetchLines(string key, IEnumerable<string> lines);
}
