"""
Tiny static file server for local WebGL / iframe tests (Milestone 1).

    python -I Tools/WebTest/serve.py <directory> <port>

- Serves .wasm as application/wasm.
- Serves pre-compressed Unity files (*.br / *.gz) with the matching Content-Encoding header,
  mirroring a correctly configured production host. (Builds made with "decompression
  fallback" use *.unityweb and need no special headers.)
- Disables caching so every reload picks up a fresh build.
- Sends Access-Control-Allow-Origin: * (required for sandboxed iframes without allow-same-origin).
Local testing only - not a production server.
"""
import functools
import http.server
import os
import sys


class Handler(http.server.SimpleHTTPRequestHandler):
    extensions_map = {
        **http.server.SimpleHTTPRequestHandler.extensions_map,
        ".wasm": "application/wasm",
        ".js": "application/javascript",
        ".data": "application/octet-stream",
        ".unityweb": "application/octet-stream",
    }

    def end_headers(self):
        path = self.path.split("?", 1)[0]
        if path.endswith(".br"):
            self.send_header("Content-Encoding", "br")
        elif path.endswith(".gz"):
            self.send_header("Content-Encoding", "gzip")
        self.send_header("Cache-Control", "no-store")
        # Needed when the embedding iframe is sandboxed WITHOUT allow-same-origin: the game's
        # origin becomes opaque ("null") and every fetch of Build/* is cross-origin.
        self.send_header("Access-Control-Allow-Origin", "*")
        super().end_headers()

    def guess_type(self, path):
        base = path
        for suffix in (".br", ".gz"):
            if base.endswith(suffix):
                base = base[: -len(suffix)]
        return super().guess_type(base)


def main():
    if len(sys.argv) != 3:
        print(__doc__)
        return 1
    directory = os.path.abspath(sys.argv[1])
    port = int(sys.argv[2])
    handler = functools.partial(Handler, directory=directory)
    with http.server.ThreadingHTTPServer(("127.0.0.1", port), handler) as httpd:
        print(f"Serving {directory} at http://localhost:{port}/")
        httpd.serve_forever()
    return 0


if __name__ == "__main__":
    sys.exit(main())
