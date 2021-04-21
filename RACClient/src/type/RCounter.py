from .helper import msg_construct
from .helper import req_construct


class RCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("rc", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("rc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def inc(self, id, value, rid = ""):
        req = req_construct("rc", id, "i", [str(value), rid]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res


    def dec(self, id, value, rid = ""):
        req = req_construct("rc", id, "d", [str(value), rid]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def rev(self, id, value):
        req = req_construct("rc", id, "r", [str(value)]) 
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res
