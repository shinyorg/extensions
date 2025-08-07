let handler;

window.ShinyServices = {
    isOnline: function() {
        return navigator.onLine;
    },

    subscribe: function(interop) {

        handler = function() {
            interop.invokeMethodAsync("ShinyServices.OnStatusChanged", navigator.onLine);
        }

        window.addEventListener("online", handler);
        window.addEventListener("offline", handler);
    },

    unsubscribe: function() {
        if (handler == null)
            return;

        window.removeEventListener("online", handler);
        window.removeEventListener("offline", handler);
    },
    
    
    getUserAgent: function() {
        return navigator.userAgent; 
    },
    
    // TODO: battery
    // navigator.getBattery().then((battery) => {
    //     batteryIsCharging = battery.charging;
    //
    //     battery.addEventListener("chargingchange", () => {
    //         batteryIsCharging = battery.charging;
    //     });
    // });

    // navigator.vibrate(200); // vibrate for 200ms
    // navigator.vibrate([
    //     100, 30, 100, 30, 100, 30, 200, 30, 200, 30, 200, 30, 100, 30, 100, 30, 100,
    // ]); // Vibrate 'SOS' in Morse.
    
    // TODO: culture
    getLocale: function() {
        if (navigator.languages !== undefined)
            return navigator.languages[0];
        return navigator.language;
    },

    // TODO: timezone offset
    getTimezoneOffset: function() {
        return new Date().getTimezoneOffset();
    }
};