class ArgType:
    def __init__(self):
        # group type
        self.group_tiny = "tiny"
        self.group_small = "small"
        self.group_big = "big"

        # protocol
        self.protocol_json = 'json'
        self.protocol_messagepack = 'messagepack'

        # transport
        self.transport_websockets = 'Websockets'
        self.transport_long_polling = 'Longpolling'
        self.transport_server_sent_event = 'ServerSentEvent'


class ScenarioType:
    def __init__(self):
        self.echo = "echo"
        self.broadcast = "broadcast"
        self.send_to_client = "sendToClient"
        self.send_to_group = "sendToGroup"
        self.frequent_join_leave_group = "frequentJoinLeaveGroup"
