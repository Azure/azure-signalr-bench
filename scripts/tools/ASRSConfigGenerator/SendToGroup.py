from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
from Util.Common import *
import BaseScenario as ParentClass

class SendToGroup(ParentClass.BaseScenario):
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type):
        super().__init__(sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type)
        self.post_act_after_reconnect = "JoinToGroup"

    def BuildSending(self):
        remainder_end_dx = self.scenario_config.step

        arg_type = ArgType()

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
                super().SimpleConditionalStopReconnect()

            if self.scenario_config.group_config_mode == arg_type.group_config_mode_group:
                group_member = 3 if self.scenario_config.group_type == arg_type.group_big \
                    else self.scenario_config.connections // self.scenario_config.group_count
                self.sending += [send_to_group_group_mode(self.scenario_config.type,
                                                     self.__class__.__name__,
                                                     self.sending_config.duration,
                                                     self.sending_config.interval,
                                                     self.sending_config.message_size,
                                                     self.scenario_config.connections,
                                                     self.scenario_config.group_count,
                                                     0, remainder_end,
                                                     0, group_member,
                                                     self.scenario_config.connections // self.scenario_config.group_count)]
            else:
                self.sending += [send_to_group_connection_mode(
                                                          self.scenario_config.type,
                                                          self.__class__.__name__,
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

    def generate_config_4_perf(self):
        super().BuildCommonPreSending()
        super().BuildJoinGroup()
        super().BuildConstantWait()
        super().BuildLeaveGroup()
        self.BuildSending()

    def generate_config_4_longrun(self):
        super().BuildLongrunCommonPreSending()
        super().BuildJoinGroup()
        super().BuildConstantWait()
        super().BuildLeaveGroup()
        super().BuildLongrunSending()
