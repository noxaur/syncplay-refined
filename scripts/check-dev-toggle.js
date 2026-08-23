#!/usr/bin/env node
'use strict';

const fs = require('fs');
const path = require('path');
const { URL } = require('url');

function store() {
  const data = Object.create(null);
  return {
    getItem: (k) => (Object.prototype.hasOwnProperty.call(data, k) ? data[k] : null),
    setItem: (k, v) => { data[k] = String(v); },
    removeItem: (k) => { delete data[k]; },
    _data: data
  };
}

function installBrowserStubs(href) {
  const localStorage = store();
  const sessionStorage = store();
  const g = globalThis;
  g.window = g;
  g.document = {
    readyState: 'complete',
    documentElement: {},
    addEventListener() {},
    querySelector() { return null; },
    querySelectorAll() { return []; }
  };
  g.localStorage = localStorage;
  g.sessionStorage = sessionStorage;
  g.history = {
    replaceState(_s, _t, url) {
      g.location = new URL(url, 'http://localhost/');
    }
  };
  g.location = new URL(href);
  g.MutationObserver = function () { this.observe = function () {}; };
  g.requestAnimationFrame = (fn) => fn();
  g.URL = URL;
  g.__syncPlayRefinedRequireAuth = false;
  delete g.__syncPlayRefined;
  delete g.SyncPlayRefinedDev;
  return { localStorage, sessionStorage };
}

function loadClient() {
  const file = path.join(__dirname, '..', 'Jellyfin.Plugin.SyncPlayRefined', 'Web', 'client.js');
  // eslint-disable-next-line no-eval
  eval(fs.readFileSync(file, 'utf8'));
}

function assert(cond, msg) {
  if (!cond) {
    throw new Error(msg);
  }
}

installBrowserStubs('http://localhost/web/index.html');
loadClient();

const Dev = globalThis.SyncPlayRefinedDev;
assert(Dev, 'SyncPlayRefinedDev missing');
assert(Dev.enabled() === false, 'default off');
assert(Dev.feature('future-thing') === false, 'feature off when master off');

assert(Dev.enable() === true, 'enable');
assert(Dev.enabled() === true, 'enabled after enable');
assert(Dev.feature('future-thing') === false, 'named feature still off');

assert(Dev.setFeature('future-thing', true) === true, 'setFeature on');
assert(Dev.feature('future-thing') === true, 'feature on');
assert(Dev.status().features['future-thing'] === true, 'status lists feature');

assert(Dev.disable() === false, 'disable master');
assert(Dev.feature('future-thing') === false, 'master gates leftover features');
assert(Dev.status().features['future-thing'] === true, 'feature retained while gated');

assert(Dev.toggle() === true, 'toggle on');
assert(Dev.feature('future-thing') === true, 'feature returns after master on');
assert(Dev.toggleFeature('future-thing') === false, 'toggleFeature off');
assert(Dev.feature('future-thing') === false, 'feature off after toggleFeature');

installBrowserStubs('http://localhost/web/index.html?sprDev=1&sprFeature=alpha');
loadClient();
assert(globalThis.SyncPlayRefinedDev.enabled() === true, 'url enables master');
assert(globalThis.SyncPlayRefinedDev.feature('alpha') === true, 'url enables feature');
assert(!String(globalThis.location).includes('sprDev'), 'sprDev stripped');
assert(!String(globalThis.location).includes('sprFeature'), 'sprFeature stripped');

installBrowserStubs('http://localhost/web/index.html?sprDev=0');
globalThis.localStorage.setItem('syncplay-refined-dev', JSON.stringify({ on: true, features: { alpha: true } }));
loadClient();
assert(globalThis.SyncPlayRefinedDev.enabled() === false, 'url disables master');

console.log('check-dev-toggle: ok');
