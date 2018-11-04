from Echo import *
from Broadcast import *
from SendToClient import *
import argparse
import yaml
from Util.SettingsHelper import *

def parse_settings(path):
    with open(path, 'r') as f:
        content = f.read()
        config = yaml.load(content, Loader=yaml.Loader)
    return config


def parse_arguments():
    parser = argparse.ArgumentParser(description='')

    # required
    parser.add_argument('-u', '--unit', type=int, required=True)
    parser.add_argument('-S', '--scenario', required=True)
    parser.add_argument('-p', '--protocol', required=True)
    parser.add_argument('-t', '--transport', required=True)
    parser.add_argument('-U', '--url', required=True)
    parser.add_argument('-m', '--use_max_connection', action='store_true')
    parser.add_argument('-sc', '--slave_count', type=int, required=True)

    # todo: add default value
    parser.add_argument('-ms', '--message_size', type=int, default=2*1024)  # todo: set default value
    parser.add_argument('-M', '--module', required=True)  # todo: set default value
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='')  # todo: set default value
    parser.add_argument('-d', '--duration', type=int, default=240000)
    parser.add_argument('-i', '--interval', type=int, default=1000)
    parser.add_argument('-so', '--statistics_output_path', default='counters.txt')
    parser.add_argument('-si', '--statistic_interval', type=int, default=1000)
    parser.add_argument('-w', '--wait_time', type=int, default=5000)
    parser.add_argument('-c', '--config_save_path', default='config.yaml')

    args = parser.parse_args()

    return args


def main():

    args = parse_arguments()

    # parse settings
    scenario_config_collection = parse_settings(args.settings)

    # constant config
    constant_config = ConstantConfig(args.module, args.wait_time, args.config_save_path)

    # statistics config
    statistics_config = StatisticsConfig(args.statistics_output_path, args.statistic_interval)

    # connection config
    connection_config = ConnectionConfig(args.url, args.protocol, args.transport)

    # determine settings
    scenario_config = determine_scenario_config(scenario_config_collection, args.unit, args.scenario, args.transport,
                                                args.use_max_connection, args.message_size)

    # basic sending config
    sending_config = SendingConfig(args.duration, args.interval, args.slave_count, args.message_size)

    if args.scenario == "echo":
        Echo(sending_config, scenario_config, connection_config, statistics_config, constant_config).generate_config()
    elif args.scenario == "broadcast":
        Broadcast(sending_config, scenario_config, connection_config, statistics_config, constant_config)\
            .generate_config()
    elif args.scenario == 'sendToClient':
        SendToClient(sending_config, scenario_config, connection_config, statistics_config, constant_config)\
            .generate_config()


if __name__ == "__main__":
    main()
