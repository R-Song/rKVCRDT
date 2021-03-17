from .helper import msg_construct
from .helper import req_construct

class Performance:
    def __init__(self, s):
        self.server = s

    def get(self):
        req = req_construct("pref", "pf", "g", [])
        req = msg_construct(self.server, req)

        res = self.server.send(req)
        return res