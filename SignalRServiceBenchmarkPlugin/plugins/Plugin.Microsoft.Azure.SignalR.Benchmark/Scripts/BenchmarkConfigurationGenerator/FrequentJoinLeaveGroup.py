from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
from Util.Common import *


class FrequentJoinLeaveGroup:
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config):
        self.sending_config = sending_config
        self.scenario_config = scenario_config
        self.statistics_config = statistics_config
        self.connection_config = connection_config
        self.constant_config = constant_config

    def generate_config(self):
        pre_sending = CommonStep.pre_sending_steps(self.scenario_config.type, self.connection_config,
                                                   self.statistics_config, self.scenario_config)
        pre_sending += [register_callback_record_latency(self.scenario_config.type)]

        post_sending = CommonStep.post_sending_steps(self.scenario_config.type)

        remainder_end_dx = self.scenario_config.step

        arg_type = ArgType()
        sending = []
        for epoch in range(0, self.scenario_config.step_length):
            group_member = 3 if self.scenario_config.group_type == arg_type.group_big \
                else self.scenario_config.connections // self.scenario_config.group_count
            remainder_end = self.scenario_config.base_step + epoch * remainder_end_dx

            # conditional stop and reconnect
            if epoch > 0:
                CommonStep.conditional_stop(sending, self.scenario_config, self.constant_config,
                                            self.connection_config)

            sending += [
                frequent_join_leave_group(self.scenario_config.type, self.sending_config.duration,
                                          self.sending_config.interval,
                                          self.sending_config.message_size, self.scenario_config.connections,
                                          self.scenario_config.group_count, 0, remainder_end,
                                          0, group_member,
                                          self.scenario_config.connections // self.scenario_config.group_count),
                wait(self.scenario_config.type, self.constant_config.wait_time)
            ]

        pipeline = pre_sending + sending + post_sending

        config = TemplateSetter.set_config(self.constant_config.module, [self.scenario_config.type], pipeline)

        ConfigSaver.save_yaml(config, self.constant_config.config_save_path)
