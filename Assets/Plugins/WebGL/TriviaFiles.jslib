mergeInto(LibraryManager.library, {
  TriviaOpenGames: function() {
    var url = 'https://iqgamesonline.com/?iqreturn=1';
    // Match the working SlideIQ behavior: keep the running game in its tab,
    // and open the IQ Games catalogue in a separate tab that can close to return.
    var tab = window.open('about:blank', '_blank');
    if (tab) {
      tab.opener = null;
      tab.location.replace(url);
      return;
    }

    // If the popup is blocked, provide a browser-native link without navigating
    // the Unity frame away from the running game.
    if (document.getElementById('iq-games-browse-dialog')) return;
    var previousFocus = document.activeElement;
    var shade = document.createElement('div');
    shade.id = 'iq-games-browse-dialog';
    shade.setAttribute('role', 'dialog');
    shade.setAttribute('aria-modal', 'true');
    shade.setAttribute('aria-label', 'Browse other IQ games');
    shade.style.cssText = 'position:fixed;inset:0;z-index:2147483647;background:rgba(0,0,0,.85);display:flex;align-items:center;justify-content:center;font:18px Arial,sans-serif;color:white;padding:24px;';
    var panel = document.createElement('div');
    panel.style.cssText = 'max-width:440px;background:#1b2034;padding:28px;border-radius:12px;line-height:1.5;';
    var message = document.createElement('p');
    message.textContent = 'Browse the other IQ games in a new tab. Your current game stays here.';
    var link = document.createElement('a');
    link.href = url;
    link.target = '_blank';
    link.rel = 'noopener noreferrer';
    link.textContent = 'Open IQ Games website';
    link.style.cssText = 'display:block;color:#c8f582;margin:20px 0;';
    var cancel = document.createElement('button');
    cancel.type = 'button';
    cancel.textContent = 'Stay in this game';
    cancel.style.cssText = 'padding:12px;font:inherit;cursor:pointer;';
    var dismiss = function () {
      shade.remove();
      if (previousFocus && previousFocus.focus) previousFocus.focus();
    };
    cancel.onclick = dismiss;
    link.onclick = function () { window.setTimeout(dismiss, 100); };
    shade.onkeydown = function (event) {
      if (event.key === 'Escape') dismiss();
      if (event.key === 'Tab') {
        event.preventDefault();
        (document.activeElement === link ? cancel : link).focus();
      }
    };
    panel.appendChild(message);
    panel.appendChild(link);
    panel.appendChild(cancel);
    shade.appendChild(panel);
    if (document.fullscreenElement && document.exitFullscreen) {
      var exit = document.exitFullscreen();
      if (exit && exit.catch) exit.catch(function () {});
    }
    document.body.appendChild(shade);
    link.focus();
  }
});
