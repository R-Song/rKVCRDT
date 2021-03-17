from .helper import msg_construct
from .helper import req_construct

class GCounter:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("gc", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id, value):
        req = req_construct("gc", id, "s", [str(value)])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def inc(self, id, value):
        req = req_construct("gc", id, "i", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res