from .helper import msg_construct
from .helper import req_construct

class RGraph:
    def __init__(self, s):
        self.server = s

    def get(self, id):
        req = req_construct("rg", id, "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def set(self, id):
        req = req_construct("rg", id, "s", [])
        req = msg_construct(self.server, req)
        
        res = self.server.send(req)
        return res

    def addvertex(self, id, value):
        req = req_construct("rg", id, "av", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def remvoevertex(self, id, value):
        req = req_construct("rg", id, "rv", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def addedge(self, id, value1, value2):
        req = req_construct("rg", id, "ae", [str(value1), str(value2)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def removeedge(self, id, value1, value2):
        req = req_construct("rg", id, "re", [str(value1), str(value2)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res

    def reverse(self, id, value):
        req = req_construct("rg", id, "r", [str(value)])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res