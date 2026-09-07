"""Byte-transparent TCP relay between Unity and TonyPi."""

import select
import socket
import threading


class ControlProxyServer:
    def __init__(self, config):
        self.listen_host = str(config.get("listen_host", "0.0.0.0"))
        self.listen_port = int(config.get("listen_port", 5075))
        self.allowed_client_ips = set(config.get("allowed_client_ips", []))
        self.upstream_hosts = [
            str(host)
            for host in config.get("upstream_hosts", [config["upstream_host"]])
        ]
        if not self.upstream_hosts:
            raise ValueError("at least one upstream host is required")
        self.upstream_port = int(config.get("upstream_port", 5075))
        self.connect_timeout = float(config.get("connect_timeout_seconds", 3.0))
        self.socket_timeout = float(config.get("socket_timeout_seconds", 0.5))
        self.buffer_size = int(config.get("buffer_size", 4096))
        for port in (self.listen_port, self.upstream_port):
            if not 0 <= port <= 65535:
                raise ValueError("TCP port is invalid")
        if self.buffer_size < 256:
            raise ValueError("buffer_size must be at least 256")
        self._stop_event = threading.Event()
        self._listener = None
        self._active_sockets = []
        self.bound_port = None

    def serve_forever(self, ready_event=None):
        listener = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
        listener.bind((self.listen_host, self.listen_port))
        listener.listen(1)
        listener.settimeout(self.socket_timeout)
        self._listener = listener
        self.bound_port = listener.getsockname()[1]
        print(
            "[CONTROL] listen {}:{} -> {}:{}".format(
                self.listen_host,
                self.bound_port,
                ",".join(self.upstream_hosts),
                self.upstream_port,
            ),
            flush=True,
        )
        if ready_event is not None:
            ready_event.set()

        try:
            while not self._stop_event.is_set():
                try:
                    client, address = listener.accept()
                except socket.timeout:
                    continue
                except OSError:
                    if self._stop_event.is_set():
                        break
                    raise

                client_ip = address[0]
                if self.allowed_client_ips and client_ip not in self.allowed_client_ips:
                    print("[CONTROL] reject client {}".format(client_ip), flush=True)
                    client.close()
                    continue

                try:
                    upstream, upstream_host = self._connect_upstream()
                except OSError as exc:
                    print(
                        "[CONTROL] upstream unavailable: {}: {}".format(
                            type(exc).__name__, exc
                        ),
                        flush=True,
                    )
                    client.close()
                    continue

                self._configure_socket(client)
                self._configure_socket(upstream)
                self._active_sockets = [client, upstream]
                print(
                    "[CONTROL] Unity connected from {} via {}".format(
                        client_ip, upstream_host
                    ),
                    flush=True,
                )
                try:
                    relay_bidirectional(
                        client,
                        upstream,
                        stop_event=self._stop_event,
                        buffer_size=self.buffer_size,
                        poll_seconds=self.socket_timeout,
                    )
                finally:
                    self._close_socket(client)
                    self._close_socket(upstream)
                    self._active_sockets = []
                    print("[CONTROL] Unity disconnected", flush=True)
        finally:
            self._close_socket(listener)
            self._listener = None
            self._active_sockets = []

    @staticmethod
    def _configure_socket(sock):
        sock.setsockopt(socket.IPPROTO_TCP, socket.TCP_NODELAY, 1)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_KEEPALIVE, 1)

    def _connect_upstream(self):
        last_error = None
        for host in self.upstream_hosts:
            try:
                return (
                    socket.create_connection(
                        (host, self.upstream_port), timeout=self.connect_timeout
                    ),
                    host,
                )
            except OSError as exc:
                last_error = exc
        raise last_error

    @staticmethod
    def _close_socket(sock):
        if sock is None:
            return
        try:
            sock.shutdown(socket.SHUT_RDWR)
        except OSError:
            pass
        try:
            sock.close()
        except OSError:
            pass

    def stop(self):
        self._stop_event.set()
        self._close_socket(self._listener)
        for sock in list(self._active_sockets):
            self._close_socket(sock)


def relay_bidirectional(left, right, stop_event=None, buffer_size=4096, poll_seconds=0.5):
    """Forward bytes in both directions without inspecting or changing them."""
    stop_event = stop_event or threading.Event()
    peers = {left: right, right: left}
    while not stop_event.is_set():
        try:
            readable, _, exceptional = select.select(
                [left, right], [], [left, right], poll_seconds
            )
        except (OSError, ValueError):
            break
        if exceptional:
            break
        for source in readable:
            try:
                data = source.recv(buffer_size)
            except OSError:
                return
            if not data:
                return
            try:
                peers[source].sendall(data)
            except OSError:
                return
