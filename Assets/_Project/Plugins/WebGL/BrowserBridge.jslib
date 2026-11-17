// JavaScript side of SayHeyDash.Platform.BrowserBridge.
// Keep functions small, side-effect free where possible, and prefixed with SHD_.
mergeInto(LibraryManager.library, {

  SHD_IsEmbedded: function () {
    try {
      return window.self !== window.top ? 1 : 0;
    } catch (e) {
      // Cross-origin access to window.top throws => we are definitely framed.
      return 1;
    }
  },

  SHD_IsTouchDevice: function () {
    return (('ontouchstart' in window) || (navigator.maxTouchPoints > 0)) ? 1 : 0;
  },

  SHD_HasFocus: function () {
    return document.hasFocus() ? 1 : 0;
  },

  SHD_GetDevicePixelRatio: function () {
    return window.devicePixelRatio || 1;
  },

  // Popups are only allowed during a real user gesture. Unity processes input one frame
  // after the browser event, so we defer window.open to the next pointerup/keyup.
  SHD_OpenUrlOnNextGesture: function (urlPtr) {
    var url = UTF8ToString(urlPtr);
    var open = function () {
      document.removeEventListener('pointerup', open, true);
      document.removeEventListener('keyup', open, true);
      // Note: passing 'noopener' as a feature makes window.open always return null,
      // so we detach the opener manually instead to keep blocked-popup detection reliable.
      var win = window.open(url, '_blank');
      if (win) {
        try { win.opener = null; } catch (e) { /* ignore */ }
      } else {
        console.warn('[SayHeyDash] window.open was blocked (iframe sandbox may lack allow-popups): ' + url);
      }
    };
    document.addEventListener('pointerup', open, true);
    document.addEventListener('keyup', open, true);
  }
});
