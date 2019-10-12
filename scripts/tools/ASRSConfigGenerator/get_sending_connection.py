import argparse
from RestPersistBroadcast import *
from RestPersistSendToGroup import *
from RestPersistSendToUser import *
from RestSendToGroup import *
from RestSendToUser import *
from RestBroadcast import *
from SendToClient import *
from SendToGroup import *
from StreamingEcho import *
from Util.SettingsHelper import *


def parse_arguments():
    arg_type = ArgType()
    scenario_type = ScenarioType()

    parser = argparse.ArgumentParser(description='')

    # required
    parser.add_argument('-u', '--unit', type=int, required=True, help='Azure SignalR service unit.')
    parser.add_argument('-S',
                        '--scenario',
                        required=True,
                        choices=[scenario_type.echo,
                                 scenario_type.broadcast,
                                 scenario_type.rest_persist_broadcast,
                                 scenario_type.rest_persist_send_to_group,
                                 scenario_type.rest_persist_send_to_user,
                                 scenario_type.rest_broadcast,
                                 scenario_type.rest_send_to_group,
                                 scenario_type.rest_send_to_user,
                                 scenario_type.send_to_client,
                                 scenario_type.send_to_group,
                                 scenario_type.streaming_echo,
                                 scenario_type.frequent_join_leave_group],
                        help="Scenario, choose from <{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>|<{}>"
                        .format(scenario_type.echo,
                                scenario_type.broadcast,
                                scenario_type.rest_persist_broadcast,
                                scenario_type.rest_persist_send_to_group,
                                scenario_type.rest_persist_send_to_user,
                                scenario_type.rest_broadcast,
                                scenario_type.rest_send_to_group,
                                scenario_type.rest_send_to_user,
                                scenario_type.send_to_client,
                                scenario_type.send_to_group,
                                scenario_type.streaming_echo,
                                scenario_type.frequent_join_leave_group))
    parser.add_argument('-p', '--protocol', required=True, choices=[arg_type.protocol_json,
                                                                    arg_type.protocol_messagepack],
                        help="SignalR Hub protocol, choose from <{}>|<{}>, default is {}".format(arg_type.protocol_json,
                                                                   arg_type.protocol_messagepack,
                                                                   arg_type.protocol_json))
    parser.add_argument('-t', '--transport', required=True, choices=[arg_type.transport_websockets,
                                                                     arg_type.transport_long_polling,
                                                                     arg_type.transport_server_sent_event],
                        help="SignalR connection transport type, choose from: <{}>|<{}>|<{}>".format(
                            arg_type.transport_websockets, arg_type.transport_long_polling,
                            arg_type.transport_server_sent_event))
    parser.add_argument('-ms', '--message_size', type=int, default=2*1024, help="Message size")
    # todo: set default value
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='Settings from different unit')
    parser.add_argument('-m', '--use_max_connection', action='store_true',
                        help="Flag indicates using max connection or not. Set true to apply 1.5x on normal connections")
    parser.add_argument('-q', '--query', choices=["sendingSteps","concurrentConnection","totalConnections"], help='Specify the query item', required=True)
    parser.add_argument('-g', '--group_type', type=str, choices=[arg_type.group_tiny, arg_type.group_small,
                                                                 arg_type.group_big], default=arg_type.group_tiny,
                        help="Group type, choose from <{}>|<{}>|<{}>, default is {}".format(arg_type.group_tiny, arg_type.group_small,
                                                                             arg_type.group_big, arg_type.group_tiny))
    # group config mode
    parser.add_argument('-gm', '--group_config_mode', choices=[arg_type.group_config_mode_group,
                                                               arg_type.group_config_mode_connection],
                        default=arg_type.group_config_mode_connection,
                        help='Group configuration mode, default is {}'.format(arg_type.group_config_mode_connection))
    args = parser.parse_args()

    return args

def sendingSteps(scenario_config):
    step_list = [scenario_config.base_step + i * scenario_config.step for i in range(0, scenario_config.step_length)]
    print(','.join(str(step) for step in step_list if step <= scenario_config.connections))

def concurrentConnection(scenario_config):
    print(scenario_config.concurrent)

def totalConnections(scenario_config):
    print(scenario_config.connections)

def main():
    args = parse_arguments()

    scenario_config_collection = parse_settings(args.settings)

    # determine settings
    scenario_config = determine_scenario_config(scenario_config_collection,
                                                args.unit,
                                                args.scenario,
                                                args.transport,
                                                args.protocol,
                                                args.use_max_connection,
                                                args.message_size,
                                                args.group_type,
                                                args.group_config_mode)

    func="{f}(scenario_config)".format(f=args.query)
    eval(func)


if __name__ == "__main__":
    main()
