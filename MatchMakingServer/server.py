import socket
import threading
import socketserver

socket = None

class ThreadedUDPRequestHandler(socketserver.BaseRequestHandler):

    def handle(self):
        data = self.request[0].strip()
        socket = self.request[1]

        #print("{} wrote".format(self.client_address[0]))
        #print(data)

        handle_new_request(data, endoint)

        return

class ThreadedUDPServer(socketserver.ThreadingMixIn, socketserver.UDPServer):
    pass

def SendMessageToEndpoint(data, endpoint):

    if endpoint is None:
        print("Warning: Cannot send message to None endpoint")
        return

    if socket is not None:
        socket.sendto(data, endpoint)
    else:
        print("Warning: Could not send message... socket not defined yet")

    return

def RunServer(HOST, PORT):

    global socket

    with ThreadedUDPServer((HOST, PORT), ThreadedUDPRequestHandler) as server:
        ip, port = server.server_address
        socket = server.socket

        print("Starting server at {}:{}".format(ip,port))

        server_thread = threading.Thread(target=server.serve_forever)
        server_thread.daemon = True
        server_thread.start()

        # server.shutdown()
    return

if __name__ == "__main__":
    RunServer("localhost", 10069)
