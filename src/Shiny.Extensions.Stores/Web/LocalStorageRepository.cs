using Microsoft.JSInterop;
using Shiny.Extensions.Stores.Repositories;

namespace Shiny.Extensions.Stores.Web;


/// <summary>
/// <see cref="IRepository"/> implementation backed by the browser's <c>window.localStorage</c>.
/// Blazor WebAssembly only - requires <see cref="IJSInProcessRuntime"/>.
/// </summary>
public class LocalStorageRepository(IJSRuntime jsRuntime, ISerializer serializer) : IRepository
{
    const string KeyPrefix = "shiny:repo:";

    readonly LocalStorage storage = new(jsRuntime);


    public event EventHandler<(RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity)>? ActionOccurred;


    public bool Exists<TEntity>(string identifier) where TEntity : IRepositoryEntity
        => this.storage.ContainsKey(GetKey<TEntity>(identifier));


    public TEntity? Get<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var json = this.storage.GetItem(GetKey<TEntity>(identifier));
        if (json == null)
            return default;

        return serializer.Deserialize<TEntity>(json);
    }


    public IReadOnlyList<TEntity> GetAll<TEntity>() where TEntity : IRepositoryEntity
    {
        var keys = this.storage.GetKeys(GetTypePrefix<TEntity>());
        var items = new List<TEntity>(keys.Length);

        foreach (var key in keys)
        {
            var json = this.storage.GetItem(key);
            if (json != null)
            {
                var entity = serializer.Deserialize<TEntity>(json);
                if (entity != null)
                    items.Add(entity);
            }
        }

        return items;
    }


    public bool Set<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        var exists = this.storage.ContainsKey(key);
        this.storage.SetItem(key, serializer.Serialize<TEntity>(entity));

        var action = exists ? RepositoryAction.Update : RepositoryAction.Add;
        this.ActionOccurred?.Invoke(this, (action, typeof(TEntity), entity));
        return exists;
    }


    public void Insert<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        if (this.storage.ContainsKey(key))
            throw new RepositoryException($"{typeof(TEntity).FullName} already has a record with identifier '{entity.Identifier}'");

        this.storage.SetItem(key, serializer.Serialize<TEntity>(entity));
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Add, typeof(TEntity), entity));
    }


    public void Update<TEntity>(TEntity entity) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(entity.Identifier);
        if (!this.storage.ContainsKey(key))
            throw new RepositoryException($"{typeof(TEntity).FullName} - no record exists with identifier '{entity.Identifier}'");

        this.storage.SetItem(key, serializer.Serialize<TEntity>(entity));
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Update, typeof(TEntity), entity));
    }


    public bool Remove<TEntity>(string identifier) where TEntity : IRepositoryEntity
    {
        var key = GetKey<TEntity>(identifier);
        var entity = this.Get<TEntity>(identifier);
        var removed = this.storage.RemoveItem(key);

        if (removed && entity != null)
            this.ActionOccurred?.Invoke(this, (RepositoryAction.Remove, typeof(TEntity), entity));

        return removed;
    }


    public void Clear<TEntity>() where TEntity : IRepositoryEntity
    {
        this.storage.RemoveKeys(GetTypePrefix<TEntity>());
        this.ActionOccurred?.Invoke(this, (RepositoryAction.Clear, typeof(TEntity), default));
    }


    static string GetTypePrefix<TEntity>() => $"{KeyPrefix}{typeof(TEntity).Name}:";
    static string GetKey<TEntity>(string identifier) => $"{GetTypePrefix<TEntity>()}{identifier}";
}
