class ScenarioConfig:
    def __init__(self, type_, connections, concurrent, base_step, step, step_length):
        self.connections = connections
        self.concurrent = concurrent
        self.base_step = base_step
        self.step = step
        self.step_length = step_length
        self.type = type_


class StatisticsConfig:
    def __init__(self, statistics_output_path, statistic_interval):
        self.statistic_interval = statistic_interval
        self.statistics_output_path = statistics_output_path


class ConnectionConfig:
    def __init__(self, url, protocol, transport):
        self.url = url
        self.protocol = protocol
        self.transport = transport


class ConstantConfig:
    def __init__(self, module, wait_time, config_save_path):
        self.wait_time = wait_time
        self.module = module
        self.config_save_path = config_save_path


class SendingConfig:
    def __init__(self, duration, interval, slave_count, message_size):
        self.duration = duration
        self.interval = interval
        self.slave_count = slave_count
        self.message_size = message_size


class ParameterKey:
    def __init__(self):
        self.normal_connection = "normal_connection"
        self.max_connection = "max_connection"
        self.base_step = "base_step"
        self.step = "step"
        self.concurrent = "concurrent"
        self.step_length = "step_length"
        self.unit_map = "unit_map"


def determine_scenario_config(settings, unit, scenario, transport, use_max_connection=True):
    if scenario == "sendToClient":
        pass
    else:
        key = "{}:{},{}:{}".format("scenario", scenario, "transport", transport)
        para_key = ParameterKey()
        cur_settings = settings[key]

        index = 0
        found = False
        for k, v in enumerate(settings[para_key.unit_map]):
            if v == unit:
                index = k
                found = True
                break

        if found is False:
            print("Cannot find unit {}, use the first one by default".format(unit))

        connections = cur_settings[para_key.normal_connection] if use_max_connection is False else \
            cur_settings[para_key.max_connection][index]
        concurrent = cur_settings[para_key.concurrent][index]
        base_step = cur_settings[para_key.base_step][index]
        step = cur_settings[para_key.step][index]
        step_length = cur_settings[para_key.step_length][index]

        config = ScenarioConfig(scenario, connections, concurrent, base_step, step, step_length)

        return config

