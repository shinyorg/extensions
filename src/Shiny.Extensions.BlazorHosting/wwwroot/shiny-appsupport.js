// Shiny app-support helpers for Blazor WebAssembly.
// Attached to window so IJSInProcessRuntime can invoke them synchronously.
window.shinyAppSupport = {
    getUserAgent: function () {
        return navigator.userAgent;
    },
    getScreenWidth: function () {
        return window.screen.width;
    },
    getScreenHeight: function () {
        return window.screen.height;
    },
    getBrowserWidth: function () {
        return window.innerWidth;
    },
    getBrowserHeight: function () {
        return window.innerHeight;
    }
};
