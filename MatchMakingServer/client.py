import socket
import threading
import socketserver
import server
import json
import os

user_state = None

match_making_server_endpoint = ('localhost', 10069)

def send_single_message(endpoint, message):
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)

    try:
        sock.sendto(message, endpoint)
        #response = sock.recv(1024)
        #print("Received: {}".format(response))
    finally:
        sock.close()

    print("Sent message to {}".format(endpoint))

def send_msg(data):

    #send_single_message(match_making_server_endpoint, json.dumps(data).encode())

    server.SendMessageToEndpoint(json.dumps(data) , match_making_server_endpoint)

    return

def register_user_request(car_id):

    data = { "request_type" : "register_user" , "car_id" : car_id }

    send_msg(data)

    return

def unregister_user_request():

    data = { "request_type" : "unregister_user" , "user_id" : user_state["id"] }

    send_msg(data)

    return

def join_party_request(party_id):

    data = { "request_type" : "join_party" , "user_id" : user_state["id"], "party_id" : party_id }

    send_msg(data)

def leave_party_request():

    data = { "request_type" : "leave_party" , "user_id" : user_state["id"], "party_id" : user_state["active_party"]["id"] }

    send_msg(data)

    return

def dissolve_party_request():

    data = { "request_type" : "dissolve_party" , "party_id" : user_state["active_party"]["id"] }

    send_msg(data)

    return

def select_car_request(car_id):

    data = { "request_type" : "select_car" , "user_id" : user_state["id"], "car_id" : car_id }

    send_msg(data)

    return

def handle_responce(data, endpoint):

    global user_state

    user_state = json.loads(data)

    #print("{} wrote".format(endpoint))
    #print(data)

    print("State has been updated:")
    print(user_state)

    return

def run():

    server.RunServer("localhost", 0, handle_responce)

    while True:

        print("reg - register user\nunreg - unregister user\njn_p - join party\nlv_p - leave party\ndis_p - dissolve party\nsel_c - select car\nquit - duh...")
        print("----------------")
        print(user_state)
        print("----------------")

        cmd = input("cmd: ")

        if cmd == "quit":
            break
        elif cmd == "reg":
            car_id = input("car id: ")
            register_user_request(car_id)
        elif cmd == "unreg":
            unregister_user_request()
        elif cmd == "jn_p":
            party_id = input("party id: ")
            join_party_request(party_id)
        elif cmd == "lv_p":
            leave_party_request()
        elif cmd == "dis_p":
            dissolve_party_request()
        elif cmd == "sel_c":
            car_id = input("car id: ")
            select_car_request(car_id)
        else:
            print("invalid cmd")

        input()
        os.system('clear')

    server.ShutdownServer()

    return

if __name__ == "__main__":
    run()
