import yaml
from Util.Common import *


class ScenarioConfig:
    def __init__(self, type_, connections, concurrent, base_step, step, step_length,
                 batch_mode, batch_wait, group_count, group_type, group_config_mode,
                 streaming_item_count, streaming_item_send_interval):
        self.connections = connections
        self.concurrent = concurrent
        self.base_step = base_step
        self.step = step
        self.step_length = step_length
        self.type = type_
        self.group_count = group_count
        self.group_type = group_type
        self.group_config_mode = group_config_mode
        self.batch_mode = batch_mode
        self.batch_wait = batch_wait
        self.streaming_item_count = streaming_item_count
        self.streaming_item_send_interval = streaming_item_send_interval


class StatisticsConfig:
    def __init__(
            self,
            statistics_output_path,
            statistic_interval,
            statistic_latency_max,
            statistic_latency_step,
            connection_percentile_list):
        self.statistic_interval = statistic_interval
        self.statistics_output_path = statistics_output_path
        self.statistic_latency_max = statistic_latency_max
        self.statistic_latency_step = statistic_latency_step
        self.connection_percentile_list = connection_percentile_list


class ConnectionConfig:
    def __init__(self, url, protocol, transport):
        self.url = url
        self.protocol = protocol
        self.transport = transport


class ConstantConfig:
    def __init__(self, module, wait_time, config_save_path, criteria_max_fail_connection_amount,
                 criteria_max_fail_connection_percentage, criteria_max_fail_sending_percentage):
        self.wait_time = wait_time
        self.module = module
        self.config_save_path = config_save_path
        self.criteria_max_fail_connection_amount = criteria_max_fail_connection_amount
        self.criteria_max_fail_connection_percentage = criteria_max_fail_connection_percentage
        self.criteria_max_fail_sending_percentage = criteria_max_fail_sending_percentage


class SendingConfig:
    def __init__(self, duration, interval, message_size):
        self.duration = duration
        self.interval = interval
        self.message_size = message_size


class SettingParaKey:
    def __init__(self):
        self.normal_connection = "normal_connection"
        self.max_connection = "max_connection"
        self.base_step = "base_step"
        self.step = "step"
        self.concurrent = "concurrent"
        self.step_length = "step_length"
        self.unit_map = "unit_map"
        self.group_count = "group_count"
        self.max_group_count = "max_group_count"
        self.batch_mode = "batch_mode"
        self.batch_wait = "batch_wait"


def parse_settings(path):
    with open(path, 'r') as f:
        content = f.read()
        config = yaml.load(content, Loader=yaml.Loader)
    return config


def determine_scenario_config(settings,
                              unit,
                              scenario,
                              transport,
                              protocol,
                              use_max_connection,
                              message_size,
                              group,
                              group_config_mode,
                              streaming_item_count,
                              streaming_item_send_interval):
    scenario_type = ScenarioType()

    if scenario == scenario_type.send_to_client:
        key = "{}:{},{}:{},{}:{},{}:{}".format("scenario", scenario, "transport", transport, "protocol", protocol,
                                               "message_size", message_size)
    elif scenario.endswith("Group"): # scenarios end with "Group" will automatically check group value
        key = "{}:{},{}:{},{}:{}".format("scenario", scenario, "transport", transport, "group", group)
    else:
        key = "{}:{},{}:{}".format("scenario", scenario, "transport", transport)

    #print(key)
    para_key = SettingParaKey()
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

    connections = cur_settings[para_key.normal_connection][index] if use_max_connection is False else \
        cur_settings[para_key.max_connection][index]
    concurrent = cur_settings[para_key.concurrent][index]
    base_step = cur_settings[para_key.base_step][index]
    step = cur_settings[para_key.step][index]
    step_length = cur_settings[para_key.step_length][index]
    batch_mode = cur_settings[para_key.batch_mode][index] if para_key.batch_mode in cur_settings else "HighPress"
    batch_wait = cur_settings[para_key.batch_wait][index] if para_key.batch_wait in cur_settings else 1000

    if scenario.endswith("Group"):
      group_count = cur_settings[para_key.group_count][index] if use_max_connection is False else \
                           cur_settings[para_key.max_group_count][index]
    else:
      group_count = 0

    config = ScenarioConfig(scenario, connections, concurrent, base_step, step, step_length,
                            batch_mode, batch_wait, group_count, group, group_config_mode,
                            streaming_item_count, streaming_item_send_interval)

    return config

