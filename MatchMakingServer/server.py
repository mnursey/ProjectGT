import socket
import threading
import socketserver

server = None
socket = None

request_callback = None

class ThreadedUDPRequestHandler(socketserver.BaseRequestHandler):

    def handle(self):
        data = self.request[0]

        if len(data) > 0:
            data = data.decode().strip()
            socket = self.request[1]

            endpoint = self.client_address

            #print("{} wrote".format(endpoint))
            #print(data)

            request_callback(data, endpoint)

        else:
            print("Warning: Received 0 bytes...")

        return

def SendMessageToEndpoint(data, endpoint):

    if endpoint is None:
        print("Warning: Cannot send message to None endpoint")
        return

    if socket is not None:
        data = data.encode()

        if len(data) > 0:
            bytes_sent = socket.sendto(data, endpoint)
            #print("Sent {} bytes".format(bytes_sent))
        else:
            print("Warning: Tried sending msg with zero data")
    else:
        print("Warning: Could not send message... socket not defined yet")

    return

def RunServer(HOST, PORT, request_cb):

    global server
    global socket
    global request_callback

    """
    with socketserver.ThreadingUDPServer((HOST, PORT), ThreadedUDPRequestHandler) as server:
        ip, port = server.server_address
        socket = server.socket

        print("Starting server at {}:{}".format(ip,port))

        server_thread = threading.Thread(target=server.serve_forever)
        server_thread.daemon = True
        server_thread.start()
    """

    request_callback = request_cb

    server = socketserver.ThreadingUDPServer((HOST, PORT), ThreadedUDPRequestHandler)

    ip, port = server.server_address
    socket = server.socket

    print("Starting server at {}:{}".format(ip,port))

    server_thread = threading.Thread(target=server.serve_forever)
    server_thread.daemon = True
    server_thread.start()

    return

def ShutdownServer():

    global server

    server.shutdown()

    print("Shutdown server")

    return

if __name__ == "__main__":
    RunServer("localhost", 10069)
