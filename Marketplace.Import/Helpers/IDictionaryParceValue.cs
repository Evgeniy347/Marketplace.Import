namespace Marketplace.Import.Helpers
{
    public interface IDictionaryParceValue
    {
        bool TryGetValue(string key, out string value);
    }
}
