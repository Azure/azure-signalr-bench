from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
from Util.Common import *
import BaseScenario as ParentClass

class FrequentJoinLeaveGroup(ParentClass.BaseScenario):
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config, useAspNet=0):
        super().__init__(sending_config, scenario_config, connection_config, statistics_config, constant_config, useAspNet)

    def BuildSending(self):
        remainder_end_dx = self.scenario_config.step

        arg_type = ArgType()

        self.sending = []

        for epoch in range(0, self.scenario_config.step_length):

            remainder_end = self.scenario_config.base_step + epoch * remainder_end_dx

            if self.scenario_config.group_config_mode == arg_type.group_config_mode_connection and \
               remainder_end > self.scenario_config.connections:
                    break

            if self.scenario_config.group_config_mode == arg_type.group_config_mode_group and \
                    remainder_end > self.scenario_config.group_count:
                    break

            # conditional stop and reconnect
            if epoch > 0:
                self.sending += CommonStep.conditional_stop_and_reconnect_steps(self.sending, self.scenario_config,
                                                                           self.constant_config,
                                                                           self.connection_config)

            if self.scenario_config.group_config_mode == arg_type.group_config_mode_group:
                group_member = 3 if self.scenario_config.group_type == arg_type.group_big \
                    else self.scenario_config.connections // self.scenario_config.group_count
                self.sending += [frequent_join_leave_group_group_mode(self.scenario_config.type,
                                                                 self.sending_config.duration,
                                                                 self.sending_config.interval,
                                                                 self.sending_config.message_size,
                                                                 self.scenario_config.connections,
                                                                 self.scenario_config.group_count, 0, remainder_end,
                                                                 0, group_member,
                                                                 self.scenario_config.connections //
                                                                 self.scenario_config.group_count)]
            else:
                self.sending += [frequent_join_leave_group_connection_mode(self.scenario_config.type,
                                                                      self.sending_config.duration,
                                                                      self.sending_config.interval,
                                                                      self.sending_config.message_size,
                                                                      self.scenario_config.connections,
                                                                      self.scenario_config.group_count,
                                                                      0, remainder_end,
                                                                      self.scenario_config.connections)]
            self.sending += [
                wait(self.scenario_config.type, self.constant_config.wait_time)
            ]

    def generate_config(self):
        super().BuildCommonPreSending()
        super().BuildRegRecordLatency()
        super().BuildPostSending()
        self.BuildSending()
        super().GenerateConfig()
