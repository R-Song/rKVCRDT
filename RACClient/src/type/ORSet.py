from .helper import msg_construct
from .helper import req_construct

class ORSet:

    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("os", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id):
        req = req_construct("os", id, "s", [])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def add(self, id, value):
        req = req_construct("os", id, "a", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def remvoe(self, id, value):
        req = req_construct("os", id, "rm", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res
