import React, { Component, useEffect, useState } from 'react';
//import { Modal, Button } from 'antd';
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'

export class TestConfig extends Component {
    constructor(props) {
        super(props);
        this.state = {
            show: false, loading: true, obj: {  signalRUnitSize:1 }
        };
        this.handleClose = this.handleClose.bind(this);
        this.handleShow = this.handleShow.bind(this);
        this.handleChange = this.handleChange.bind(this);
        this.handleChangeNum = this.handleChangeNum.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
        this.unitRef=React.createRef();
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
        if(e.target.name=="connectionString"){
            console.log("disable")
            console.log(this)
            if(e.target.value){
                 this.unitRef.current.disabled=true
            }
            else
                 this.unitRef.current.disabled=false
        }
        if(e.target.value==null||e.target.value==""){
            delete this.state.obj[e.target.name]
        }
        else
        this.state.obj[e.target.name] = e.target.value
        console.log(JSON.stringify(this.state.obj))
    }
    handleChangeNum(e) {
        if(e.target.value==null||e.target.value==""){
            delete this.state.obj[e.target.name]
        }
        else
          this.state.obj[e.target.name] = parseInt(e.target.value);
        console.log(JSON.stringify(this.state.obj))
    }

    async handleStart(e) {
       var json= e.target.getAttribute("value")
        console.log(json)
        await fetch('testconfig/starttest', {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
            },
            body: json
        }).then(res=>{
            console.log(res)
            window.open("/test-status/"+json["partitionKey"])
        });
    }
    async handleSubmit() {
        await fetch('testconfig', {
            method: 'PUT',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(this.state.obj)
        });
        this.state.show = false;
        await this.populateTestConfigData();
    }

    componentDidMount() {
        this.populateTestConfigData();
    }

     renderTestConfigsTable(testConfigs) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestName</th>
                        <th>SignalRUnitSize</th>
                        <th>ClientConnections</th>
                        <th>ServerNum</th>
                        <th>Start</th>
                    </tr>
                </thead>
                <tbody>
                    {testConfigs.map(testConfig => {
                        var json=JSON.stringify(testConfig)
                        var link="/test-status/"+testConfig.rowKey;
                        return <tr key={testConfig.rowKey}>
                            <td><a href={link}>{testConfig.rowKey}</a></td>
                            <td>{testConfig.signalRUnitSize}</td>
                            <td>{testConfig.clientCons}</td>
                            <td>{testConfig.serverNum}</td>
                            <td ><button className="link" value={json} onClick={this.handleStart}>Run</button></td>
                        </tr>
                    }

                    )}
                </tbody>
            </table>
        );
    }
    render() {
        console.log("render")
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestConfigsTable(this.state.testConfigs);
        return (
            <>
                <Button variant="primary" onClick={this.handleShow}>
                    Create Test Job
      </Button>

                <Modal show={this.state.show} onHide={this.handleClose}>
                    <Modal.Header closeButton>
                        <Modal.Title>Create a test job config</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <Form  name="CreateConfigForm">
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
                                <Form.Control name="RoundDuration" onChange={this.handleChangeNum} placeholder="Time each round takes. (60)[Unit: s]" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Round Start Index</Form.Label>
                                <Form.Control name="Start" onChange={this.handleChangeNum} placeholder="Number of connections sending requests at first round. (0)" />
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
                    {contents}
                </div>
            </>
        );
    }

    async populateTestConfigData() {
        const response = await fetch('testconfig');
        const data = await response.json();
        this.setState({ testConfigs: data, loading: false });
    }


}