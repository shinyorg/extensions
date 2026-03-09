using UserNotifications;

namespace Shiny;

public interface IOnFinishedLaunching
{
    void Handle(UIApplicationLaunchEventArgs args);
}

public interface INotificationHandler
{
    void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);
    void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
}

public interface IHandleEventsForBackgroundUrl
{
    bool Handle(string sessionIdentifier, Action completionHandler);
}

public interface IContinueActivity
{
    bool Handle(NSUserActivity activity, UIApplicationRestorationHandler completionHandler);

    //     ios.SceneWillConnect((scene, sceneSession, sceneConnectionOptions)
    //         => HandleAppLink(sceneConnectionOptions.UserActivities.ToArray()
    //             .FirstOrDefault(a => a.ActivityType == NSUserActivityType.BrowsingWeb)));

    //     ios.SceneContinueUserActivity((scene, userActivity)
    //         => HandleAppLink(userActivity));
}
