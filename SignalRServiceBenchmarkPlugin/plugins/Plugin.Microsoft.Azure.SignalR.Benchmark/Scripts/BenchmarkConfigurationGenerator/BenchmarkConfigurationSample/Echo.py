from BenchmarkConfigurationStep import *
import Common

type_ = 'echo'
total_connection = 20000
wait_time = 5 * 1000
duration = 60 * 1000
interval = 1 * 1000
statistic_interval = 1 * 1000
message_size = 2 * 1024
remainder_begin = 0
remainder_end_dx = 1000
remainder_end_base = remainder_end_dx
concurrent_connection = 100
protocol = 'json'
transport_type = 'Websockets'
types = [type_]

pre_send = [
    init_statistics_collector(type_),
    collect_statistics(type_, statistic_interval, './counters_{}.txt'.format(type_)),
    create_connection(type_, total_connection, Common.hub_url, protocol, transport_type),
    start_connection(type_, concurrent_connection),
]

sending = []
for remainder_end in range(remainder_end_base, total_connection + 1, remainder_end_dx):
    sending += [
        echo(type_, duration, interval, remainder_begin, remainder_end, total_connection, message_size),
        wait(type_, wait_time)
    ]

post_sending = [
    stop_collector(type_),
    stop_connection(type_),
    dispose_connection(type_)
]

pipeline = pre_send + sending + post_sending

Config = Common.set_config(types, pipeline)

