import socket
import threading
import socketserver
import server
import json
import os
import random

user_state = None

match_making_server_endpoint = ('localhost', 10069)

# mms -> match making server
# nmsg -> network message

mms_nmsg_tracker = 0
nmsg_tracker = 0

unconfirmed_sent_nmsgs = []

queued_incoming_messages = []

def get_outgoing_nmsg_id():

    global nmsg_tracker

    nmsg_id = nmsg_tracker

    nmsg_tracker += 1

    return nmsg_id

def send_msg(data):

    global unconfirmed_sent_nmsgs
    global user_state

    data["nmsg_id"] = get_outgoing_nmsg_id()

    if user_state != None and "id" in user_state:
        data["client_id"] = user_state["id"]

    data["confirmed_nmsg_id"] = mms_nmsg_tracker

    unconfirmed_sent_nmsgs.append(data)

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

def missing_nmsgs_request(nmsg_ids):

    print("Requested lost msgs... {}".format(nmsg_ids))

    data = { "request_type" : "missing_nmsgs" , "nmsg_ids" : nmsg_ids }

    send_msg(data)

    return

def store_nmsg(new_state):

    global queued_incoming_messages 

    nmsg = next((n for n in queued_incoming_messages if n["nmsg_id"] == new_state["nmsg_id"]), None)

    if nmsg == None:
        queued_incoming_messages.append(new_state)

    return

def get_missing_nmsg_ids(new_state):

    global mms_nmsg_tracker

    missing_nmsg_ids = []

    for i in range(mms_nmsg_tracker + 1, new_state["nmsg_id"]):
        missing_nmsg_ids.append(i)

    return missing_nmsg_ids

def handle_responce(data, endpoint):

    global user_state
    global mms_nmsg_tracker
    global queued_incoming_messages

    new_state = json.loads(data)

    nmsg_id_delta = new_state["nmsg_id"] - mms_nmsg_tracker

    if nmsg_id_delta != 1:

        if nmsg_id_delta > 0:
            print("Warning nmsg was lost... {} {}".format(mms_nmsg_tracker, new_state["nmsg_id"]))

            store_nmsg(new_state)
            missing_nmsg_ids = get_missing_nmsg_ids(new_state)
            missing_nmsgs_request(missing_nmsg_ids)

        else:
            # ignore old msgs
            print("Warning old extra nmsg... {} {}".format(mms_nmsg_tracker, new_state["nmsg_id"]))

    else:
        process_request(new_state)

    return

def process_request(state):

    global user_state
    global mms_nmsg_tracker
    global queued_incoming_messages

    user_state = state
    mms_nmsg_tracker = state["nmsg_id"]

    print("State has been updated:")
    print(state)

    # check queued msg for next msg to process
    nmsg = next((n for n in queued_incoming_messages if n["nmsg_id"] == mms_nmsg_tracker + 1), None)

    if nmsg != None:
        queued_incoming_messages.remove(nmsg)
        process_request(nmsg)

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
        os.system('cls')

    server.ShutdownServer()

    return

if __name__ == "__main__":
    run()
