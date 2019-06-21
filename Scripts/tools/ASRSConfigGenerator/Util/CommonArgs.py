class StaticArgs:
    def __init__(self, module="default", statistic_interval=1000, statistics_output_path="counters.txt",
                 config_save_path="config.yaml",
                 wait_time=5000, slave_count=1, send_step=1, send_length=1,
                 connections=10, concurrent=10, message_size=1):
        self.connections = connections
        self.concurrent = concurrent
        self.slave_count = slave_count
        self.send_step = send_step
        self.send_length = send_length
        self.message_size = message_size
        self.module = module
        self.statistic_interval = statistic_interval
        self.wait_time = wait_time
        self.statistics_output_path = statistics_output_path
        self.config_save_path = config_save_path


class DynamicArgs:
    def __init__(self, type_, url, transport, protocol, message_size_list, duration, interval):
        self.type = type_
        self.duration = duration
        self.interval = interval
        self.url = url
        self.transport = transport
        self.protocol = protocol
        self.message_size_list = message_size_list
