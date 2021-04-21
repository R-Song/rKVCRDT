rac_starter = "-RAC-\n"
rac_ender = "\n-EOF-"
client_addr = "Client"
typePrefix = "TYPE:"
uidPrefix = "UID:"
opPrefix = "OP:"
paramPrefix = "P:"

def msg_construct(server, msg):
    s = rac_starter + \
        "FROM:" + client_addr + "\n" + \
        "TO:" + str(server.ip) + ":" + str(server.port) + "\n" + \
        "CLS:" + "c\n" + \
        "LEN:" + str(len(msg)) + "\n" + \
        "CNT:\n" + msg + str(rac_ender)

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
        lines = res.split("CNT:")[1].strip().strip('-EOF-').splitlines()
    except IndexError:
        print("Parsing failure")
        print(res)
        return (False, "")

    if "Succeed" in lines[0]:
        success = True 
    else:
        success = False

    del lines[0]

    return (success, lines)