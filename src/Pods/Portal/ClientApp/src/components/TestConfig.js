import React, { Component, useEffect, useState } from 'react';
//import { Modal, Button } from 'antd';
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'

export class TestConfig extends Component {
    constructor(props) {
        super(props);
        this.state = {
            show: false, loading: true, obj: { serverNum: 1, signalRUnitSize:1 }
        };
        this.handleClose = this.handleClose.bind(this);
        this.handleShow = this.handleShow.bind(this);
        this.handleChange = this.handleChange.bind(this);
        this.handleChangeNum = this.handleChangeNum.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
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
        this.state.obj[e.target.name] = e.target.value
        console.log(JSON.stringify(this.state.obj))
    }
    handleChangeNum(e) {
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
                        <th>TestId</th>
                        <th>SignalRUnitSize</th>
                        <th>ClientConnections</th>
                        <th>ServerNum</th>
                        <th>Start</th>
                    </tr>
                </thead>
                <tbody>
                    {testConfigs.map(testConfig => {
                        var json=JSON.stringify(testConfig)
                        return <tr key={testConfig.rowKey}>
                            <td>{testConfig.rowKey}</td>
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
                        <Modal.Title>Modal heading</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <Form >
                            <Form.Group >
                                <Form.Label >TestName</Form.Label>
                                <Form.Control name="rowKey" onChange={this.handleChange} placeholder="give a unique name for this test" />
                            </Form.Group>
                            <Form.Group  >
                                <Form.Label>Signarl unit size</Form.Label>
                                <Form.Control name="signalRUnitSize" onChange={this.handleChangeNum} as="select">
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
                                <Form.Control name="clientCons" onChange={this.handleChangeNum} placeholder="set the test server number" />
                            </Form.Group>
                            <Form.Group >
                                <Form.Label>Server number</Form.Label>
                                <Form.Control name="serverNum" onChange={this.handleChangeNum} placeholder="set the test server number" />
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