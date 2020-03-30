import server
import json
import time
import threading

print("Project GT\nMatch Making Server")

active_users = []
active_servers = []

parties = []
invites = []

requests = []

user_id_tracker = 0
server_id_tracker = 0
party_id_tracker = 0

enable_debug = True
testing = False

def debug(msg):

    if enable_debug:
        print(msg)

    return

def new_user_id():

    global user_id_tracker

    id = user_id_tracker
    user_id_tracker += 1

    return user_id_tracker

def new_server_id():

    global server_id_tracker

    id = server_id_tracker
    server_id_tracker += 1

    return server_id_tracker

def new_party_id():

    global party_id_tracker

    id = party_id_tracker
    party_id_tracker += 1

    return party_id_tracker

def process_requests():

    global requests

    if len(requests) > 0:
        request = request.pop(0)
        request.procress()

        return True

    return False

def handle_new_request(data, endpoint):

    global requests

    data = json.loads(data)

    function_map = {
        "register_user" : register_user,
        "unregister_user" : unregister_user,
        "play_mode" : play_mode,
        "create_party" : create_party,
        "dissolve_party" : dissolve_party,
        "join_party" : join_party,
        "leave_party" : leave_party,
        "invite_to_party" : invite_to_party,
        "uninvite_to_party" : uninvite_to_party,
        "select_car" : select_car,
        "get_user_state" : get_user_state,
        "register_server" : register_server,
        "unregister_server" : unregister_user,
        "get_game_server_info" : get_game_server_info,
        "update_server_status" : update_server_status
    }

    input_map = {
        "register_user" : [endpoint, data['car_id']],
        "unregister_user" : [data['user_id']],
        "play_mode" : [data['party_id'], data['mode']],
        "create_party" : [data['user_id']],
        "dissolve_party" : [data['party_id']],
        "join_party" : [data['party_id'], data['user_id']],
        "leave_party" : [data['party_id'], data['user_id']],
        "invite_to_party" : [data['party_id'], data['user_id']],
        "uninvite_to_party" : [data['party_id'], data['user_id']],
        "select_car" : [data['user_id'], data['car_id']],
        "get_user_state" : [data['user_id']],
        "register_server" : [endpoint],
        "unregister_server" : [data['server_id'], data['mode'], data['state'], data['population'], data['max_population']],
        "get_game_server_info" : [data['server_id']],
        "update_server_status" : [data['server_id'], data['mode'], data['state'], data['population'], data['max_population']]
    }

    request_type = data.request_type
    request_function = function_map.get(request_type, None)
    request_input = input_map.get(request_type, None)

    requests.append(Request(request_input, request_function))

    return

def register_user(user_endpoint, car_id):

    global active_users

    user = User(new_user_id(), user_endpoint, car_id)

    active_users.append(user)

    if user.active_party is None:
        user.active_party = create_party(user)

    debug("Registered new user... ID : {}".format(user.id))

    server.SendMessageToEndpoint(json.dumps(get_user_state(user.id)), user.endpoint)

    return

def unregister_user(user_id):

    global active_users

    user = next((u for u in active_users if u.id == user_id), None)

    if user is not None:

        leave_party(user.active_party.id, user_id)

        for invite in iter(i for i in invites if i.user == user):
            uninvite_to_party(invite.party.id, invite.user.id)

        active_users.remove(user)

        debug("Unregistered user... ID : {}".format(user.id))

        server.SendMessageToEndpoint(json.dumps({"msg" : "unregistered"}), user.endpoint)

    return

def register_server(server_endpoint, mode, state, population, max_population):

    global active_servers

    game_server = GameServer(new_server_id(), server_endpoint, mode, state, population, max_population)

    active_servers.append(game_server)

    debug("Registered new server... ID : {}".format(game_server.id))

    server.SendMessageToEndpoint(json.dumps(get_game_server_info(game_server.id)), game_server.endpoint)

    return

