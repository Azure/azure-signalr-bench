from Util.BenchmarkConfigurationStep import *


def pre_sending_steps(type_, connection_config, statistics_config, scenario_config, constant_config, connection_type):
    pre_send = [
        init_statistics_collector(type_, statistics_config.statistic_latency_max,
                                  statistics_config.statistic_latency_step),
        collect_statistics(type_, statistics_config.statistic_interval, statistics_config.statistics_output_path),
        create_connection(type_, scenario_config.connections, connection_config.url, connection_config.protocol,
                          connection_config.transport, connection_type),
        start_connection(type_, scenario_config.concurrent, scenario_config.batch_mode, scenario_config.batch_wait),
        wait(type_, constant_config.wait_time),
        reconnect(scenario_config.type, scenario_config.connections, connection_config.url,
                  connection_config.protocol, connection_config.transport,
                  scenario_config.concurrent, scenario_config.batch_mode, scenario_config.batch_wait)
    ]
    return pre_send


def post_sending_steps(type_):
    post_sending = [
        stop_collector(type_),
        stop_connection(type_),
        dispose_connection(type_)
    ]
    return post_sending


def conditional_stop_and_reconnect_steps(sending, scenario_config, constant_config, connection_config):
    return [
        conditional_stop(scenario_config.type,
                         constant_config.criteria_max_fail_connection_percentage,
                         scenario_config.connections + 1,
                         constant_config.criteria_max_fail_sending_percentage),
        reconnect(scenario_config.type, scenario_config.connections, connection_config.url,
                  connection_config.protocol, connection_config.transport,
                  scenario_config.concurrent, scenario_config.batch_mode, scenario_config.batch_wait),
        conditional_stop(scenario_config.type,
                         constant_config.criteria_max_fail_connection_percentage,
                         scenario_config.connections + 1, 2.00)
    ]
