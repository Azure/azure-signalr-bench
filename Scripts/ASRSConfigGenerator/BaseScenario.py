from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep

class BaseScenario:
    def __init__(self,
                 sending_config,
                 scenario_config,
                 connection_config,
                 statistics_config,
                 constant_config,
                 connection_type,
                 kind_type):
        self.sending_config = sending_config
        self.scenario_config = scenario_config
        self.statistics_config = statistics_config
        self.connection_config = connection_config
        self.constant_config = constant_config
        self.connection_type = connection_type
        self.kind_type = kind_type
        self.pre_config = []
        self.post_config = []
        self.sending = []
        self.post_act_after_reconnect = "None"

    def GetPreConfig(self):
        return self.pre_config

    def GetPostConfig(self):
        return self.post_config

    def GetSending(self):
        return self.sending

    def BuildCommonPreSending(self):
        self.pre_config += CommonStep.pre_sending_steps(self.scenario_config.type,
                                                        self.connection_config,
                                                        self.statistics_config,
                                                        self.scenario_config,
                                                        self.constant_config,
                                                        self.connection_type)

    def BuildLongrunCommonPreSending(self):
        self.pre_config += CommonStep.longrun_pre_sending_steps(self.scenario_config.type,
                                                                self.connection_config,
                                                                self.statistics_config,
                                                                self.scenario_config,
                                                                self.constant_config,
                                                                self.connection_type)


    def BuildCollectConnectionId(self):
        self.pre_config += [collect_connection_id(self.scenario_config.type)]

    def BuildJoinGroup(self):
        self.pre_config += [join_group(self.scenario_config.type,
                                       self.scenario_config.group_count,
                                       self.scenario_config.connections)]

    def BuildLeaveGroup(self):
        self.post_config += [leave_group(self.scenario_config.type,
                                         self.scenario_config.group_count,
                                         self.scenario_config.connections)]

    def BuildConstantWait(self):
        self.pre_config += [wait(self.scenario_config.type,
                                 self.constant_config.wait_time)]

    def BuildReconnect(self):
        self.pre_config += [reconnect(self.scenario_config.type,
                                      self.scenario_config.connections,
                                      self.connection_config.url,
                                      self.connection_config.protocol,
                                      self.connection_config.transport,
                                      self.scenario_config.concurrent,
                                      self.scenario_config.batch_mode,
                                      self.scenario_config.batch_wait)]

    def BuildPostSending(self):
        self.post_config += CommonStep.post_sending_steps(self.scenario_config.type)

    def generate_config(self):
        if self.kind_type == PERF_KIND:
            self.generate_config_4_perf()
        else:
            self.generate_config_4_longrun()
        self.BuildPostSending()
        self.GenerateConfig()

    def GenerateConfig(self):
        pipeline = self.GetPreConfig() + self.GetSending() + self.GetPostConfig()
        config = TemplateSetter.set_config(self.constant_config.module, [self.scenario_config.type], pipeline)
        ConfigSaver.save_yaml(config, self.constant_config.config_save_path)

    def SimpleConditionalStopReconnect(self):
        if self.kind_type == PERF_KIND:
           self.sending += CommonStep.conditional_stop_and_reconnect_steps(
                           self.sending,
                           self.scenario_config,
                           self.constant_config,
                           self.connection_config)
        else:
           self.sending += [conditional_stop(self.scenario_config.type,
                             self.constant_config.criteria_max_fail_connection_percentage,
                             self.scenario_config.connections + 1,
                             self.constant_config.criteria_max_fail_sending_percentage)]

    def BuildLongrunSending(self):
        self.sending = [repair_connection(self.scenario_config.type,
                                          self.post_act_after_reconnect)]
        self.BuildSending()

    # subclass must implements
    def BuildSending(self):
        pass

    def generate_config_4_perf(self):
        pass

    def generate_config_4_longrun(self):
        pass
