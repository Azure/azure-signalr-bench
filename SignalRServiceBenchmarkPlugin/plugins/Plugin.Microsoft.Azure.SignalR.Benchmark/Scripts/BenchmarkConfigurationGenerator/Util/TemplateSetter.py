from Util import BenchmarkConfigurationStep

Config = {
    BenchmarkConfigurationStep.Key['Types']: None,
    BenchmarkConfigurationStep.Key['ModuleName']: None,
    BenchmarkConfigurationStep.Key['Pipeline']: None
}


def set_config(module_name, types, pipeline):
    config = dict(Config)
    config[BenchmarkConfigurationStep.Key['Types']] = types
    config[BenchmarkConfigurationStep.Key['ModuleName']] = module_name
    config[BenchmarkConfigurationStep.Key['Pipeline']] = pipeline
    return config
