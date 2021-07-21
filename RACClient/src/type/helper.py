rac_starter = "-RAC-\n"
rac_ender = "\n-EOF-"
client_addr = "Client"
typePrefix = "TYPE:"
uidPrefix = "UID:"
opPrefix = "OP:"
paramPrefix = "P:"

def msg_construct(server, msg):
    s = '\f'  + client_addr + '\t' + \
        str(server.ip) + ':' + str(server.port) + '\t' + \
        'c\t' + \
        str(len(msg)) + '\t' + \
        msg + '\f'

    return s


def req_construct(tid, uid, op, params):
    req = typePrefix + tid + "\n" + \
          uidPrefix + uid + "\n" + \
          opPrefix + op + "\n"

    for p in params:
        req += paramPrefix + p + "\n" 

    return req


def res_parse(res):
    
    # TODO: add try for timeout
    try:
        lines = res.split('\t')[4].strip().splitlines()
    except IndexError:
        print("Parsing failure:")
        print(res)
        print("======================")
        return (False, "")

    if "Succeed" in lines[0]:
        success = True 
    else:
        success = False

    del lines[0]

    return (success, lines)