from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep
import RestBase as ParentClass

class PersistBroadcast(ParentClass.RestBase):
    def __init__(self, sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type):
        super().__init__(sending_config, scenario_config, connection_config, statistics_config, constant_config, connection_type, kind_type)

