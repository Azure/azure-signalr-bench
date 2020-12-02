import React, { Component, useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import ReactJson from 'react-json-view'
import { Util } from './Util'

import { Search, Grid, Header, Segment, Divider, Button, Icon } from 'semantic-ui-react'


export class TestConfig extends Component {
    constructor(props) {
        super(props);
        this.state = {
            show: false, loading: true, obj: { signalRUnitSize: 1 },
            showjson: false,
            json: {},
            testConfigs: [],
            total: []
        };
        this.handleClose = this.handleClose.bind(this);
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
    handleSearchChange(e, data) {
        if (data.value != undefined && data.value.trim()) {
            var testConfigs = this.state.total.filter(x => x.rowKey.includes(data.value.trim()))
            this.setState({ testConfigs: testConfigs })
        }
        else
            this.setState({ testConfig: this.state.total })
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
            json: content
        })
    }
    handleClose() {
        this.setState({
            show: false
        })
    }
    handleShow() {
        this.setState({
            show: true
        })
    }
    handleChange(e) {
        if (e.target.name == "connectionString") {
            if (e.target.value) {
                this.unitRef.current.disabled = true
            }
            else
                this.unitRef.current.disabled = false
        }
        if (e.target.value == null || e.target.value == "") {
            delete this.state.obj[e.target.name]
        }
        else
            this.state.obj[e.target.name] = e.target.value.trim()
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
        const response = await fetch('testconfig/starttest/'+key, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
            },
            body: { key: key },
            redirect: 'manual'
        })
        await Util.CheckAuth(response)
        window.open("/test-status/" + key)
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
        const testName=this.state.obj["rowKey"]
        if(!(testName.match("[a-z0-9]([-a-z0-9]*[a-z0-9])?"))){
            alert("invalid testName. Should be of format [a-z0-9]([-a-z0-9]*[a-z0-9])")
            return
        }
        const response = await fetch('testconfig', {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(this.state.obj),
            redirect: 'manual'
        });
        await Util.CheckAuth(response)
        this.state.show = false;
        await this.populateTestConfigData();
    }

    componentDidMount() {
        this.populateTestConfigData();
    }

    renderTestConfigsTable(testConfigs) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel" >
                <thead>
                    <tr>
                        <th>TestName</th>
                        <th>timestamp</th>
                        <th>ClientConnections</th>
                        <th>Creater</th>
                        <th>Config</th>
                        <th>Start</th>
                        <th>Remove</th>

                    </tr>
                </thead>
                <tbody>
                    {testConfigs.map(testConfig => {
                        var json = JSON.stringify(testConfig)
                        var link = "/test-status/" + testConfig.rowKey;
                        return <tr key={testConfig.rowKey}>
                            <td><a href={link}>{testConfig.rowKey}</a></td>
                            <td>{testConfig.timestamp}</td>
                            <td>{testConfig.clientCons}</td>
                            <td>{testConfig.user}</td>
                            <td><Icon size="large" name='file code outline' value={json} onClick={this.handleJsonShow} /></td>
                            <td ><Button color="teal" size='mini' value={testConfig["partitionKey"]} onClick={this.handleStart}>Run</Button></td>
                            <td ><Button color="orange" size='mini' value={testConfig["partitionKey"]} onClick={this.handleDelete}>Delete</Button></td>
                        </tr>
                    }
                    )}
                </tbody>
            </table>
        );
    }

    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestConfigsTable(this.state.testConfigs);
        return (
            <>
                <Modal show={this.state.showjson} size="lg" onHide={this.handleJsonClose}>
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
                                <Form.Control name="rowKey" onChange={this.handleChange} placeholder="give a unique name for this test" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label >ConnectionString</Form.Label>
                                <Form.Control name="connectionString" onChange={this.handleChange} placeholder="ASR Connection String. If set, the below one will be ignored." />
                            </Form.Group>
                            <Form.Group  >
                                <Form.Label>Signarl unit size</Form.Label>
                                <Form.Control ref={this.unitRef} name="signalRUnitSize" onChange={this.handleChangeNum} as="select">
                                    <option>1</option>
                                    <option>2</option>
                                    <option>5</option>
                                    <option>10</option>
                                    <option>20</option>
                                    <option>50</option>
                                    <option>100</option>
                                </Form.Control>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Total client connections</Form.Label>
                                <Form.Control name="clientCons" onChange={this.handleChangeNum} placeholder="set the Total Client connections. (Default:3000)" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Server number</Form.Label>
                                <Form.Control name="serverNum" onChange={this.handleChangeNum} placeholder="set the test server number. (Default:1)" />
                            </Form.Group>
                            <Form.Group  >
                                <Form.Label>Testing Scenerio</Form.Label>
                                <Form.Control name="Scenario" onChange={this.handleChange} as="select">
                                    <option>Echo</option>
                                    <option>Broadcast</option>
                                </Form.Control>
                            </Form.Group>
                            <Form.Group  >
                                <Form.Label>Protocol</Form.Label>
                                <Form.Control name="Protocol" onChange={this.handleChange} as="select">
                                    <option>WebSocketsWithJson</option>
                                    <option>WebSocketsWithMessagePack</option>
                                    <option>ServerSideEventsWithJson</option>
                                    <option>LongPollingWithMessagePack</option>
                                    <option>LongPollingWithJson</option>
                                </Form.Control>
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Durations</Form.Label>
                                <Form.Control name="RoundDurations" onChange={this.handleChangeNum} placeholder="Time each round takes. (60)[Unit: s]" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Start Index</Form.Label>
                                <Form.Control name="Start" onChange={this.handleChangeNum} placeholder="Number of connections sending requests at first round. (1)" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Step Size </Form.Label>
                                <Form.Control name="Step" onChange={this.handleChangeNum} placeholder="set the step between rounds. (Default:5) " />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round End Index</Form.Label>
                                <Form.Control name="End" onChange={this.handleChangeNum} placeholder="Number of connections sending requests at first round. (Start)" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>MessageSize </Form.Label>
                                <Form.Control name="MessageSize" onChange={this.handleChangeNum} placeholder="set the message size. (Default:2048) [unit KB]) " />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Sending Interval </Form.Label>
                                <Form.Control name="Interval" onChange={this.handleChangeNum} placeholder="message sending interval  (Default:1000) [unit ms]) " />
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
    }


}