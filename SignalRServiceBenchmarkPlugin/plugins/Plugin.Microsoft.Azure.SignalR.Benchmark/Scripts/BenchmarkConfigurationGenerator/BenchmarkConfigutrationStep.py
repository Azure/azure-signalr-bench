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
    'GroupInternalModulo': 'Parameter.GroupInternalModulo'
}


# Step definitions
# todo
def required(type_, method):
    return {
        Key['Type']: None,
        Key['Method']: None
    }


def init_statistics_collector():
    return dict(required)


def collect_statistics(interval):
    return {
        **required,
        **{
            Key['Interval']: interval,
        }
    }


def stop_collector():
    return dict(required)


# Method
def create_connection(connection_total, hub_url, protocol, transport_type):
    return {
        **required,
        **{
            Key['ConnectionTotal']: connection_total,
            Key['HubUrl']: hub_url,
            Key['Protocol']: protocol,
            Key['TransportType']: transport_type
        }
    }


def start_connection(concurrent_connection):
    return {
        **required,
        **{
            Key['ConcurrentConnetion']: concurrent_connection
        }
    }


def collect_connection_id():
    return dict(required)


def echo_broadcast(duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return {
        **required,
        **{
            Key['Duration']: duration,
            Key['Interval']: interval,
            Key['RemainderBegin']: remainder_begin,
            Key['RemainderEnd']: remainder_end,
            Key['Modulo']: modulo,
            Key['MessageSize']: message_size
        }
    }


def echo(duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return echo_broadcast(duration, interval, remainder_begin, remainder_end, modulo, message_size)


def broadcast(duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return echo_broadcast(duration, interval, remainder_begin, remainder_end, modulo, message_size)


def send_to_client(connection_total, duration, interval, remainder_begin, remainder_end, modulo, message_size):
    return {
        **echo_broadcast(duration, interval, remainder_begin, remainder_end, modulo, message_size),
        **{
            Key['ConnectionTotal']: connection_total
        }
    }


def join_leave_group(group_count, connection_total):
    return {
        **required,
        **{
            Key['GroupCount']: group_count,
            Key['ConnectionTotal']: connection_total
        }
    }


def join_group(group_count, connection_total):
    return join_leave_group(group_count, connection_total)


def leave_group(group_count, connection_total):
    return join_leave_group(group_count, connection_total)


def group(duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
          group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
          group_internal_modulo):
    return {
        **required,
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


def send_to_group(duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
                  group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
                  group_internal_modulo):
    return group(duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
                 group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
                 group_internal_modulo)


def frequent_join_leave_group(duration, interval, message_size, connection_total, group_count,
                              group_level_remainder_begin, group_level_remainder_end, group_internal_remainder_begin,
                              group_internal_remainder_end, group_internal_modulo):
    return group(duration, interval, message_size, connection_total, group_count, group_level_remainder_begin,
                 group_level_remainder_end, group_internal_remainder_begin, group_internal_remainder_end,
                 group_internal_modulo)
