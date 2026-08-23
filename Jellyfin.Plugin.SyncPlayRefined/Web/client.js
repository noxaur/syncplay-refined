(function () {
  'use strict';

  if (window.__syncPlayRefined) {
    return;
  }
  window.__syncPlayRefined = true;

  var PARAM = 'syncplayGroup';
  var STORAGE_KEY = 'syncplay-refined-join';
  var SHARE_ATTR = 'data-syncplay-refined-share';
  var LABEL_ATTR = 'data-syncplay-refined-label';

  // Server injects window.__syncPlayRefinedDev from plugin settings. Off unless that is true.
  function isDevOn() {
    return window.__syncPlayRefinedDev === true;
  }

  window.SyncPlayRefinedDev = {
    enabled: isDevOn,
    feature: function (name) {
      return !!name && isDevOn();
    }
  };

  function api() {
    return window.ApiClient || (window.ServerConnections && window.ServerConnections.currentApiClient && window.ServerConnections.currentApiClient()) || null;
  }

  function isAuthed() {
    var a = api();
    try {
      return !!(a && ((typeof a.getCurrentUserId === 'function' && a.getCurrentUserId()) || (typeof a.accessToken === 'function' && a.accessToken())));
    } catch (e) {
      return false;
    }
  }

  function manager() {
    var pm = window.pluginManager;
    var plugin = pm && typeof pm.firstOfType === 'function' && (pm.firstOfType('syncPlay') || pm.firstOfType('SyncPlay'));
    return plugin && plugin.instance ? plugin.instance.Manager : null;
  }

  function gid(g) {
    return (g && (g.GroupId || g.groupId || g.Id)) || null;
  }

  function joinTarget() {
    try {
      return sessionStorage.getItem(STORAGE_KEY);
    } catch (e) {
      return null;
    }
  }

  function resolveGroupId() {
    var mgr = manager();
    var id = gid(mgr && typeof mgr.getGroupInfo === 'function' && mgr.getGroupInfo());
    if (id) {
      return Promise.resolve(id);
    }
    var a = api();
    if (!a || typeof a.ajax !== 'function') {
      return Promise.resolve(null);
    }
    var name = ((a._currentUser && a._currentUser.Name) || '').toLowerCase();
    return a.ajax({ type: 'GET', url: a.getUrl('SyncPlay/List'), dataType: 'json' }).then(function (groups) {
      if (!Array.isArray(groups)) {
        groups = [];
      }
      var mine = [];
      var i;
      var j;
      for (i = 0; i < groups.length; i++) {
        var parts = groups[i].Participants || groups[i].participants || [];
        for (j = 0; j < parts.length; j++) {
          var p = parts[j];
          var pn = typeof p === 'string' ? p : (p && (p.Name || p.name)) || '';
          if (name && pn.toLowerCase() === name) {
            mine.push(groups[i]);
            break;
          }
        }
      }
      var titleEl = document.querySelector('.syncPlayGroupMenu .actionSheetTitle, .actionSheetTitle');
      var title = titleEl ? String(titleEl.textContent || '').trim() : '';
      var pool = mine.length ? mine : groups;
      if (title) {
        for (i = 0; i < pool.length; i++) {
          if ((pool[i].GroupName || pool[i].groupName || '') === title) {
            return gid(pool[i]);
          }
        }
      }
      if (mine.length) {
        return gid(mine[0]);
      }
      return groups.length === 1 ? gid(groups[0]) : null;
    }).catch(function () {
      return null;
    });
  }

  function joinGroup(a, groupId) {
    if (typeof a.joinSyncPlayGroup === 'function') {
      return a.joinSyncPlayGroup({ GroupId: groupId });
    }
    return a.ajax({
      type: 'POST',
      url: a.getUrl('SyncPlay/Join'),
      data: JSON.stringify({ GroupId: groupId }),
      contentType: 'application/json'
    });
  }

  function copyText(text) {
    if (navigator.clipboard && typeof navigator.clipboard.writeText === 'function') {
      return navigator.clipboard.writeText(text);
    }
    return new Promise(function (resolve, reject) {
      var ta = document.createElement('textarea');
      ta.value = text;
      ta.setAttribute('readonly', '');
      ta.style.position = 'fixed';
      ta.style.left = '-9999px';
      document.body.appendChild(ta);
      ta.select();
      try {
        if (document.execCommand('copy')) {
          resolve();
        } else {
          reject(new Error('copy failed'));
        }
      } catch (e) {
        reject(e);
      } finally {
        ta.remove();
      }
    });
  }

  function setLabel(el, text) {
    var label = el.querySelector('[' + LABEL_ATTR + ']') || el;
    var original = el.getAttribute('data-original-label') || label.textContent;
    el.setAttribute('data-original-label', original);
    label.textContent = text;
    window.setTimeout(function () {
      label.textContent = original;
    }, 1600);
  }

  function onShareClick(ev) {
    ev.preventDefault();
    ev.stopPropagation();
    var el = ev.currentTarget;
    resolveGroupId().then(function (groupId) {
      if (!groupId) {
        setLabel(el, 'Not in a group');
        return;
      }
      var url = new URL(window.location.href);
      url.searchParams.set(PARAM, groupId);
      return copyText(url.toString()).then(function () {
        setLabel(el, 'Copied');
      }).catch(function () {
        setLabel(el, 'Copy failed');
      });
    });
  }

  function replaceItemText(node, primary, secondary) {
    var primaryEl = node.querySelector('.MuiListItemText-primary, .listItemBodyText');
    var secondaryEl = node.querySelector('.MuiListItemText-secondary, .secondaryText, .listItemBodyText.secondary');
    (primaryEl || node).setAttribute(LABEL_ATTR, '1');
    (primaryEl || node).textContent = primary;
    if (secondaryEl) {
      secondaryEl.textContent = secondary || '';
    }
  }

  function injectMuiMenu(menu) {
    if (menu.querySelector('[' + SHARE_ATTR + ']')) {
      return;
    }
    var labelled = menu.getAttribute('aria-labelledby') === 'sync-play-active-subheader'
      || !!menu.querySelector('#sync-play-active-subheader')
      || !!menu.querySelector('[aria-labelledby="sync-play-active-subheader"]');
    if (!labelled) {
      return;
    }
    var items = menu.querySelectorAll('[role="menuitem"]');
    if (!items.length) {
      return;
    }
    var share = items[0].cloneNode(true);
    share.setAttribute(SHARE_ATTR, '1');
    share.removeAttribute('id');
    replaceItemText(share, 'Copy invite link', 'Share a link that joins this group');
    share.addEventListener('click', onShareClick);
    if (items.length >= 3) {
      items[0].after(share);
    } else {
      items[0].before(share);
    }
  }

  function injectActionSheet(root) {
    if (root.querySelector('[' + SHARE_ATTR + ']')) {
      return;
    }
    var inGroup = root.querySelector('[data-id="leave-group"], [data-id="settings"], [data-id="resume-playback"], [data-id="halt-playback"]');
    if (!inGroup) {
      return;
    }
    var anchor = root.querySelector('[data-id="resume-playback"], [data-id="halt-playback"]');
    var template = anchor || root.querySelector('[data-id="settings"]') || inGroup;
    var share = template.cloneNode(true);
    share.setAttribute(SHARE_ATTR, '1');
    share.setAttribute('data-id', 'syncplay-refined-share');
    replaceItemText(share, 'Copy invite link', 'Share a link that joins this group');
    share.addEventListener('click', onShareClick);
    if (anchor) {
      anchor.after(share);
    } else {
      template.before(share);
    }
  }

  function scan() {
    var mui = document.querySelector('#app-sync-play-menu');
    if (mui) {
      injectMuiMenu(mui);
    }
    document.querySelectorAll('.syncPlayGroupMenu').forEach(injectActionSheet);
  }

  var joining = false;
  function tryJoin() {
    var groupId = joinTarget();
    var a = api();
    if (!groupId || joining || !isAuthed()) {
      return;
    }
    joining = true;
    Promise.resolve(joinGroup(a, groupId)).then(function () {
      try {
        sessionStorage.removeItem(STORAGE_KEY);
      } catch (e) {
        /* ignore */
      }
      window.setTimeout(function () {
        var mgr = manager();
        if (mgr && typeof mgr.resumeGroupPlayback === 'function') {
          mgr.resumeGroupPlayback(a);
        }
      }, 400);
    }).catch(function (err) {
      console.warn('[SyncPlay Refined] join failed', err);
    }).then(function () {
      joining = false;
    });
  }

  function captureInvite() {
    try {
      var url = new URL(window.location.href);
      var id = url.searchParams.get(PARAM);
      if (id) {
        sessionStorage.setItem(STORAGE_KEY, id);
        url.searchParams.delete(PARAM);
        history.replaceState(null, '', url.toString());
      }
    } catch (e) {
      /* ignore */
    }
  }

  function start() {
    captureInvite();
    tryJoin();
    var ticks = 0;
    var timer = window.setInterval(function () {
      tryJoin();
      ticks += 1;
      if (!joinTarget() || ticks > 240) {
        window.clearInterval(timer);
      }
    }, 500);

    var scheduled = false;
    new MutationObserver(function () {
      if (scheduled) {
        return;
      }
      scheduled = true;
      requestAnimationFrame(function () {
        scheduled = false;
        scan();
      });
    }).observe(document.documentElement, { childList: true, subtree: true });
    scan();
    document.addEventListener('viewshow', tryJoin, true);
  }

  function boot() {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', start);
    } else {
      start();
    }
  }

  captureInvite();
  if (window.__syncPlayRefinedRequireAuth === false || isAuthed()) {
    boot();
  } else {
    var authTimer = window.setInterval(function () {
      if (isAuthed()) {
        window.clearInterval(authTimer);
        boot();
      }
    }, 300);
  }
})();
