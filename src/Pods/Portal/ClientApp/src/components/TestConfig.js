import React, { Component, useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import ReactJson from 'react-json-view'
import { Util } from './Util'

import { Search, Grid, Header, Segment, Divider, Button, Icon, Accordion, Dropdown } from 'semantic-ui-react'
import Terminal from 'terminal-in-react';


export class TestConfig extends Component {
    constructor(props) {
        super(props);
        this.defaultObj={ signalRUnitSize: 1, mode: "Default", service: "SignalR", scenario: "Echo", framework: "Netcore", env: "AzureGlobal", createMode: "ConnectionString" };
        this.state = {
            show: false, loading: true, obj:JSON.parse(JSON.stringify(this.defaultObj)),
            showjson: false,
            json: {},
            testConfigs: [],
            total: [],
            activeIndex: { "Default": true },
            search:"",
            edit:false
        };
        this.handleClose = this.handleClose.bind(this);
        this.handleEdit = this.handleEdit.bind(this);
        this.handleFork = this.handleFork.bind(this);
        this.handleShow = this.handleShow.bind(this);
        this.handleJsonClose = this.handleJsonClose.bind(this);
        this.handleJsonShow = this.handleJsonShow.bind(this);
        this.handleChange = this.handleChange.bind(this);
        this.handleChangeNum = this.handleChangeNum.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
        this.handleSearchChange = this.handleSearchChange.bind(this);
        this.handleDelete = this.handleDelete.bind(this);
        this.unitRef = React.createRef();
    }

    handleDirClick = (e, titleProps) => {
        const { index } = titleProps
        const activeIndex = this.state.activeIndex
        if (activeIndex[index]) {
            activeIndex[index] = false
        } else {
            activeIndex[index] = true
        }
        this.setState({ activeIndex: activeIndex })
    }

    handleSearchChange(e, data) {
        this.state.search=data
        if (data.value != undefined && data.value.trim()) {
            var acDir = {}
            var testConfigs = this.state.total.filter(x => x.rowKey.includes(data.value.trim()))
            testConfigs.forEach(t => acDir[t.dir] = true)
            this.setState({ testConfigs: testConfigs, activeIndex: acDir })
        }
        else {
           // var acDir = { "Default": true }
            this.setState({ testConfigs: this.state.total })
        }
    }
    handleJsonClose() {
        this.setState({
            showjson: false
        })
    }
    handleJsonShow(e) {
        var content = JSON.parse(e.target.getAttribute("value"))
        delete content["eTag"]
        content["TestName"] = content["rowKey"]
        delete content["rowKey"]
        delete content["partitionKey"]
        if (content["connectionString"])
            delete content["signalRUnitSize"]
        this.setState({
            showjson: true,
            json: content,
        })
    }
    handleClose() {
        this.setState({
            show: false
        })
    }
    handleFork(e) {
        var content = JSON.parse(e.target.getAttribute("value"))
        delete content["clientNum"]
        delete content["serverNum"]
        this.setState({
            show: true,
            obj:content,
            edit:false,
        })
    }

    handleEdit(e) {
        var content = JSON.parse(e.target.getAttribute("value"))
        this.setState({
            show: true,
            obj:content,
            edit:true
        })
    }
    
    handleShow() {
        console.log(this.defaultObj)
        this.setState({
            show: true,
            edit:false,
            obj:JSON.parse(JSON.stringify(this.defaultObj))
        })
    }
    handleChange(e) {
        if (e.target.name == "connectionString") {
            if (e.target.value) {
                this.unitRef.current && (this.unitRef.current.disabled = true)
            }
            else
                this.unitRef.current && (this.unitRef.current.disabled = false)
        }
        if (e.target.getAttribute("type") == "select") {
            console.log("type is select")
            var obj = this.state.obj;
            obj[e.target.name] = e.target.value;
            this.setState({ obj: obj })
            return
        }
        if (e.target.value == null || e.target.value == "") {
            delete this.state.obj[e.target.name]
        }
        else {
            var value = e.target.value.replaceAll("\"", "").trim();
            this.state.obj[e.target.name] = value
        }
    }
    handleChangeNum(e) {
        if (e.target.value == null || e.target.value == "") {
            delete this.state.obj[e.target.name]
        }
        else
            this.state.obj[e.target.name] = parseInt(e.target.value);
    }

    async handleStart(e) {
        e.persist()
        e.target.setAttribute("class", "ui teal loading mini button")
        var key = e.target.getAttribute("value")
        const response = await fetch('testconfig/starttest/' + key, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
            },
            body: { key: key },
            redirect: 'manual'
        })
        await Util.CheckAuth(response)
        window.open("/test-status/testname/" + key)
        e.target.setAttribute("class", "ui teal mini button")
    }
    async handleDelete(e) {
        e.persist()
        e.target.setAttribute("class", "ui orange loading mini button")
        var key = e.target.getAttribute("value")
        const response = await fetch('testconfig/' + key, {
            method: 'Delete',
            headers: {
                'Accept': 'application/json',
            },
            redirect: 'manual'
        })
        await Util.CheckAuth(response)
        await this.populateTestConfigData()
    }
    async handleSubmit() {
        console.log(this.state.obj)
        const testName = this.state.obj["rowKey"]
        this.state.obj.partitionKey=this.state.obj.rowKey
        if ((testName == undefined) || !(testName.match("^[a-z0-9]([-a-z0-9]*[a-z0-9])?$"))) {
            alert("invalid testName. Should be of format [a-z0-9]([-a-z0-9]*[a-z0-9]?)")
            return
        }
        if( testName.includes("--")){
            alert("-- is reserved!")
            return
        }
        const method=this.state.edit?"PATCH":"PUT"
        const response = await fetch('testconfig', {
            method: method,
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(this.state.obj),
            redirect: 'manual'
        });
        if (response.status != 200) {
            var result = await response.text()
            console.log(result)
            alert(result)
            return
        }
        await Util.CheckAuth(response)
        this.state.show = false;
        await this.populateTestConfigData();
    }

    componentDidMount() {
        this.populateTestConfigData(); 
    }

    renderTestConfigsTable(testConfigs) {
        var classify = {}, def = []
        for (var i in testConfigs) {
            if (testConfigs[i].dir == "Default") {
                def.push(testConfigs[i])
            } else {
                var dic = testConfigs[i].dir
                if (classify[dic] == undefined) {
                    classify[dic] = []
                }
                classify[dic].push(testConfigs[i])
            }
        }
        var list = []
        for (var key in classify) {
            list.push([key, classify[key]])
        }
        list.sort((a, b) => a[0] - b[0]);
        list.unshift(["Default", def])
        return (
            <div>

                <Accordion exclusive={false}>
                    <Accordion.Title
                        active={this.state.activeIndex['Terminal']}
                        index='Terminal'
                        onClick={this.handleDirClick}
                    >
                        <Icon name='dropdown' />
                            Terminal
                     </Accordion.Title>
                    <Accordion.Content active={this.state.activeIndex['Terminal']}>
                        <div class="terminal"
                        >
                            <Terminal
                                startState='maximised'
                                hideTopBar="true"
                                color='green'
                                backgroundColor='black'
                                barColor='black'
                                style={{ fontWeight: "bold", fontSize: "1em", marginBottom: 0 }}
                                commands={{
                                    move:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 3) {
                                                print("Usage: move {testName}  {dirName}")
                                                return
                                            }
                                            fetch(`testconfig/move/jobConfig/${args[1]}/${args[2]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                    rename:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 3) {
                                                print("Usage: rename {testName}  {newTestName}")
                                                return
                                            }
                                            fetch(`testconfig/rename/${args[1]}/${args[2]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                    movedir:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 3) {
                                                print("Usage: move {dirName}  {dirName}")
                                                return
                                            }
                                            fetch(`testconfig/move/dir/${args[1]}/${args[2]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                    cron:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 3) {
                                                print("Usage: cron {testName} {0_12_*_*_*}")
                                                return
                                            }
                                            fetch(`testconfig/cron/${args[1]}?cron=${args[2]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                 
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                    auth:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 3) {
                                                print("Usage: auth {user} {role}")
                                                return
                                            }
                                            fetch(`home/auth/${args[1]}?role=${args[2]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                response.text().then(data => alert(data))
                                            })
                                        },
                                    batch:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 4) {
                                                print("Usage: batch {testName} {group} {units[ex:1,2,5,100]}")
                                                return
                                            }
                                            fetch(`testconfig/batch/${args[1]}?dir=${args[2]}&units=${args[3]}`, {
                                                method: 'PUT',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                    var activeIndex=this.state.activeIndex
                                                    activeIndex["Default"]=false
                                                    activeIndex[args[2]]=true
                                                    this.setState({activeIndex:activeIndex})
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                        startdir:
                                        (args, print, runCommand) => {
                                            console.log(args)
                                            if (args.length != 5) {
                                                print("Usage: startdir {dir} {index} {unitLimit} {instanceLimit}")
                                                return
                                            }
                                            fetch(`testconfig/batch/starttest/${args[1]}?index=${args[2]}&unitLimit=${args[3]}&instanceLimit=${args[4]}`, {
                                                method: 'POST',
                                                redirect: 'manual'
                                            }).then(response => {
                                                if (response.status == 200) {
                                                    alert("Succeed")
                                                    this.populateTestConfigData()
                                                    var activeIndex=this.state.activeIndex
                                                    activeIndex["Default"]=false
                                                    activeIndex[args[2]]=true
                                                    this.setState({activeIndex:activeIndex})
                                                    window.open("/test-status/dir/" + args[1]+"/"+args[2])
                                                } else {
                                                    response.text().then(data => alert(data))
                                                }
                                            })
                                        },
                                }}
                                descriptions={{
                                    move: 'move {testName} {dirName}', movedir: 'movedir {dirName} {dirName}', cron: "run test periodically [Unix version crontab]. Usage: cron {testName} {0_12_*_*_*}",
                                    rename: 'rename {testName} {newTestName}',
                                    auth: 'auth {user} {role}. Generate a password for a user with that role',
                                    batch: 'batch {testName} {group} {units[ex:1,2,5,100]. Generate different config from a template for dif units',
                                    startdir: 'Usage: startdir {dir} {index} {unitLimit} {instanceLimit}. Start all tests in a dir with custom index.'

                                }}
                                msg='Type help to see all supported commands'
                            />
                        </div>
                    </Accordion.Content>
                    {list.map(pair => {
                        return <div>
                            <Accordion.Title
                                active={this.state.activeIndex[pair[0]]}
                                index={pair[0]}
                                onClick={this.handleDirClick}
                            >
                                <Icon name='dropdown' />
                                {pair[0]}
                            </Accordion.Title>
                            <Accordion.Content active={this.state.activeIndex[pair[0]]}>
                                <table className='table table-striped' aria-labelledby="tabelLabel" >
                                    <thead>
                                        <tr>
                                            <th>TestName</th>
                                            <th>timestamp</th>
                                            <th>ClientConnections</th>
                                            <th>Creater</th>
                                            <th>Config</th>
                                            <th>Edit</th>
                                            <th>Fork</th>
                                            <th>Start</th>
                                            <th>Remove</th>

                                        </tr>
                                    </thead>
                                    <tbody>
                                        {pair[1].map(testConfig => {
                                            var json = JSON.stringify(testConfig)
                                            var link = "/test-status/testname/" + testConfig.rowKey;
                                            return <tr key={testConfig.rowKey}>
                                                <td><a href={link}>{testConfig.rowKey}</a></td>
                                                <td>{testConfig.timestamp}</td>
                                                <td>{testConfig.clientCons}</td>
                                                <td>{testConfig.user}</td>
                                                <td><Icon size="large" name='file code outline' value={json} onClick={this.handleJsonShow} /></td>
                                                <td><Icon size="large" name='pencil alternate' value={json} onClick={this.handleEdit} /></td>
                                                <td><Icon size="large" name='gay' value={json} onClick={this.handleFork} /></td>
                                                <td ><Button color="teal" size='mini' value={testConfig["partitionKey"]} onClick={this.handleStart}>Run</Button></td>
                                                <td ><Button color="orange" size='mini' value={testConfig["partitionKey"]} onClick={this.handleDelete}>Delete</Button></td>
                                            </tr>
                                        }
                                        )}
                                    </tbody>
                                </table>
                            </Accordion.Content>
                        </div>
                    })}
                </Accordion>
            </div>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestConfigsTable(this.state.testConfigs);
        //remove the terminal blank. The autofocus is removed by https://github.com/nitin42/terminal-in-react/issues/59
        var terminal = document.querySelector('.terminal')
        terminal && terminal.children[0].classList.remove('gSZAyM')
        if(this.state.obj.createMode==null)
           this.state.obj.createMode=this.state.obj.serverUrl!=null?"SelfHostedServer":this.state.obj.connectionString!=null?"ConnectionString":"CreateByPerf";
        return (
            <>
                <Modal show={this.state.showjson} dialogClassName="modalCss" onHide={this.handleJsonClose}>
                    <Modal.Header closeButton>
                        <Modal.Title>Config details</Modal.Title>
                    </Modal.Header>
                    <ReactJson src={this.state.json} displayDataTypes={false} sortKeys={true} name={false} />
                </Modal>
                <Modal show={this.state.show} onHide={this.handleClose}>
                    <Modal.Header closeButton>
                        <Modal.Title>Create a test job config</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <Form name="CreateConfigForm">
                            <Form.Group >
                                <Form.Label >TestName</Form.Label>
                                <Form.Control disabled={this.state.edit} name="rowKey" onChange={this.handleChange} placeholder="give a unique name for this test" defaultValue={this.state.obj.rowKey} />
                            </Form.Group>
                            <Form.Group  >
                                <Form.Label>Service Name</Form.Label>
                                <Form.Control name="service" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.service}>
                                    <option>SignalR</option>
                                    <option>RawWebsocket</option>
                                </Form.Control>
                            </Form.Group>
                          
                            {this.state.obj.service == "SignalR" && <Form.Group  >
                                <Form.Label>Service Mode</Form.Label>
                                <Form.Control name="mode" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.mode}>
                                    <option>Default</option>
                                    <option>Serverless</option>
                                </Form.Control>
                            </Form.Group>}
                            {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && <Form.Group  >
                                <Form.Label>Framework</Form.Label>
                                <Form.Control name="framework" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.framework}>
                                    <option>Netcore</option>
                                    <option>Netframework</option>
                                </Form.Control>
                            </Form.Group>}
                            { (this.state.obj.service == "SignalR" && this.state.obj.mode == "Serverless" && this.state.obj.createMode=="ConnectionString") &&
                                <div>
                                    <strong>Add upstream settings: </strong>
                                    <code> https://{window.location.hostname}/upstream/{"{hub}"}/api/{"{category}"}/{"{event}"}</code>
                                </div>
                            }
                             {(this.state.obj.service == "RawWebsocket" ) &&
                                <div>
                                    <strong>Add upstream settings: </strong>
                                    <code> https://{window.location.hostname}/upstream/{"{event}"}</code>
                                </div>
                            }
                            {this.state.obj.service == "SignalR" && <Form.Group  >
                                <Form.Label>CreateMode</Form.Label>
                                <Form.Control name="createMode" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.createMode}>
                                    <option>ConnectionString</option>
                                    <option>CreateByPerf</option>
                                    <option>SelfHostedServer</option>
                                </Form.Control>
                            </Form.Group>}
                            {this.state.obj.createMode == "ConnectionString" && <Form.Group >
                                <Form.Label >ConnectionString</Form.Label>
                                <Form.Control name="connectionString" onChange={this.handleChange} placeholder="ASR Connection String." defaultValue={this.state.obj.connectionString} />
                            </Form.Group>}
                            {this.state.obj.service == "SignalR" && window.perfppe  && this.state.obj.createMode == "CreateByPerf"&& <Form.Group  >
                                <Form.Label>Environment</Form.Label>
                                <Form.Control name="env" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.env}>
                                    <option>AzureGlobal</option>
                                    <option>PPE</option>
                                </Form.Control>
                            </Form.Group>}
                            {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && this.state.obj.createMode == "CreateByPerf" && <Form.Group  >
                                <Form.Label>Signarl unit size</Form.Label>
                                <Form.Control ref={this.unitRef} name="signalRUnitSize" onChange={this.handleChangeNum} as="select" defaultValue={this.state.obj.signalRUnitSize}>
                                    <option>1</option>
                                    <option>2</option>
                                    <option>5</option>
                                    <option>10</option>
                                    <option>20</option>
                                    <option>50</option>
                                    <option>100</option>
                                </Form.Control>
                            </Form.Group>}
                            {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && this.state.obj.createMode == "CreateByPerf" && <Form.Group >
                                <Form.Label >Tags</Form.Label>
                                <Form.Control name="tags" onChange={this.handleChange} placeholder="key1=value1;key2=value2" defaultValue={this.state.obj.tags} />
                            </Form.Group>}
                            {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && this.state.obj.createMode == "SelfHostedServer" && <Form.Group >
                                <Form.Label >ServerlUrl</Form.Label>
                                <Form.Control name="serverUrl" onChange={this.handleChange} placeholder="http(s)://host:port"  defaultValue={this.state.obj.serverUrl}/>
                            </Form.Group>}
                            <Form.Group >
                                <Form.Label>Total client connections</Form.Label>
                                <Form.Control name="clientCons" onChange={this.handleChangeNum} placeholder="set the Total Client connections. (Default:1000)" defaultValue={this.state.obj.clientCons} />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Total client connections establish round num</Form.Label>
                                <Form.Control name="connectEstablishRoundNum" onChange={this.handleChangeNum} placeholder="Establish all connections gradually. (Default:1)" defaultValue={this.state.obj.connectEstablishRoundNum}/>
                            </Form.Group>
                             <Form.Group >
                                     <Form.Label>Client number</Form.Label>
                                 <Form.Control name="clientNum" onChange={this.handleChangeNum} placeholder="set the test client number. (Default:Total con/5000)" defaultValue={this.state.edit?this.state.obj.clientNum:""}/>
                             </Form.Group>
                            { this.state.obj.createMode != "SelfHostedServer" && <Form.Group >
                                     <Form.Label>Server number</Form.Label>
                                 <Form.Control name="serverNum" onChange={this.handleChangeNum} placeholder="set the test server number. (Default:ClientNum/2)" defaultValue={this.state.edit?this.state.obj.serverNum:""}/>
                             </Form.Group>}
                            <Form.Group  >
                                <Form.Label>Testing Scenario</Form.Label>
                                <Form.Control name="scenario" type="select" onChange={this.handleChange} as="select" defaultValue={this.state.obj.scenario}>
                                    <option>Echo</option>
                                    <option>Broadcast</option>
                                    <option>GroupBroadcast</option>
                                    <option>P2P</option>
                                </Form.Control>
                            </Form.Group>
                            {this.state.obj.scenario == "GroupBroadcast" && <Form.Group >
                                <Form.Label>GroupSize</Form.Label>
                                <Form.Control name="groupSize" onChange={this.handleChangeNum} placeholder="set the test server number. (Default:100)" defaultValue={this.state.obj.groupSize}/>
                            </Form.Group>}
                            <Form.Group  >
                                <Form.Label>Protocol</Form.Label>
                                <Form.Control name="protocol" onChange={this.handleChange} as="select" defaultValue={this.state.obj.protocol}>
                                    <option>WebSocketsWithJson</option>
                                    {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && this.state.obj.framework == "Netcore" && <option>WebSocketsWithMessagePack</option>}
                                    <option>ServerSideEventsWithJson</option>
                                    {this.state.obj.service == "SignalR" && this.state.obj.mode == "Default" && this.state.obj.framework == "Netcore" && <option>LongPollingWithMessagePack</option>}
                                    <option>LongPollingWithJson</option>
                                </Form.Control>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Connection Rate</Form.Label>
                                <Form.Control name="rate" onChange={this.handleChangeNum} placeholder="set the Connection Rate. (Default:200)" defaultValue={this.state.obj.rate}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Durations</Form.Label>
                                <Form.Control name="roundDurations" onChange={this.handleChangeNum} placeholder="Time each round takes. (60)[Unit: s]" defaultValue={this.state.obj.roundDurations}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Nums </Form.Label>
                                <Form.Control name="roundNum" onChange={this.handleChangeNum} placeholder="how many rounds to test. (Default:5) " defaultValue={this.state.obj.roundNum}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Start Index</Form.Label>
                                <Form.Control name="start" onChange={this.handleChangeNum} placeholder="Number of connections sending requests at first round. (1)" defaultValue={this.state.obj.start}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round End Index</Form.Label>
                                <Form.Control name="end" onChange={this.handleChangeNum} placeholder="Number of connections sending requests at last round. (Start)" defaultValue={this.state.obj.end}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>MessageSize </Form.Label>
                                <Form.Control name="messageSize" onChange={this.handleChangeNum} placeholder="set the message size. (Default:2048) [unit B]) " defaultValue={this.state.obj.messageSize}/>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Sending Interval </Form.Label>
                                <Form.Control name="interval" onChange={this.handleChangeNum} placeholder="message sending interval  (Default:1000) [unit ms]) " defaultValue={this.state.obj.interval} />
                            </Form.Group>
                        </Form>
                    </Modal.Body>
                    <Modal.Footer>
                        <Button variant="secondary" onClick={this.handleClose}>
                            Cancel
          </Button>
                        <Button variant="primary" onClick={this.handleSubmit}>
                            Submit
          </Button>
                    </Modal.Footer>
                </Modal>



                <div>
                    <h1 id="tabelLabel" >Test Job Configs</h1>
                    <Segment basic textAlign='center'>
                        <Button
                            color='teal'
                            content='Create New TestConfig'
                            icon='add'
                            labelPosition='left'
                            onClick={this.handleShow}
                        />
                        <Divider horizontal>Search by TestName</Divider>
                        <Grid.Column verticalAlign='middle'>
                            <Search
                                loading={false} icon='search'
                                onSearchChange={this.handleSearchChange}
                                showNoResults={false}
                            />
                        </Grid.Column>

                    </Segment>

                    {contents}
                </div>
            </>
        );
    }

    async populateTestConfigData() {
        const response = await fetch('testconfig', {
            redirect: "manual"
        });
        await Util.CheckAuth(response)
        const data = await response.json();
        this.setState({ testConfigs: data, loading: false, total: data });
        this.handleSearchChange(null,this.state.search)
    }


}