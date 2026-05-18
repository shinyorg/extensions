using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Extensions.Stores.Repositories.Impl;

namespace Shiny;


/// <summary>
/// Service registration and convenience extensions for <see cref="IRepository"/>.
/// </summary>
public static class RepositoryExtensions
{
    /// <summary>
    /// Registers the default JSON filesystem repository.
    /// If <paramref name="rootDirectory"/> is omitted, the directory under
    /// <see cref="Environment.SpecialFolder.LocalApplicationData"/>/Shiny is used.
    /// </summary>
    public static IServiceCollection AddDefaultRepository(this IServiceCollection services, DirectoryInfo? rootDirectory = null)
    {
        services.AddShinyStores();

        var dir = rootDirectory ?? new DirectoryInfo(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Shiny"
        ));

        services.TryAddSingleton<IRepository>(sp => new FileSystemRepository(
            dir,
            sp.GetRequiredService<ISerializer>(),
            sp.GetRequiredService<ILogger<FileSystemRepository>>()
        ));
        return services;
    }


    /// <summary>
    /// Removes the given entity from the repository by its identifier.
    /// </summary>
    public static bool Remove<T>(this IRepository repository, T item) where T : IRepositoryEntity
        => repository.Remove<T>(item.Identifier);
}
