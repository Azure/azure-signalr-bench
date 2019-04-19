
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
        self.transport_long_polling = 'LongPolling'
        self.transport_server_sent_event = 'ServerSentEvents'

        # group config mode
        self.group_config_mode_group = "Group"
        self.group_config_mode_connection = "Connection"

        # connection type
        self.connection_type_core = "Core"
        self.connection_type_aspnet = "AspNet"
        self.connection_type_rest_direct = "CoreDirect"
        self.connection_type_map = {
            "Core": "CreateConnection",
            "AspNet": "CreateAspNetConnection",
            "CoreDirect": "CreateDirectConnection"
        }

    def ConnectionTypeName(self, connection_type):
        return self.connection_type_map[connection_type]

PERF_KIND = 0
LONGRUN_KIND = 1

class KindType:
    def __init__(self):
        self.perf = "perf"
        self.longrun = "longrun"

class ScenarioType:
    def __init__(self):
        self.echo = "echo"
        self.broadcast = "broadcast"
        self.rest_persist_broadcast = "restPersistBroadcast"
        self.rest_persist_send_to_user = "restPersistSendToUser"
        self.rest_persist_send_to_group = "restPersistSendToGroup"
        self.rest_broadcast = "restBroadcast"
        self.rest_send_to_group = "restSendToGroup"
        self.rest_send_to_user = "restSendToUser"
        self.send_to_client = "sendToClient"
        self.send_to_group = "sendToGroup"
        self.frequent_join_leave_group = "frequentJoinLeaveGroup"
