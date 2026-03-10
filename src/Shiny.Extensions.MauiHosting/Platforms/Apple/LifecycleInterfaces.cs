using UserNotifications;

namespace Shiny;

public interface IOnFinishedLaunching
{
    void Handle(NSDictionary launchOptions);
}

// public interface INotificationHandler
// {
//     void OnDidReceiveNotificationResponse(UNNotificationResponse response, Action completionHandler);
//     void OnWillPresentNotification(UNNotification notification, Action<UNNotificationPresentationOptions> completionHandler);
// }
//
// public interface IHandleEventsForBackgroundUrl
// {
//     bool Handle(string sessionIdentifier, Action completionHandler);
// }

public interface IContinueActivity
{
    bool Handle(NSUserActivity activity);
}
