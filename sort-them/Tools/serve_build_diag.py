#!/usr/bin/env python3
"""Раздаёт WebGL-сборку по Wi-Fi и принимает ошибки со страницы телефона.

Использование: python3 Tools/serve_build_diag.py [папка] [порт]
По умолчанию Builds/WebGL_Diag и порт 8000. Всё, что страница шлёт POST-ом на /log,
печатается в терминал и дописывается в Tools/diag_log.txt (перезаписывается при запуске).
Страница должна быть пропатчена Tools/inject_diag.py.
"""
import http.server, json, os, subprocess, sys, time

DIR = sys.argv[1] if len(sys.argv) > 1 else "Builds/WebGL_Diag"
PORT = int(sys.argv[2]) if len(sys.argv) > 2 else 8000
LOG = os.path.join(os.path.dirname(os.path.abspath(__file__)), "diag_log.txt")
open(LOG, "w").close()


def lan_ips():
    out = subprocess.run(["ifconfig"], capture_output=True, text=True).stdout
    ips, iface = [], None
    for line in out.splitlines():
        if line and not line[0].isspace():
            iface = line.split(":")[0]
        elif line.strip().startswith("inet ") and iface and iface.startswith("en"):
            ip = line.split()[1]
            if not ip.startswith("169.254."):
                ips.append(ip)
    return ips or ["127.0.0.1"]


class Handler(http.server.SimpleHTTPRequestHandler):
    def __init__(self, *a, **kw):
        super().__init__(*a, directory=DIR, **kw)

    def end_headers(self):
        self.send_header("Cache-Control", "no-store")
        if self.path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        if self.path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        super().end_headers()

    def do_POST(self):
        if self.path != "/log":
            self.send_error(404)
            return
        n = int(self.headers.get("Content-Length", 0))
        body = self.rfile.read(n).decode("utf-8", "replace")
        try:
            items = json.loads(body)
            if not isinstance(items, list):
                items = [items]
        except ValueError:
            items = [{"kind": "raw", "msg": body}]
        stamp = time.strftime("%H:%M:%S")
        ua = self.headers.get("User-Agent", "")[:60]
        with open(LOG, "a") as f:
            for it in items:
                line = f"{stamp} [{self.client_address[0]}] {it.get('kind','?')}: {it.get('msg','')}"
                if it.get("extra"):
                    line += "  | " + json.dumps(it["extra"], ensure_ascii=False)
                print(line, flush=True)
                f.write(line + f"  | ua={ua}\n")
        self.send_response(204)
        self.end_headers()

    def log_message(self, fmt, *args):
        if self.command == "GET" and not self.path.startswith("/Build/"):
            return
        if self.command == "GET":
            print(time.strftime("%H:%M:%S"), f"[{self.client_address[0]}] GET {self.path}", flush=True)


print("Открой на телефоне: " + "  или  ".join(f"http://{ip}:{PORT}/" for ip in lan_ips()) + f"   (папка {DIR}, лог {LOG})", flush=True)
http.server.ThreadingHTTPServer(("", PORT), Handler).serve_forever()
