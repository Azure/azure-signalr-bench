from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
import BaseScenario as ParentClass

class SendToClient(ParentClass.BaseScenario):
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type):
        super().__init__(sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type)

    def BuildSending(self):
        remainder_begin = 0
        remainder_end_dx = self.scenario_config.step

        for epoch in range(0, self.scenario_config.step_length):
            remainder_end = self.scenario_config.base_step + epoch * remainder_end_dx

            # if remainder_end - remainder_begin > self.scenario_config.connections:
            #     break

            # conditional stop and reconnect
            if epoch > 0:
                self.sending += [
                    conditional_stop(self.scenario_config.type,
                                     self.constant_config.criteria_max_fail_connection_percentage,
                                     self.scenario_config.connections + 1,
                                     self.constant_config.criteria_max_fail_sending_percentage),
                    reconnect(self.scenario_config.type,
                              self.scenario_config.connections,
                              self.connection_config.url,
                              self.connection_config.protocol,
                              self.connection_config.transport,
                              self.scenario_config.concurrent,
                              self.scenario_config.batch_mode,
                              self.scenario_config.batch_wait),
                    collect_connection_id(self.scenario_config.type),
                    conditional_stop(self.scenario_config.type,
                                     self.constant_config.criteria_max_fail_connection_percentage,
                                     self.scenario_config.connections + 1,
                                     self.constant_config.criteria_max_fail_sending_percentage)
                ]
                #self.sending += CommonStep.conditional_stop_and_reconnect_steps(self.sending, self.scenario_config, self.constant_config,
                #                            self.connection_config)

            self.sending += [
                send_to_client(self.scenario_config.type,
                               self.__class__.__name__,
                               self.scenario_config.connections,
                               self.sending_config.duration,
                               self.sending_config.interval,
                               remainder_begin,
                               remainder_end,
                               self.scenario_config.connections,
                               self.sending_config.message_size),
                wait(self.scenario_config.type, self.constant_config.wait_time)
            ]

    def generate_config_4_perf(self):
        super().BuildCommonPreSending()
        super().BuildCollectConnectionId()
        super().BuildConstantWait()
        super().BuildReconnect()
        super().BuildConstantWait()
        super().BuildCollectConnectionId()
        self.BuildSending()

    def generate_config_4_longrun(self):
        super().BuildLongrunCommonPreSending()
        super().BuildCollectConnectionId()
        super().BuildConstantWait()
        super().BuildLongrunSending()

