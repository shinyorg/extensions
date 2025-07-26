// namespace Shiny.Extensions.Stores;
//
//
// public class FileKeyValueStore(ISerializer serializer) : IKeyValueStore
// {
//     public string Alias => "file";
//     public bool IsReadOnly => false;
//     
//     public bool Remove(string key)
//     {
//         return false;
//     }
//
//     public void Clear()
//     {
//     }
//
//     public bool Contains(string key)
//     {
//         return false;
//     }
//
//     public object? Get(Type type, string key)
//     {
//         return null;
//     }
//
//     public void Set(string key, object value)
//     {
//         
//     }
//
//
//     void Transaction(Action<Dictionary<string, string>> action)
//     {
//         
//     }
// }