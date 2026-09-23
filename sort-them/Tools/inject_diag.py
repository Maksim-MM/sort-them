#!/usr/bin/env python3
"""Вставляет в index.html WebGL-сборки перехват ошибок и прогресса загрузки.

Использование: python3 Tools/inject_diag.py [папка сборки]   (по умолчанию Builds/WebGL_Diag)
Страница шлёт на /log того же сервера: window.onerror, необработанные промисы, console.error/warn,
баннеры Unity (unityShowBanner), прогресс загрузки, событие pagehide и раз в 5 с пульс с памятью
(performance.memory есть только в Chrome). Сервер: Tools/serve_build_diag.py.
"""
import os, re, sys

DIR = sys.argv[1] if len(sys.argv) > 1 else "Builds/WebGL_Diag"
path = os.path.join(DIR, "index.html")
html = open(path, encoding="utf-8").read()
if "__diag" in html:
    print("уже пропатчен:", path)
    sys.exit(0)

hook = r"""
    <script>
      (function () {
        var q = [], t = 0, start = performance.now();
        function mem() {
          var m = performance.memory;
          var o = { t: Math.round(performance.now() - start) / 1000 };
          if (m) o.jsHeapMB = Math.round(m.usedJSHeapSize / 1048576);
          if (navigator.deviceMemory) o.deviceGB = navigator.deviceMemory;
          return o;
        }
        function flush() {
          if (!q.length) return;
          var body = JSON.stringify(q); q = [];
          try { navigator.sendBeacon("/log", body); }
          catch (e) { try { fetch("/log", { method: "POST", body: body, keepalive: true }); } catch (e2) {} }
        }
        var panel = null, lines = [];
        function show(kind, msg) {
          if (/^(pulse|visibility|progress)$/.test(kind)) return;
          if (!panel) {
            panel = document.createElement("pre");
            panel.style.cssText = "position:fixed;left:0;right:0;bottom:0;max-height:45vh;overflow:auto;margin:0;padding:6px 8px;background:rgba(0,0,0,.78);color:#ff6;font:11px/1.35 monospace;white-space:pre-wrap;word-break:break-all;z-index:99999;pointer-events:auto";
            panel.onclick = function () { panel.style.display = "none"; };
            (document.body || document.documentElement).appendChild(panel);
          }
          lines.push(Math.round((performance.now() - start) / 100) / 10 + "s " + kind + ": " + String(msg).slice(0, 300));
          if (lines.length > 14) lines.shift();
          panel.textContent = lines.join("\n");
          panel.style.display = "";
        }
        function log(kind, msg, extra) {
          var e = extra || {}; var m = mem(); for (var k in m) e[k] = m[k];
          q.push({ kind: kind, msg: String(msg).slice(0, 2000), extra: e });
          clearTimeout(t); t = setTimeout(flush, 200);
          try { show(kind, msg); } catch (ex) {}
        }
        window.__diag = log;
        window.addEventListener("error", function (ev) {
          log("error", ev.message || ev, { src: ev.filename, line: ev.lineno, stack: ev.error && ev.error.stack ? String(ev.error.stack).slice(0, 1500) : undefined });
        });
        window.addEventListener("unhandledrejection", function (ev) {
          var r = ev.reason; log("unhandledrejection", r && r.message ? r.message : r, { stack: r && r.stack ? String(r.stack).slice(0, 1500) : undefined });
        });
        ["error", "warn"].forEach(function (lvl) {
          var orig = console[lvl];
          console[lvl] = function () { log("console." + lvl, Array.prototype.join.call(arguments, " ")); orig.apply(console, arguments); };
        });
        window.addEventListener("pagehide", function () { log("pagehide", "страница скрыта или закрыта"); flush(); });
        document.addEventListener("visibilitychange", function () { log("visibility", document.visibilityState); });
        setInterval(function () { log("pulse", "alive"); }, 5000);
        var gl = document.createElement("canvas").getContext("webgl2") || document.createElement("canvas").getContext("webgl");
        var ext = gl ? gl.getSupportedExtensions().filter(function (x) { return /compressed_texture/.test(x); }) : ["no webgl"];
        log("start", navigator.userAgent, { dpr: window.devicePixelRatio, screen: screen.width + "x" + screen.height, webgl2: !!(gl && gl.constructor.name === "WebGL2RenderingContext"), texExt: ext, cores: navigator.hardwareConcurrency });
      })();
    </script>
"""

# перехват баннеров Unity и прогресса загрузки
html = html.replace(
    "function unityShowBanner(msg, type) {",
    "function unityShowBanner(msg, type) {\n        if (window.__diag) window.__diag('unity.' + (type || 'banner'), msg);", 1)
html = re.sub(
    r"createUnityInstance\(canvas, config, \(progress\) => \{",
    "var __lastP = -1;\n        createUnityInstance(canvas, config, (progress) => {\n"
    "          var p = Math.floor(progress * 10); if (p !== __lastP) { __lastP = p; if (window.__diag) window.__diag('progress', Math.round(progress * 100) + '%'); }",
    html, count=1)
html = html.replace(
    "}).then((unityInstance) => {",
    "}).then((unityInstance) => {\n          if (window.__diag) window.__diag('unity.ready', 'createUnityInstance resolved');", 1)
html = html.replace(
    "}).catch((message) => {",
    "}).catch((message) => {\n          if (window.__diag) window.__diag('unity.fail', message);", 1)

# скрипт-перехватчик в самое начало <head>, до всего остального
html = html.replace("<head>", "<head>" + hook, 1)
open(path, "w", encoding="utf-8").write(html)
print("пропатчен:", path)
