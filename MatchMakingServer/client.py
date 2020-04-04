import socket
import threading
import socketserver
import server
import json
import os

user_state = None

match_making_server_endpoint = ('localhost', 10069)


def send_msg(data):

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

def play_mode_request():

    data = { "request_type" : "play_mode" , "party_id" : user_state["active_party"]["id"], "mode" : "practice" }

    send_msg(data)

    return

def cancel_play_search_request():

    data = { "request_type" : "cancel_play_search" , "party_id" : user_state["active_party"]["id"] }

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

def send_user_msg_request(msg):

    data = { "request_type" : "send_user_msg" , "user_id" : user_state["id"], "party_id" : user_state["active_party"]["id"], "msg" : msg}

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

        print("play - play practice mode\ncpr - cancel play request\nreg - register user\nunreg - unregister user\njn_p - join party\nlv_p - leave party\ndis_p - dissolve party\nsel_c - select car\nmsg - send chat message to party\nquit - duh...")
        print("----------------")
        print(user_state)
        print("----------------")

        cmd = input("cmd: ")

        if cmd == "quit":
            break
        elif cmd == "play":
            play_mode_request()
        elif cmd == "cpr":
            cancel_play_search_request()
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
        elif cmd == "msg":
            msg = input("msg: ")
            send_user_msg_request(msg)
        else:
            print("invalid cmd")

        input()
        os.system('clear')

    server.ShutdownServer()

    return

if __name__ == "__main__":
    run()
