class ArgType:
    def __init__(self):
        self.tiny_group = "tiny"
        self.small_group = "small"
        self.big_group = "big"


class ScenarioType:
    def __init__(self):
        self.echo = "echo"
        self.broadcast = "broadcast"
        self.send_to_client = "sendToClient"
        self.send_to_group = "sendToGroup"
