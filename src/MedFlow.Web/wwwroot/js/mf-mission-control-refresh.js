/**
 * Mission Control — opciones:
 * 1) Recarga completa cada 3 min (ligera pero pesada).
 * 2) Solo KPIs vía JSON (/Dashboard/KpiSnapshot) sin recargar gráficos.
 */
(function () {
  var KEY_RELOAD = "mfMissionControlAutoRefresh";
  var KEY_FETCH = "mfMissionControlFetchKpi";
  var INTERVAL_MS = 180000;

  function getDays() {
    var el = document.getElementById("mfDashboardDays");
    return el && el.value ? parseInt(el.value, 10) || 14 : 14;
  }

  function applyKpiPayload(data) {
    if (!data) return;
    document.querySelectorAll("[data-mf-kpi]").forEach(function (el) {
      var k = el.getAttribute("data-mf-kpi");
      if (!k || data[k] === undefined || data[k] === null) return;
      el.textContent =
        typeof data[k] === "number" ? String(data[k]) : String(data[k]);
    });
  }

  function fetchKpis() {
    var days = getDays();
    var url =
      (window.MedFlowDashboardKpiUrl ||
        "/Dashboard/KpiSnapshot") +
      "?days=" +
      encodeURIComponent(days);
    fetch(url, { credentials: "same-origin", headers: { Accept: "application/json" } })
      .then(function (r) {
        if (!r.ok) throw new Error("kpi");
        return r.json();
      })
      .then(applyKpiPayload)
      .catch(function () {});
  }

  var reloadTimer;
  var fetchTimer;

  function clearTimers() {
    clearInterval(reloadTimer);
    clearInterval(fetchTimer);
  }

  function schedule() {
    clearTimers();

    var reloadOn = false;
    var fetchOn = false;
    try {
      reloadOn = localStorage.getItem(KEY_RELOAD) === "1";
      fetchOn = localStorage.getItem(KEY_FETCH) === "1";
    } catch (_) {}

    var reloadToggle = document.getElementById("mfMcRefreshToggle");
    var fetchToggle = document.getElementById("mfMcFetchToggle");
    if (reloadToggle) reloadToggle.checked = reloadOn;
    if (fetchToggle) fetchToggle.checked = fetchOn;

    if (fetchOn) {
      fetchKpis();
      fetchTimer = setInterval(function () {
        if (document.visibilityState !== "visible") return;
        fetchKpis();
      }, INTERVAL_MS);
      return;
    }

    if (reloadOn) {
      reloadTimer = setInterval(function () {
        if (document.visibilityState !== "visible") return;
        window.location.reload();
      }, INTERVAL_MS);
    }
  }

  function init() {
    var reloadToggle = document.getElementById("mfMcRefreshToggle");
    var fetchToggle = document.getElementById("mfMcFetchToggle");
    if (!reloadToggle && !fetchToggle) return;

    function hook(el, key) {
      if (!el) return;
      try {
        el.checked = localStorage.getItem(key) === "1";
      } catch (_) {}
      el.addEventListener("change", function () {
        try {
          localStorage.setItem(key, el.checked ? "1" : "0");
          if (key === KEY_FETCH && el.checked && reloadToggle) {
            reloadToggle.checked = false;
            localStorage.setItem(KEY_RELOAD, "0");
          }
          if (key === KEY_RELOAD && el.checked && fetchToggle) {
            fetchToggle.checked = false;
            localStorage.setItem(KEY_FETCH, "0");
          }
        } catch (_) {}
        schedule();
      });
    }

    hook(reloadToggle, KEY_RELOAD);
    hook(fetchToggle, KEY_FETCH);
    schedule();
  }

  if (document.readyState === "loading")
    document.addEventListener("DOMContentLoaded", init);
  else init();
})();
