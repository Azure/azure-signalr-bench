﻿import React, { Component, useEffect, useState } from 'react';
//import { Modal, Button } from 'antd';
//import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import { Search, Grid, Header, Segment, Divider,Button,Icon } from 'semantic-ui-react'
import {Util} from './Util'

export class TestStatus extends Component {
    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            show: false,
            report:[],
            errorShow:false,
            error:""
        };
       this.report= this.report.bind(this)
       this.errorInfo=this.errorInfo.bind(this)
    }


    componentDidMount() {
        this.populateTestStatusData(this);
        setInterval(()=>this.populateTestStatusData(this),5000)
    }

    async report(e) {
        console.log("report")
        var json= e.target.getAttribute("value")
        this.setState({error:true,report:JSON.parse(json)})
     }
     async errorInfo(e) {
        console.log("error")
        var error= e.target.getAttribute("value")
        this.setState({errorShow:true,error:error})
     }

     renderTestStatusTable(testStatuses) {
        return (
            <table className='table table-striped'  aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestId</th>
                        <th>Index</th>
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
                      var clz="ui disabled button"
                      var data=testStatus.report
                      var cb=this.report
                      if(!testStatus.healthy){
                          clz="ui red button"
                          data=testStatus.errorInfo
                          cb=this.errorInfo
                      }else if(testStatus.report){
                          clz="ui teal button"
                      }
                        return <tr key={trkey}>
                            <td>{testStatus.partitionKey}</td>
                            <td>{testStatus.rowKey}</td>
                            <td>{testStatus.timestamp}</td>
                            <td ><font color={colorstyle}>{testStatus.status}</font></td>
                            <td ><button className={clz} value={data} onClick={cb}>Report</button></td>
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
                <Modal show={this.state.errorShow} size="lg" onHide={()=>this.setState({errorShow:false})}>
                    <Modal.Header closeButton>
                        <Modal.Title>Test Report</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        {this.state.error}
                    </Modal.Body>
                    <Modal.Footer>
                    </Modal.Footer>
                </Modal>
                
            </>
        );
    }

    async populateTestStatusData(testStatus) {
        var key=testStatus.props.match.params.key;
        if(key===undefined)
             key="";
        const response = await fetch('teststatus/list/'+key,{
            redirect:'manual'
        });
       await Util.CheckAuth(response)
        const data = await response.json();
        testStatus.setState({ testStatuses: data, loading: false });
    }


}