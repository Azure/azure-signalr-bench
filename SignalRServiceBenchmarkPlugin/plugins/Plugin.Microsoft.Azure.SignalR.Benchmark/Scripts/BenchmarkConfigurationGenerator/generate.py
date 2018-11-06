from Echo import *
from Broadcast import *
from SendToClient import *
from SendToGroup import *
from FrequentJoinLeaveGroup import *
import argparse
from Util.SettingsHelper import *
from Util.Common import *


def parse_arguments():
    arg_type = ArgType()
    scenario_type = ScenarioType()
    parser = argparse.ArgumentParser(description='Generate benchmark configuration')

    # required
    parser.add_argument('-u', '--unit', type=int, required=True, help="Azure SignalR service unit.")
    parser.add_argument('-S', '--scenario', required=True, choices=[scenario_type.echo, scenario_type.broadcast,
                                                                    scenario_type.send_to_client,
                                                                    scenario_type.send_to_group,
                                                                    scenario_type.frequent_join_leave_group],
                        help="Scenario, choose from <{}>|<{}>|<{}>|<{}>|<{}>"
                        .format(scenario_type.echo,
                                scenario_type.broadcast,
                                scenario_type.send_to_client,
                                scenario_type.send_to_group,
                                scenario_type.frequent_join_leave_group))
    parser.add_argument('-p', '--protocol', required=True, choices=[arg_type.protocol_json,
                                                                    arg_type.protocol_messagepack],
                        help="SignalR Hub protocol, choose from <{}>|<{}>".format(arg_type.protocol_json,
                                                                                  arg_type.protocol_messagepack))
    parser.add_argument('-t', '--transport', required=True, choices=[arg_type.transport_websockets,
                                                                     arg_type.transport_long_polling,
                                                                     arg_type.transport_server_sent_event],
                        help="SignalR connection transport type, choose from: <{}>|<{}>|<{}>".format(
                            arg_type.transport_websockets, arg_type.transport_long_polling,
                            arg_type.transport_server_sent_event))
    parser.add_argument('-U', '--url', required=True, help="App server Url")
    parser.add_argument('-m', '--use_max_connection', action='store_true',
                        help="Flag indicates using max connection or not. Set true to apply 1.5x on normal connections")

    # todo: add default value
    parser.add_argument('-g', '--group_type', type=str, choices=[arg_type.group_tiny, arg_type.group_small,
                                                                 arg_type.group_big], default=arg_type.group_tiny,
                        help="Group type, choose from <{}>|<{}>|<{}>".format(arg_type.group_tiny, arg_type.group_small,
                                                                             arg_type.group_big))
    # todo: set default value
    parser.add_argument('-ms', '--message_size', type=int, default=2*1024, help="Message size")
    # todo: set default value
    parser.add_argument('-M', '--module', required=True, help='Plugin name')
    # todo: set default value
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='Settings from different unit')
    parser.add_argument('-d', '--duration', type=int, default=240000, help='Duration to run (second)')
    parser.add_argument('-i', '--interval', type=int, default=1000, help='Interval for message sending')
    parser.add_argument('-so', '--statistics_output_path', default='counters.txt',
                        help='Path to counters which record the statistics while running benchmark')
    parser.add_argument('-si', '--statistic_interval', type=int, default=1000, help='Interval for collecting intervals')
    parser.add_argument('-w', '--wait_time', type=int, default=5000, help='Waiting time for each epoch')
    parser.add_argument('-c', '--config_save_path', default='config.yaml',
                        help='Path of output benchmark configuration')

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
                                                args.protocol, args.use_max_connection, args.message_size,
                                                args.group_type)

    # basic sending config
    sending_config = SendingConfig(args.duration, args.interval, args.message_size)

    if args.scenario == "echo":
        Echo(sending_config, scenario_config, connection_config, statistics_config, constant_config).generate_config()
    elif args.scenario == "broadcast":
        Broadcast(sending_config, scenario_config, connection_config, statistics_config, constant_config)\
            .generate_config()
    elif args.scenario == 'sendToClient':
        SendToClient(sending_config, scenario_config, connection_config, statistics_config, constant_config)\
            .generate_config()
    elif args.scenario == 'sendToGroup':
        SendToGroup(sending_config, scenario_config, connection_config, statistics_config, constant_config) \
            .generate_config()
    elif args.scenario == 'frequentJoinLeaveGroup':
        FrequentJoinLeaveGroup(sending_config, scenario_config, connection_config, statistics_config, constant_config) \
            .generate_config()


if __name__ == "__main__":
    main()