def unregister_server(server_id):

    global active_servers

    game_server = next((s for s in active_servers if s.id == server_id), None)

    if game_server != None:
        active_servers.remove(game_server)

        debug("Unregistered server... ID : {}".format(game_server.id))

        server.SendMessageToEndpoint(json.dumps({ "msg" : "unregistered" }), game_server.endpoint)

    return

def update_server_status(server_id, mode, state, population, max_population):

    global active_servers

    game_server = next((s for s in active_servers if s.id == server_id), None)

    if game_server != None:
        game_server.mode = mode
        game_server.state = state
        game_server.population = population
        game_server.max_population = max_population

        debug("Updated server state... ID : {}, Mode : {}, State : {}, Population : {}, Max Population : {}".format(game_server.id, game_server.mode, game_server.state, game_server.population, game_server.max_population))

        server.SendMessageToEndpoint(json.dumps(get_game_server_info(game_server.id)), game_server.endpoint)

    return

def ping():

    pass

def play_mode(party_id, mode):

    global active_users
    global active_servers
    global parties

    # TODO:
    # find server for party to join
    # ping and skill level dependant

    output = ""

    party = next((p for p in parties if p.id == party_id), None)

    if party is not None:
        potential_servers = [s for s in active_servers if s.mode == mode and s.state == "Accepting" and s.max_population >= s.population + len(party.users)]

        if len(potential_servers) > 0:

            # TODO:
            # Check ping
            # select server
            # send all clients msg to connect to server
            # update client state?

            output = "server_found"

        else:
            output = "no_servers_found"

    return output


def create_party(party_leader):

    global parties

    party = Party(new_party_id(), party_leader)

    parties.append(party)

    debug("Created new party... ID : {}".format(party.id))

    return party

def dissolve_party(party_id):

    global parties
    global invites

    party = next((p for p in parties if p.id == party_id), None)

    if party is not None:
        parties.remove(party)

        for user in party.users:
            user.active_party = create_party(user)

        for invite in iter(i for i in invites if i.party == party):
            uninvite_to_party(invite.party.id, invite.user.id)

        debug("Dissolved party... ID : {}".format(party.id))

        for party_user in party.users:
            server.SendMessageToEndpoint(json.dumps(get_user_state(party_user.id)), party_user.endpoint)

    return

def join_party(party_id, user_id):

    global active_users
    global parties
    global invites

    user = next((u for u in active_users if u.id == user_id), None)
    party = next((p for p in parties if p.id == party_id), None)

    if user is not None and party is not None:

        uninvite_to_party(party_id, user_id)

        if user.active_party != None:
            leave_party(user.active_party.id, user.id)

        user.active_party = party
        party.users.append(user)

        debug("User joined party... User ID : {}, Party ID : {}".format(user.id, party.id))

        for party_user in party.users:
            server.SendMessageToEndpoint(json.dumps(get_user_state(party_user.id)), party_user.endpoint)

    return

def leave_party(party_id, user_id):

    global active_users
    global parties

    user = next((u for u in active_users if u.id == user_id), None)
    party = next((p for p in parties if p.id == party_id), None)

    if user is not None and party is not None:

        party.users.remove(user)

        if party.lead_user is user:
            party.lead_user = None

            if len(party.users) > 0:
                party.lead_user = party.users[0]
            else:
                dissolve_party(party.id)

        debug("User left party... User ID : {}, Party ID : {}".format(user.id, party.id))

        user.active_party = create_party(user)

        for party_user in party.users:
            server.SendMessageToEndpoint(json.dumps(get_user_state(party_user.id)), party_user.endpoint)

        server.SendMessageToEndpoint(json.dumps(get_user_state(user.id)), user.endpoint)

    else:
        print("Warning: could not remove user {} from party {}".format(user_id, party_id))

    return

