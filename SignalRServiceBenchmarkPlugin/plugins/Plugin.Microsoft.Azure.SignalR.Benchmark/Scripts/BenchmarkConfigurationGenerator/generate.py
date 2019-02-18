from Echo import *
from Broadcast import *
from RestSendToUser import *
from RestBroadcast import *
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
    parser.add_argument('-S', '--scenario', required=True, choices=[scenario_type.echo,
                                                                    scenario_type.broadcast,
                                                                    scenario_type.rest_broadcast,
                                                                    scenario_type.rest_send_to_user,
                                                                    scenario_type.send_to_client,
                                                                    scenario_type.send_to_group,
                                                                    scenario_type.frequent_join_leave_group],
                        help="Scenario, choose from <{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>"
                        .format(scenario_type.echo,
                                scenario_type.broadcast,
                                scenario_type.rest_broadcast,
                                scenario_type.rest_send_to_user,
                                scenario_type.send_to_client,
                                scenario_type.send_to_group,
                                scenario_type.frequent_join_leave_group))
    parser.add_argument('-p', '--protocol', required=False, default=arg_type.protocol_json, choices=[arg_type.protocol_json,
                                                                    arg_type.protocol_messagepack],
                        help="SignalR Hub protocol, choose from <{}>|<{}>".format(arg_type.protocol_json,
                                                                                  arg_type.protocol_messagepack))
    parser.add_argument('-t', '--transport', required=False, default=arg_type.transport_websockets, choices=[arg_type.transport_websockets,
                                                                     arg_type.transport_long_polling,
                                                                     arg_type.transport_server_sent_event],
                        help="SignalR connection transport type, choose from: <{}>|<{}>|<{}>".format(
                            arg_type.transport_websockets, arg_type.transport_long_polling,
                            arg_type.transport_server_sent_event))
    parser.add_argument('-U', '--url', required=True, help="App server Url or connection string (only for REST API test)")
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
    parser.add_argument('-M', '--module', help='Plugin name', default='Plugin.Microsoft.Azure.SignalR.Benchmark.\
SignalRBenchmarkPlugin, Plugin.Microsoft.Azure.SignalR.Benchmark')
    # todo: set default value
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='Settings from different unit')
    parser.add_argument('-d', '--duration', type=int, default=240, help='Duration to run (second)')
    parser.add_argument('-i', '--interval', type=int, default=1000, help='Interval for message sending (millisecond)')
    parser.add_argument('-c', '--config_save_path', default='config.yaml',
                        help='Path of output benchmark configuration')

    # args for conditional stop
    parser.add_argument('-cc', '--criteria_max_fail_connection_amount', type=int, default=100, help='Criteria for max \
failed connection amount')
    parser.add_argument('-cp', '--criteria_max_fail_connection_percentage', type=float, default=0.01, help='Criteria \
for max failed connection percentage')
    parser.add_argument('-cs', '--criteria_max_fail_sending_percentage', type=float, default=0.01, help='Criteria \
for max failed sending percentage')

    # args for statistics collector
    parser.add_argument('-so', '--statistics_output_path', default='counters.txt',
                        help='Path to counters which record the statistics while running benchmark')
    parser.add_argument('-si', '--statistic_interval', type=int, default=1000, help='Interval for collecting intervals')
    parser.add_argument('-w', '--wait_time', type=int, default=15000, help='Waiting time for each epoch')
    parser.add_argument('-lm', '--statistic_latency_max', type=int, default=1000, help='Latency max of statistics')
    parser.add_argument('-ls', '--statistic_latency_step', type=int, default=100, help='Latency step of statistics')

    # group config mode
    parser.add_argument('-gm', '--group_config_mode', choices=[arg_type.group_config_mode_group,
                                                               arg_type.group_config_mode_connection],
                        default=arg_type.group_config_mode_connection, help='Group configuration mode')
    parser.add_argument('-ct', '--connection_type', type=str,
                         choices=[arg_type.connection_type_core,
                                  arg_type.connection_type_aspnet,
                                  arg_type.connection_type_rest_direct],
                         default=arg_type.connection_type_core,
                         help='Specify the connection type: Core, AspNet, or CoreDirect')
    # args
    args = parser.parse_args()

    # unit convert from second to millisecond
    args.duration = args.duration * 1000

    return args


def main():

    args = parse_arguments()

    # parse settings
    scenario_config_collection = parse_settings(args.settings)

    # constant config
    constant_config = ConstantConfig(args.module, args.wait_time, args.config_save_path,
                                     args.criteria_max_fail_connection_amount,
                                     args.criteria_max_fail_connection_percentage,
                                     args.criteria_max_fail_sending_percentage)

    # statistics config
    statistics_config = StatisticsConfig(args.statistics_output_path, args.statistic_interval,
                                         args.statistic_latency_max, args.statistic_latency_step)

    # connection config
    connection_config = ConnectionConfig(args.url, args.protocol, args.transport)

    # determine settings
    scenario_config = determine_scenario_config(scenario_config_collection, args.unit, args.scenario, args.transport,
                                                args.protocol, args.use_max_connection, args.message_size,
                                                args.group_type, args.group_config_mode)

    # basic sending config
    sending_config = SendingConfig(args.duration, args.interval, args.message_size)

    lst = [word[0].upper() + word[1:] for word in args.scenario.split()]
    func = "".join(lst)
    callfunc = "{func_name}(sending_config, scenario_config, connection_config, statistics_config, constant_config, args.connection_type).generate_config()".format(
      func_name=func)
    eval(callfunc)

if __name__ == "__main__":
    main()
