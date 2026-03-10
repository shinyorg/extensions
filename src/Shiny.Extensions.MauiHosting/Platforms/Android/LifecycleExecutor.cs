using Android.Content;
using Android.Content.PM;
using Microsoft.Extensions.Logging;

namespace Shiny;


public partial interface ILifecycleExecutor
{
    void OnApplicationCreated(Application application);
    
    void OnActivityOnCreate(Activity activity, Bundle? savedInstanceState);

    void OnActivityRequestPermissionResult(Activity activity, int requestCode, string[] permissions, Permission[] grantResults);

    void OnActivityNewIntent(Activity activity, Intent intent);

    void OnActivityResult(Activity activity, int requestCode, Result resultCode, Intent data);
}

public partial class LifecycleExecutor(    
    ILogger<LifecycleExecutor> logger,
    IServiceProvider services
) : ILifecycleExecutor
{
    public void OnApplicationCreated(Application application)
        => this.Execute<IOnApplicationCreated>(x => x.Handle(application));

    public void OnActivityOnCreate(Activity activity, Bundle? savedInstanceState) 
        => this.Execute<IOnActivityOnCreate>(x => x.ActivityOnCreate(activity, savedInstanceState));

    public void OnActivityRequestPermissionResult(Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
        => this.Execute<IOnActivityRequestPermissionsResult>(x => x.Handle(activity, requestCode, permissions, grantResults));
    
    public void OnActivityNewIntent(Activity activity, Intent intent)
        => this.Execute<IOnActivityNewIntent>(x => x.Handle(activity, intent));

    public void OnActivityResult(Activity activity, int requestCode, Result resultCode, Intent data)
        => this.Execute<IOnActivityResult>(x => x.Handle(activity, requestCode, resultCode, data));
}