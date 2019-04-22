from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
from Util.Common import *
import RestGroupBase as ParentClass

class RestSendToGroup(ParentClass.RestGroupBase):
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type):
        super().__init__(sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type)
        self.post_act_after_reconnect = "JoinToGroup"

