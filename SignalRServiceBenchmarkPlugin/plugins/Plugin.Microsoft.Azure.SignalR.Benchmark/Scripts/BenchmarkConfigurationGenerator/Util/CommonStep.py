from Util.BenchmarkConfigurationStep import *


def pre_sending_steps(type_, connection_config, statistics_config, scenario_config):
    pre_send = [
        init_statistics_collector(type_),
        collect_statistics(type_, statistics_config.statistic_interval, statistics_config.statistics_output_path),
        create_connection(type_, scenario_config.connections, connection_config.url, connection_config.protocol,
                          connection_config.transport),
        start_connection(type_, scenario_config.concurrent)
    ]
    return pre_send


def post_sending_steps(type_):
    post_sending = [
        stop_collector(type_),
        stop_connection(type_),
        dispose_connection(type_)
    ]
    return post_sending
