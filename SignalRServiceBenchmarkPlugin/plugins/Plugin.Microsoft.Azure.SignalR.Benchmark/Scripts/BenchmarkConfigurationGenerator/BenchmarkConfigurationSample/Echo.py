from BenchmarkConfigurationStep import *
import Common

type_ = 'echo'
total_connection = 20000
wait_time = 5 * 1000
duration = 10 * 1000
interval = 1 * 1000
statistic_interval = 1 * 1000
message_size = 40
remainder_begin = 0
remainder_end = 1
remainder_end_dx = 3
concurrent_connection = 100
protocol = 'json'
transport_type = 'Websockets'
types = [type_]
pipeline = [
    init_statistics_collector(type_) + init_statistics_collector(type_),
    collect_statistics(type_, statistic_interval, './counters.txt'),
    create_connection(type_, total_connection, Common.hub_url, protocol, transport_type),
    start_connection(type_, concurrent_connection),
    echo(type_, duration, interval, remainder_begin, remainder_end, total_connection, message_size),
    wait(type_, wait_time),
    echo(type_, duration, interval, remainder_begin, remainder_end + remainder_end_dx, total_connection, message_size),
    wait(type_, wait_time),
    stop_collector(type_),
    stop_connection(type_),
    dispose_connection(type_)
]

Config = Common.set_config(types, pipeline)

