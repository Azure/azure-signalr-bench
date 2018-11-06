# Scripts for benchmark configuration

## Generate benchmark configuration
`generate.py`

Support different benchmark configurations for different unit

### Supported scenario
* Echo
* Broadcast
* Send to client
* Send to group
* Frequently join/leave group

### Arguments
```
usage: generate.py [-h] -u UNIT -S
                   {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}
                   -p {json,messagepack} -t
                   {Websockets,Longpolling,ServerSentEvent} -U URL [-m]
                   [-g {tiny,small,big}] [-ms MESSAGE_SIZE] -M MODULE
                   [-s SETTINGS] [-d DURATION] [-i INTERVAL]
                   [-so STATISTICS_OUTPUT_PATH] [-si STATISTIC_INTERVAL]
                   [-w WAIT_TIME] [-c CONFIG_SAVE_PATH]

Generate benchmark configuration

optional arguments:
  -h, --help            show this help message and exit
  -u UNIT, --unit UNIT  Azure SignalR service unit.
  -S {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}, --scenario {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}
                        Scenario, choose from <echo>|<broadcast>|<sendToClient
                        >|<sendToGroup>|<frequentJoinLeaveGroup>
  -p {json,messagepack}, --protocol {json,messagepack}
                        SignalR Hub protocol, choose from <json>|<messagepack>
  -t {Websockets,Longpolling,ServerSentEvent}, --transport {Websockets,Longpolling,ServerSentEvent}
                        SignalR connection transport type, choose from:
                        <Websockets>|<Longpolling>|<ServerSentEvent>
  -U URL, --url URL     App server Url
  -m, --use_max_connection
                        Flag indicates using max connection or not. Set true
                        to apply 1.5x on normal connections
  -g {tiny,small,big}, --group_type {tiny,small,big}
                        Group type, choose from <tiny>|<small>|<big>
  -ms MESSAGE_SIZE, --message_size MESSAGE_SIZE
                        Message size
  -M MODULE, --module MODULE
                        Plugin name
  -s SETTINGS, --settings SETTINGS
                        Settings from different unit
  -d DURATION, --duration DURATION
                        Duration to run (second)
  -i INTERVAL, --interval INTERVAL
                        Interval for message sending
  -so STATISTICS_OUTPUT_PATH, --statistics_output_path STATISTICS_OUTPUT_PATH
                        Path to counters which record the statistics while
                        running benchmark
  -si STATISTIC_INTERVAL, --statistic_interval STATISTIC_INTERVAL
                        Interval for collecting intervals
  -w WAIT_TIME, --wait_time WAIT_TIME
                        Waiting time for each epoch
  -c CONFIG_SAVE_PATH, --config_save_path CONFIG_SAVE_PATH
                        Path of output benchmark configuration

```

#### Settings

The `settings` is a `yaml` file, there is an example shown as below.


##### unit map
SignalR service unit map to list index. For example, unit 5 map to index 2, then for scenario "scenario:echo,transport:Websocket", the noral connection is 3000, which is in the index 2. 

##### group count
Group count for different group type.

##### scenario
All parameters that is independent to unit is defined in key.
For "echo", "broadcast", the key is in format of: `scenario:<SCENARIO>,transport:<TRANSPORT>`  
For "send to client", the key is in format of: `scenario:<SCENARIO>,transport:<TRANSPORT>,message_size:<MESSAGE_SIZE>`
For "send to group" and "frequently join/leave group", the key is in format of: `scenario:<SCENARIO>,transport:<TRANSPORT>:group:<GROUP_TYPE>`

```

unit_map:
- 1
- 2
- 5
- 10

group_count:
  tiny: 10000
  small: 1000
  big: 10
 
scenario:echo,transport:Websockets:
  normal_connection: [1000, 2000, 3000, 4000]
  max_connection: [1500, 2500, 3500, 4500]
  base_step: [3000,3000,3000,3000]
  step: [1000,2000,1000,4000]
  step_length: [1,2,3,4]
  concurrent: [100,100,100,100]


scenario:broadcast,transport:Websockets:
  normal_connection: [1000, 2000, 3000, 4000]
  max_connection: [1500, 2500, 3500, 4500]
  base_step: [1,2,2,4]
  step: [1,2,5,4]
  step_length: [1,2,3,4]
  concurrent: [100,100,100,100]

scenario:sendToClient,transport:Websockets,protocol:json,message_size:2048:
  normal_connection: [1000, 2000, 3000, 4000]
  max_connection: [1500, 2500, 3500, 4500]
  base_step: [3000,3000,1000,3000]
  step: [1000,2000,3000,4000]
  step_length: [1,2,3,4]
  concurrent: [100,100,100,100]

scenario:frequentJoinLeaveGroup,transport:Websockets,group:tiny:
  normal_connection: [1000, 2000, 3000, 4000]
  max_connection: [1500, 2500, 10000, 4500]
  base_step: [3000,3000,10000,3000]
  step: [1000,2000,10000,4000]
  step_length: [1,2,3,4]
  concurrent: [100,100,100,100]

scenario:sendToGroup,transport:Websockets,group:small:
  normal_connection: [1000, 2000, 3000, 4000]
  max_connection: [1500, 2500, 10000, 4500]
  base_step: [3000,3000,10000,3000]
  step: [1000,2000,10000,4000]
  step_length: [1,2,3,4]
  concurrent: [100,100,100,100]

```


## Get configuration information

`get_sending_connection.py`

Get sending connections list for epoches.

### Arguments
```
usage: get_sending_connection.py [-h] -u UNIT -S
                   {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}
                   -p {json,messagepack} -t
                   {Websockets,Longpolling,ServerSentEvent} [-ms MESSAGE_SIZE]
                   [-s SETTINGS]

optional arguments:
  -h, --help            show this help message and exit
  -u UNIT, --unit UNIT  Azure SignalR service unit.
  -S {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}, --scenario {echo,broadcast,sendToClient,sendToGroup,frequentJoinLeaveGroup}
                        Scenario, choose from <echo>|<broadcast>|<sendToClient
                        >|<sendToGroup>|<frequentJoinLeaveGroup>
  -p {json,messagepack}, --protocol {json,messagepack}
                        SignalR Hub protocol, choose from <json>|<messagepack>
  -t {Websockets,Longpolling,ServerSentEvent}, --transport {Websockets,Longpolling,ServerSentEvent}
                        SignalR connection transport type, choose from:
                        <Websockets>|<Longpolling>|<ServerSentEvent>
  -ms MESSAGE_SIZE, --message_size MESSAGE_SIZE
                        Message size
  -s SETTINGS, --settings SETTINGS
                        Settings from different unit

```