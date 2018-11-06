import argparse
from Util.SettingsHelper import *


def parse_arguments():
    scenario_type = ScenarioType()

    parser = argparse.ArgumentParser(description='')

    # required
    parser.add_argument('-u', '--unit', type=int, required=True, help='Azure SignalR service unit.')
    parser.add_argument('-S', '--scenario', required=True,  choices=[scenario_type.echo, scenario_type.broadcast,
                                                                     scenario_type.send_to_client,
                                                                     scenario_type.send_to_group,
                                                                     scenario_type.frequent_join_leave_group],
                        help="Scenario, choose from {}, {}, {}, {}, {}".format(scenario_type.echo,
                                                                               scenario_type.broadcast,
                                                                               scenario_type.send_to_client,
                                                                               scenario_type.send_to_group,
                                                                               scenario_type.frequent_join_leave_group))
    parser.add_argument('-p', '--protocol', required=True, help="SignalR Hub protocol")
    parser.add_argument('-t', '--transport', required=True, help="SignalR connection transport type")
    parser.add_argument('-ms', '--message_size', type=int, default=2*1024, help="Message size")
    # todo: set default value
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='Settings from different unit')

    args = parser.parse_args()

    return args


def main():
    args = parse_arguments()

    scenario_config_collection = parse_settings(args.settings)

    # determine settings
    scenario_config = determine_scenario_config(scenario_config_collection, args.unit, args.scenario, args.transport,
                                                message_size=args.message_size)

    step_list = [scenario_config.base_step + i * scenario_config.step for i in range(0, scenario_config.step_length)]

    return step_list


if __name__ == "__main__":
    main()
