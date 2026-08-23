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

  function captureJoinFromUrl() {
    try {
      var url = new URL(window.location.href);
      var id = url.searchParams.get(PARAM);
      if (!id) {
        return;
      }
      sessionStorage.setItem(STORAGE_KEY, id);
      url.searchParams.delete(PARAM);
      history.replaceState(null, '', url.toString());
    } catch (e) {
      /* ignore */
    }
  }

  function getJoinTarget() {
    try {
      return sessionStorage.getItem(STORAGE_KEY);
    } catch (e) {
      return null;
    }
  }

  function clearJoinTarget() {
    try {
      sessionStorage.removeItem(STORAGE_KEY);
    } catch (e) {
      /* ignore */
    }
  }

  function getApiClient() {
    if (window.ApiClient) {
      return window.ApiClient;
    }
    try {
      return window.ServerConnections && window.ServerConnections.currentApiClient
        ? window.ServerConnections.currentApiClient()
        : null;
    } catch (e) {
      return null;
    }
  }

  function isLoggedIn(api) {
    if (!api) {
      return false;
    }
    try {
      if (typeof api.getCurrentUserId === 'function' && api.getCurrentUserId()) {
        return true;
      }
      if (typeof api.accessToken === 'function' && api.accessToken()) {
        return true;
      }
    } catch (e) {
      /* ignore */
    }
    return false;
  }

  function getSyncPlayManager() {
    var pm = window.pluginManager;
    if (!pm || typeof pm.firstOfType !== 'function') {
      return null;
    }
    var plugin = pm.firstOfType('syncPlay') || pm.firstOfType('SyncPlay');
    return plugin && plugin.instance ? plugin.instance.Manager : null;
  }

  function groupIdOf(group) {
    return (group && (group.GroupId || group.groupId || group.Id)) || null;
  }

  function parseGroups(data) {
    if (typeof data === 'string') {
      try {
        data = JSON.parse(data);
      } catch (e) {
        return [];
      }
    }
    if (Array.isArray(data)) {
      return data;
    }
    if (data && Array.isArray(data.Items)) {
      return data.Items;
    }
    return [];
  }

  function listGroups(api) {
    var req;
    if (typeof api.ajax === 'function') {
      req = api.ajax({
        type: 'GET',
        url: api.getUrl('SyncPlay/List'),
        dataType: 'json'
      });
    } else {
      var headers = {};
      var token = typeof api.accessToken === 'function' ? api.accessToken() : '';
      if (token) {
        headers.Authorization = 'MediaBrowser Token="' + token + '"';
        headers['X-Emby-Token'] = token;
      }
      req = fetch(api.getUrl('SyncPlay/List'), { headers: headers }).then(function (res) {
        if (!res.ok) {
          throw new Error('List failed: ' + res.status);
        }
        return res.json();
      });
    }
    return Promise.resolve(req).then(parseGroups);
  }

  function currentUserName(api) {
    try {
      if (api._currentUser && api._currentUser.Name) {
        return api._currentUser.Name;
      }
    } catch (e) {
      /* ignore */
    }
    return null;
  }

  function participantName(p) {
    return typeof p === 'string' ? p : (p && (p.Name || p.name)) || '';
  }

  function groupsForUser(groups, name) {
    if (!name) {
      return [];
    }
    var lower = name.toLowerCase();
    return groups.filter(function (g) {
      var parts = g.Participants || g.participants || [];
      for (var i = 0; i < parts.length; i++) {
        if (participantName(parts[i]).toLowerCase() === lower) {
          return true;
        }
      }
      return false;
    });
  }

  function menuGroupName() {
    var title = document.querySelector('.syncPlayGroupMenu .actionSheetTitle, .actionSheetTitle');
    return title ? String(title.textContent || '').trim() : '';
  }

  function pickGroup(groups, name) {
    var mine = groupsForUser(groups, name);
    var title = menuGroupName();
    var pool = mine.length ? mine : groups;
    if (title) {
      for (var i = 0; i < pool.length; i++) {
        if ((pool[i].GroupName || pool[i].groupName || '') === title) {
          return pool[i];
        }
      }
    }
    if (mine.length) {
      return mine[0];
    }
    if (groups.length === 1) {
      return groups[0];
    }
    return null;
  }

  function getGroupIdFromManager() {
    var mgr = getSyncPlayManager();
    if (!mgr) {
      return null;
    }
    var info = typeof mgr.getGroupInfo === 'function' ? mgr.getGroupInfo() : null;
    if (!info) {
      return null;
    }
    return info.GroupId || info.groupId || info.Id || null;
  }

  function resolveGroupId() {
    var id = getGroupIdFromManager();
    if (id) {
      return Promise.resolve(id);
    }
    var api = getApiClient();
    if (!api) {
      return Promise.resolve(null);
    }
    var named = currentUserName(api);
    var userP = named
      ? Promise.resolve(named)
      : (typeof api.getCurrentUser === 'function'
        ? Promise.resolve(api.getCurrentUser()).then(function (u) {
          return u && u.Name;
        })
        : Promise.resolve(null));
    return Promise.all([listGroups(api), userP]).then(function (pair) {
      return groupIdOf(pickGroup(pair[0], pair[1]));
    }).catch(function () {
      return null;
    });
  }

  function buildInviteLink(groupId) {
    var url = new URL(window.location.href);
    url.searchParams.set(PARAM, groupId);
    return url.toString();
  }

  function joinGroup(api, groupId) {
    if (typeof api.joinSyncPlayGroup === 'function') {
      return api.joinSyncPlayGroup({ GroupId: groupId });
    }
    var body = JSON.stringify({ GroupId: groupId });
    if (typeof api.ajax === 'function') {
      return api.ajax({
        type: 'POST',
        url: api.getUrl('SyncPlay/Join'),
        data: body,
        contentType: 'application/json'
      });
    }
    var headers = { 'Content-Type': 'application/json' };
    var token = typeof api.accessToken === 'function' ? api.accessToken() : '';
    if (token) {
      headers.Authorization = 'MediaBrowser Token="' + token + '"';
      headers['X-Emby-Token'] = token;
    }
    return fetch(api.getUrl('SyncPlay/Join'), {
      method: 'POST',
      headers: headers,
      body: body
    }).then(function (res) {
      if (!res.ok) {
        throw new Error('Join failed: ' + res.status);
      }
    });
  }

  function resumePlayback(api) {
    var mgr = getSyncPlayManager();
    if (!mgr) {
      return;
    }
    if (typeof mgr.resumeGroupPlayback === 'function') {
      mgr.resumeGroupPlayback(api);
    }
  }

  function notify(text) {
    try {
      if (typeof window.require === 'function') {
        window.require(['components/toast/toast'], function (toast) {
          if (typeof toast === 'function') {
            toast({ text: text });
          }
        });
        return true;
      }
    } catch (e) {
      /* ignore */
    }
    return false;
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
      return copyText(buildInviteLink(groupId)).then(function () {
        notify('Invite link copied');
        setLabel(el, 'Copied');
      }).catch(function () {
        setLabel(el, 'Copy failed');
      });
    });
  }

  function replaceItemText(node, primary, secondary) {
    var primaryEl = node.querySelector('.MuiListItemText-primary, .listItemBodyText');
    var secondaryEl = node.querySelector('.MuiListItemText-secondary, .secondaryText, .listItemBodyText.secondary');
    if (primaryEl) {
      primaryEl.setAttribute(LABEL_ATTR, '1');
      primaryEl.textContent = primary;
    } else {
      node.setAttribute(LABEL_ATTR, '1');
      node.textContent = primary;
    }
    if (secondaryEl) {
      secondaryEl.textContent = secondary || '';
    }
  }

  function injectMuiMenu(menu) {
    if (menu.querySelector('[' + SHARE_ATTR + ']')) {
      return;
    }
    var labelled = menu.getAttribute('aria-labelledby') === 'sync-play-active-subheader'
      || !!(menu.querySelector('#sync-play-active-subheader'))
      || !!(menu.querySelector('[aria-labelledby="sync-play-active-subheader"]'));
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
    } else if (template) {
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
    var groupId = getJoinTarget();
    if (!groupId || joining) {
      return;
    }
    var api = getApiClient();
    if (!api || !isLoggedIn(api)) {
      return;
    }
    joining = true;
    Promise.resolve(joinGroup(api, groupId)).then(function () {
      clearJoinTarget();
      window.setTimeout(function () {
        resumePlayback(api);
      }, 400);
      notify('Joined SyncPlay group');
    }).catch(function (err) {
      console.warn('[SyncPlay Refined] join failed', err);
      notify('Could not join SyncPlay group');
    }).then(function () {
      joining = false;
    });
  }

  function start() {
    captureJoinFromUrl();
    tryJoin();
    var ticks = 0;
    var timer = window.setInterval(function () {
      tryJoin();
      ticks += 1;
      if (!getJoinTarget() || ticks > 240) {
        window.clearInterval(timer);
      }
    }, 500);

    var scheduled = false;
    var obs = new MutationObserver(function () {
      if (scheduled) {
        return;
      }
      scheduled = true;
      requestAnimationFrame(function () {
        scheduled = false;
        scan();
      });
    });
    obs.observe(document.documentElement, { childList: true, subtree: true });
    scan();
    document.addEventListener('viewshow', tryJoin, true);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', start);
  } else {
    start();
  }
})();