def invite_to_party(party_id, user_id):

    global active_users
    global parties
    global invites

    user = next((u for u in active_users if u.id == user_id), None)
    party = next((p for p in parties if p.id == party_id), None)

    if user is not None and party is not None:

        invite = PartyInvite(party, user)

        invites.append(invite)

        debug("User invited to party... User ID : {}, Party ID : {}".format(user.id, party.id))

        server.SendMessageToEndpoint(json.dumps(get_user_state(user.id)), user.endpoint)

    return

def uninvite_to_party(party_id, user_id):

    global active_users
    global parties
    global invites

    user = next((u for u in active_users if u.id == user_id), None)
    party = next((p for p in parties if p.id == party_id), None)

    if user is not None and party is not None:

        invite = next((i for i in invites if i.party == party and i.user == user), None)

        while invite is not None:
            invites.remove(invite)
            invite = next((i for i in invites if i.party == party and i.user == user), None)

        debug("User uninvited to party... User ID : {}, Party ID : {}".format(user.id, party.id))

        server.SendMessageToEndpoint(json.dumps(get_user_state(user.id)), user.endpoint)

    return

def select_car(user_id, car_id):

    global active_users

    user = next((u for u in active_users if u.id == user_id), None)

    if user is not None:

        user.car_id = car_id

        debug("User selected car... User ID : {}, Car ID : {}".format(user.id, car_id))

        server.SendMessageToEndpoint(json.dumps(get_user_state(user.id)), user.endpoint)

    return

def get_user_state(user_id):

    global active_users

    state = { "id" : user_id }

    user = next((u for u in active_users if u.id == user_id), None)

    if user is not None:
        state["car_id"] = user.car_id
        state["invites"] = [i.party.id for i in invites if i.user == user]
        state["active_party"] = { "id" : user.active_party.id, "leader" : user.active_party.lead_user.id, "users" : [u.id for u in user.active_party.users] } if user.active_party is not None else None
    else:
        state["error"] = "Unregistered"

    return state

def get_game_server_info(server_id):

    global active_servers

    info = {"id" : server_id }

    server = next((s for s in active_servers if s.id == server_id), None)

    if server is not None:
        info["mode"] = server.mode
        info["state"] = server.state
        info["population"] = server.population
        info["max_population"] = server.max_population
    else:
        info["error"] = "Unregistered"

    return info

class Party:

    def __init__(self, id, party_leader):
        self.id = id
        self.lead_user = party_leader
        self.users = [party_leader]


class PartyInvite:

    def __init__(self, party, user):
        self.party = party
        self.user = user


class User:

    def __init__(self, id, endpoint, car_id):
        self.id = id
        self.endpoint = endpoint
        self.car_id = car_id
        self.active_party = None


class GameServer:

    def __init__(self, id, endpoint, mode, state, population, max_population):

        self.id = id
        self.endpoint = endpoint
        self.mode = mode
        self.state = state
        self.population = population
        self.max_population = max_population

class Request:

    def __init__(self, inputs, function):
        self.inputs = inputs
        self.function = function

    def process():
        if function is not None:
            if inputs is not None:
                function(*inputs)
            else:
                print("Warning: Request with input as None...")
                function()
        else:
            print("Warning: Request with function as None...")

def process_loop():

    print("Beginning to process requests...")

    while True:

        had_request = process_requests()

        if not had_request:
            time.sleep(0.5)

    return

def run():

    process_thread = threading.Thread(target=process_loop)
    process_thread.daemon = True
    process_thread.start()

    server.RunServer("localhost", 10069)

    if testing:

        register_user(None, 0)
        dissolve_party(1)
        register_user(None, 1)
        invite_to_party(2, 2)

        print(get_user_state(1))
        print(get_user_state(2))

        join_party(2, 2)

        print(get_user_state(2))

        unregister_user(1)

        print(len(active_users))
        print(get_user_state(2))

        select_car(2, 12)
        print(get_user_state(2))

        register_server(None, "m", "s", 0, 32)
        print(get_game_server_info(1))

        update_server_status(1, "M", "S", 4, 32)
        print(get_game_server_info(1))

        unregister_server(1)

    input()

if __name__ == "__main__":
    run()
