import React, { Component, useEffect, useState } from 'react';
//import { Modal, Button } from 'antd';
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'

export class TestStatus extends Component {
    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            show: false,
            report:[]
        };
       this.report= this.report.bind(this)
    }


    componentDidMount() {
        this.populateTestStatusData();
    }

    async report(e) {
        console.log("report")
        var json= e.target.getAttribute("value")
        this.setState({show:true,report:JSON.parse(json)})
     }

     renderTestStatusTable(testStatuses) {
        return (
            <table className='table table-striped'  aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestId</th>
                        <th>Round</th>
                        <th>Time</th>
                        <th>Status</th>
                        <th>Report</th>
                    </tr>
                </thead>
                <tbody>
                    {testStatuses.map(testStatus => {
                        var trkey = testStatus.partitionKey + testStatus.rowKey;
                      //  console.log()
                      var colorstyle=testStatus.healthy?"green":"red";
                        return <tr key={trkey}>
                            <td>{testStatus.partitionKey}</td>
                            <td>{testStatus.rowKey}</td>
                            <td>{testStatus.timestamp}</td>
                            <td ><font color={colorstyle}>{testStatus.status}</font></td>
                            <td ><button className="link" value={testStatus.report} onClick={this.report}>Report</button></td>
                        </tr>
                    }

                    )}
                </tbody>
            </table >
        );
    }
    render() {
        console.log(this.state.report)
        console.log("render")
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestStatusTable(this.state.testStatuses);
        return (
            <>
                <div>
                    <h1 id="tabelLabel" >Test Jobs</h1>
                    {contents}
                </div>
                <Modal show={this.state.show} size="lg" onHide={()=>this.setState({show:false})}>
                    <Modal.Header closeButton>
                        <Modal.Title>Test Report</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                    <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>Round</th>
                        <th>0-50ms</th>
                        <th>50-100ms</th>
                        <th>100-200ms</th>
                        <th>200-500ms</th>
                        <th>500-1000ms</th>
                        <th>1-2s</th>
                        <th>2-5s</th>
                        <th>5+s</th>
                    </tr>
                </thead>
                <tbody>
                    {
                        this.state.report.map((v,i)=>{
                            return <tr key={i}> 
                            <td>{i}</td>
                            <td>{(parseFloat(v.Latency.LessThan50ms/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan100ms/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan200ms/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan500ms/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan1s/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan2s/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.LessThan5s/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            <td>{(parseFloat(v.Latency.MoreThan5s/v.MessageRecieved*100).toFixed(2)+"%")}</td>
                            </tr>
                        }
                        )
                    }
                </tbody>
            </table >
                    </Modal.Body>
                    <Modal.Footer>
                    </Modal.Footer>
                </Modal>

                
            </>
        );
    }

    async populateTestStatusData() {
        const response = await fetch('teststatus');
        const data = await response.json();
        this.setState({ testStatuses: data, loading: false });
    }


}