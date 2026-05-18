namespace Shiny.Extensions.Stores.Repositories;


/// <summary>
/// Represents an entity that can be stored in an <see cref="IRepository"/>.
/// </summary>
public interface IRepositoryEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity.
    /// </summary>
    string Identifier { get; }
}
