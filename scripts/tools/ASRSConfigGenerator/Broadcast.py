from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
import BaseScenario as ParentClass

class Broadcast(ParentClass.BaseScenario):
    def __init__(self,
                 sending_config,
                 scenario_config,
                 connection_config,
                 statistics_config,
                 constant_config,
                 connection_type,
                 kind_type):
        super().__init__(sending_config,
                         scenario_config,
                         connection_config,
                         statistics_config,
                         constant_config,
                         connection_type,
                         kind_type)

    def BuildSending(self):
        remainder_begin = 0
        remainder_end_dx = self.scenario_config.step

        for epoch in range(0, self.scenario_config.step_length):
            remainder_end = self.scenario_config.base_step + epoch * remainder_end_dx

            # if remainder_end - remainder_begin > self.scenario_config.connections:
            #     break

            # conditional stop and reconnect
            if epoch > 0:
                super().SimpleConditionalStopReconnect()

            self.sending += [
                [generate_send(self.scenario_config.type,
                              self.__class__.__name__,
                              self.sending_config.duration,
                              self.sending_config.interval,
                              remainder_begin,
                              remainder_end,
                              self.scenario_config.connections,
                              self.sending_config.message_size)],
                wait(self.scenario_config.type, self.constant_config.wait_time)
            ]

    def generate_config_4_perf(self):
        super().BuildCommonPreSending()
        self.BuildSending()

    def generate_config_4_longrun(self):
        super().BuildLongrunCommonPreSending()
        super().BuildLongrunSending()

