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
    removeItem: (k) => { delete data[k]; }
  };
}

function installBrowserStubs() {
  const g = globalThis;
  g.window = g;
  g.document = {
    readyState: 'complete',
    documentElement: {},
    addEventListener() {},
    querySelector() { return null; },
    querySelectorAll() { return []; }
  };
  g.localStorage = store();
  g.sessionStorage = store();
  g.history = { replaceState() {} };
  g.location = new URL('http://localhost/web/index.html');
  g.MutationObserver = function () { this.observe = function () {}; };
  g.requestAnimationFrame = (fn) => fn();
  g.setInterval = function () { return 0; };
  g.clearInterval = function () {};
  g.setTimeout = function () { return 0; };
  g.XMLHttpRequest = function () {};
  g.XMLHttpRequest.prototype = { open: function () {} };
  g.URL = URL;
  g.__syncPlayRefinedRequireAuth = false;
  delete g.__syncPlayRefined;
  delete g.__syncPlayRefinedDev;
  delete g.SyncPlayRefinedDev;
}

function loadClient(prefix) {
  const file = path.join(__dirname, '..', 'Jellyfin.Plugin.SyncPlayRefined', 'Web', 'client.js');
  // eslint-disable-next-line no-eval
  eval((prefix || '') + fs.readFileSync(file, 'utf8'));
}

function assert(cond, msg) {
  if (!cond) {
    throw new Error(msg);
  }
}

installBrowserStubs();
globalThis.localStorage.setItem('syncplay-refined-dev', JSON.stringify({ on: true, features: { leftover: true } }));
loadClient();

let Dev = globalThis.SyncPlayRefinedDev;
assert(Dev, 'SyncPlayRefinedDev missing');
assert(Dev.enabled() === false, 'default off when server flag omitted');
assert(Dev.feature('future-thing') === false, 'feature off when master off');
assert(Dev.enable === undefined, 'no per-browser enable()');
assert(Dev.setFeature === undefined, 'no per-browser setFeature()');

globalThis.__syncPlayRefinedDev = true;
assert(Dev.enabled() === true, 'reads live server flag');
assert(Dev.feature('future-thing') === true, 'feature follows master');
assert(Dev.feature('') === false, 'empty feature name is off');

globalThis.__syncPlayRefinedDev = false;
assert(Dev.enabled() === false, 'live flag off');
assert(Dev.feature('future-thing') === false, 'feature off after master off');

installBrowserStubs();
loadClient('window.__syncPlayRefinedRequireAuth=false;window.__syncPlayRefinedDev=true;\n');
Dev = globalThis.SyncPlayRefinedDev;
assert(Dev.enabled() === true, 'injected prefix enables master');
assert(Dev.feature('alpha') === true, 'injected prefix enables features');

installBrowserStubs();
loadClient('window.__syncPlayRefinedRequireAuth=false;window.__syncPlayRefinedDev=false;\n');
assert(globalThis.SyncPlayRefinedDev.enabled() === false, 'injected prefix disables master');

console.log('check-dev-toggle: ok');
