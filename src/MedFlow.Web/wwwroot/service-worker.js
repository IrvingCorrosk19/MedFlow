/* MedFlow — SW mínimo: cache ligero de shell estático (fase PWA). */
var CACHE = "medflow-shell-v1";
var CORE = ["/css/site.css", "/manifest.webmanifest"];

self.addEventListener("install", function (e) {
  e.waitUntil(
    caches.open(CACHE).then(function (cache) {
      return cache.addAll(CORE.map(function (u) { return new Request(u, { cache: "reload" }); })).catch(function () {});
    })
  );
  self.skipWaiting();
});

self.addEventListener("activate", function (e) {
  e.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", function (e) {
  var req = e.request;
  if (req.method !== "GET") return;
  var url = req.url;
  if (url.indexOf("/css/") === -1 && url.indexOf("/manifest.webmanifest") === -1) return;
  e.respondWith(
    caches.match(req).then(function (hit) {
      return hit || fetch(req);
    })
  );
});
