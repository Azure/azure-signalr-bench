import argparse
from Util.SettingsHelper import *


def parse_arguments():
    parser = argparse.ArgumentParser(description='')

    # required
    parser.add_argument('-u', '--unit', type=int, required=True)
    parser.add_argument('-S', '--scenario', required=True)
    parser.add_argument('-p', '--protocol', required=True)
    parser.add_argument('-t', '--transport', required=True)
    parser.add_argument('-ms', '--message_size', type=int, default=2*1024)
    parser.add_argument('-s', '--settings', type=str, default='settings.yaml', help='')  # todo: set default value

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
