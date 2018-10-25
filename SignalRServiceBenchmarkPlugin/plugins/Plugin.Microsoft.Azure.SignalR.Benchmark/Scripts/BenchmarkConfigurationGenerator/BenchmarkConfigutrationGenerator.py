from BenchmarkConfigutrationStep import *
import yaml

config = {
    Key['Types']: ['P1'],
    Key['ModuleName']: 'TestPlugin',
    Key['Pipeline']: []
}

pipeline = config[Key['Pipeline']]

pipeline.append(echo(duration=1, interval=1, remainder_begin=1, remainder_end=1, modulo=1, message_size=1))


