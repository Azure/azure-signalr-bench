from Util.BenchmarkConfigurationStep import *
from Util import TemplateSetter, ConfigSaver, CommonStep

module_name = "Module name"
scenarios = ["scenario echo", "scenario broadcast"]

# unit: ms
duration = 10000
interval = 1000 

connections = 100
concurrent_connection = 10
message_size = 2048
url = 'http://localhost:5050/signalrbench'
transport_type = 'WebSockets'
protocol = 'json'

pre_sending = [
    create_connection(scenarios[0], connections, url, protocol, transport_type) + create_connection(scenarios[1], connections, url, protocol, transport_type),
    start_connection(scenarios[0], concurrent_connection) + start_connection(scenarios[1], concurrent_connection)
]

# 'echo' and 'broadcast' will be executed parallelly
# Combination of two 'waits' will be executed after combination of 'echo' and 'broadcast' finish
# Structure of pipeline:
#
# [[sub-step11, sub-step12], [sub-step21, sub-step22]]
#       (s t e p  - 1)            (s t e p  - 2)
# steps are executed in order while sub-steps in the same step are executed parallelly
sending = [
    echo(scenarios[0], duration, interval, 0, connections, connections, message_size) + broadcast(scenarios[0], duration, interval, 0, 1, connections, message_size),
    wait(scenarios[0], interval) + wait(scenarios[1], interval)
]

post_sending = [
    stop_connection(scenarios[0]) + stop_connection(scenarios[1]),
    dispose_connection(scenarios[0]) + dispose_connection(scenarios[1])
]

pipeline = pre_sending + sending + post_sending

config = TemplateSetter.set_config(module_name, scenarios, pipeline)

print(config)
ConfigSaver.save_yaml(config, "Mix.yaml")

