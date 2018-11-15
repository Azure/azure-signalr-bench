Key = {
    'Types': 'Types',
    'ModuleName': 'ModuleName',
    'Type': 'Type',
    'Method': 'Method',
    'Pipeline': 'Pipeline',
    'Interval': 'Parameter.Interval',
    'Duration': 'Parameter.Duration',
    'ConnectionTotal': 'Parameter.ConnectionTotal',
    'HubUrl': 'Parameter.HubUrl',
    'Protocol': 'Parameter.Protocol',
    'TransportType': 'Parameter.TransportType',
    'ConcurrentConnetion': 'Parameter.ConcurrentConnetion',
    'MessageSize': 'Parameter.MessageSize',
    'RemainderBegin': 'Parameter.RemainderBegin',
    'RemainderEnd': 'Parameter.RemainderEnd',
    'Modulo': 'Parameter.Modulo',
    'GroupCount': 'Parameter.GroupCount',
    'GroupLevelRemainderBegin': 'Parameter.GroupLevelRemainderBegin',
    'GroupLevelRemainderEnd': 'Parameter.GroupLevelRemainderEnd',
    'GroupInternalRemainderBegin': 'Parameter.GroupInternalRemainderBegin',
    'GroupInternalRemainderEnd': 'Parameter.GroupInternalRemainderEnd',
    'GroupInternalModulo': 'Parameter.GroupInternalModulo',
    'StatisticsOutputPath': 'Parameter.StatisticsOutputPath',
    'CriteriaMaxFailConnectionPercentage': 'Parameter.CriteriaMaxFailConnectionPercentage',
    'CriteriaMaxFailConnectionAmount': 'Parameter.CriteriaMaxFailConnectionAmount',
    'CriteriaMaxFailSendingPercentage': 'Parameter.CriteriaMaxFailSendingPercentage',
    'LatencyStep': 'Parameter.LatencyStep',
    'LatencyMax': 'Parameter.LatencyMax'
}


# Step definitions
def required(type_, method):
    return {
        Key['Type']: type_,
        Key['Method']: method
    }


def wait(type_, duration):
    return [{
        **required(type_, "Wait"),
        **{
            Key['Duration']: duration
        }
    }]


def conditional_stop(type_, criteria_max_fail_connection_percentage, criteria_max_fail_connection_amount,
                     criteria_max_fail_sending_percentage):
    return [{
        **required(type_, "ConditionalStop"),
        **{
            Key['CriteriaMaxFailConnectionPercentage']: criteria_max_fail_connection_percentage,
            Key['CriteriaMaxFailConnectionAmount']: criteria_max_fail_connection_amount,
            Key['CriteriaMaxFailSendingPercentage']: criteria_max_fail_sending_percentage
        }
    }]


def reconnect(type_, connection_total, hub_url, protocol, transport_type, concurrent):
    return [{
        **required(type_, "Reconnect"),
        **{
            Key['ConnectionTotal']: connection_total,
            Key['HubUrl']: hub_url,
            Key['Protocol']: protocol,
            Key['TransportType']: transport_type,
            Key['ConcurrentConnetion']: concurrent
        }
    }]


def register_callback_record_latency(type_):
    return [dict(required(type_, "RegisterCallbackRecordLatency"))]


def register_callback_join_group(type_):
    return [dict(required(type_, "RegisterCallbackJoinGroup"))]


def register_callback_leave_group(type_):
    return [dict(required(type_, "RegisterCallbackLeaveGroup"))]


def init_statistics_collector(type_, latency_max, latency_step):
    return [{
        **required(type_, "InitStatisticsCollector"),
        **{
            Key['LatencyMax']: latency_max,
            Key['LatencyStep']: latency_step
        }
    }]


def collect_statistics(type_, interval, output_path):
    return [{
        **required(type_, "CollectStatistics"),
        **{
            Key['Interval']: interval,
            Key['StatisticsOutputPath']: output_path
        }
    }]


def stop_collector(type_):
    return [dict(required(type_, "StopCollector"))]


def create_connection(type_, connection_total, hub_url, protocol, transport_type):
    return [{
        **required(type_, "CreateConnection"),
        **{
            Key['ConnectionTotal']: connection_total,
            Key['HubUrl']: hub_url,
            Key['Protocol']: protocol,
            Key['TransportType']: transport_type
        }
    }]


def start_connection(type_, concurrent_connection):
    return [{
        **required(type_, "StartConnection"),
        **{
            Key['ConcurrentConnetion']: concurrent_connection
        }
    }]


def stop_connection(type_):
    return [required(type_, "StopConnection")]


def dispose_connection(type_):
    return [required(type_, "DisposeConnection")]


def collect_connection_id(type_):
    return [required(type_, "CollectConnectionId")]


def echo_broadcast(type_, method, duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return {
        **required(type_, method),
        **{
            Key['Duration']: duration,
            Key['Interval']: interval,
            Key['RemainderBegin']: remainder_begin,
            Key['RemainderEnd']: remainder_end,
            Key['Modulo']: modulo,
            Key['MessageSize']: message_size
        }
    }


def echo(type_, duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return [echo_broadcast(type_, "Echo", duration, interval, remainder_begin, remainder_end, modulo, message_size)]


def broadcast(type_, duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return [echo_broadcast(type_, "Broadcast", duration, interval, remainder_begin, remainder_end, modulo,
                           message_size)]


def send_to_client(type_, connection_total, duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return [{
        **echo_broadcast(type_, "SendToClient", duration, interval, remainder_begin, remainder_end, modulo,
                         message_size),
        **{
            Key['ConnectionTotal']: connection_total
        }
    }]


def join_leave_group(type_, method, group_count, connection_total):
    return {
        **required(type_, method),
        **{
            Key['GroupCount']: group_count,
            Key['ConnectionTotal']: connection_total
        }
    }


def join_group(type_, group_count, connection_total):
    return [join_leave_group(type_, "JoinGroup", group_count, connection_total)]


def leave_group(type_, group_count, connection_total):
    return [join_leave_group(type_, "LeaveGroup", group_count, connection_total)]


def group(type_, method, duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
          group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
          group_internal_modulo):
    return {
        **required(type_, method),
        **{
            Key['Duration']: duration,
            Key['Interval']: interval,
            Key['MessageSize']: message_size,
            Key['ConnectionTotal']: connection_total,
            Key['GroupCount']: group_count,
            Key['GroupLevelRemainderBegin']: group_level_remainder_begin,
            Key['GroupLevelRemainderEnd']: group_level_remainder_end,
            Key['GroupInternalRemainderBegin']: group_internal_remainder_begin,
            Key['GroupInternalRemainderEnd']: group_internal_remainder_end,
            Key['GroupInternalModulo']: group_internal_modulo,
        }
    }


def send_to_group(type_, duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
                  group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
                  group_internal_modulo):
    return [group(type_, "SendToGroup", duration, interval, message_size, connection_total, group_count,
                  group_level_remainder_begin, group_level_remainder_end, group_internal_remainder_begin,
                  group_internal_remainder_end, group_internal_modulo)]


def frequent_join_leave_group(type_, duration, interval, message_size, connection_total, group_count,
                              group_level_remainder_begin, group_level_remainder_end, group_internal_remainder_begin,
                              group_internal_remainder_end, group_internal_modulo):
    return [group(type_, "FrequentJoinLeaveGroup", duration, interval, message_size, connection_total, group_count,
                  group_level_remainder_begin, group_level_remainder_end, group_internal_remainder_begin,
                  group_internal_remainder_end, group_internal_modulo)]
