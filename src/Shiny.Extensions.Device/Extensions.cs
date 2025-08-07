namespace Shiny.Extensions.Device;

public static class Extensions
{
    public static async Task WaitForAvailable(this IConnectivity conn, CancellationToken cancelToken = default)
    {
        // try
        // {
        //     await using var _ = cancelToken.Register(() => this.waitSource?.TrySetCanceled());
        //     this.waitSource = new();
        //     await this.waitSource.Task.ConfigureAwait(false);
        // }
        // finally
        // {
        //     this.waitSource = null;
        // }
    }    
}