import React, { Component } from 'react';
import Modal from 'react-bootstrap/Modal'
import ReactJson from 'react-json-view'
import { TestReport } from './TestReport'
import { Util } from './Util'
import { Button, Icon } from 'semantic-ui-react'

export class TestStatus extends Component {
    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            show: false,
            showjson:false,
            currentTestStatus: {}
        };
        this.report = this.report.bind(this)
        this.cancel = this.cancel.bind(this)
    }


    componentDidMount() {
        this.populateTestStatusData(this);
        setInterval(() => this.populateTestStatusData(this), 5000)
    }

    async report(e) {
        console.log("report")
        var json = JSON.parse(e.target.getAttribute("value"))
        this.setState({ show: true, currentTestStatus: json })
    }

    async cancel(e) {
        console.log("report")

        e.persist()
        e.target.setAttribute("class", "ui orange loading mini button")
        var json = JSON.parse(e.target.getAttribute("value"))
        var key = json.testId
        var queueName = json.queueName
        const response = await fetch("teststatus/cancel/" + key+"/" + queueName, {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
            },
            body: { key: key },
            redirect: 'manual'
        })
        if (response.type === 'opaqueredirect') {
            alert("No permission")
            e.target.setAttribute("class", "ui orange mini button")
            return
        }
        await Util.CheckAuth(response)
        e.target.setAttribute("class", "ui orange mini button")
    }
    handleJsonShow=(e)=> {
        var content = JSON.parse(e.target.getAttribute("value"))
        console.log(content)
        delete content["ETag"]
        content["TestName"] = content["RowKey"]
        delete content["RowKey"]
        delete content["PartitionKey"]
        if (content["ConnectionString"])
            delete content["SignalRUnitSize"]
        console.log(content)
        this.setState({
            showjson: true,
            json: content,
        })
    }
    handleJsonClose=()=> {
        this.setState({
            showjson: false
        })
    }
    renderTestStatusTable(testStatuses) {
        return (
            <div>
                 <Modal show={this.state.showjson} dialogClassName="modalCss" onHide={this.handleJsonClose}>
                    <Modal.Header closeButton>
                        <Modal.Title>Config details</Modal.Title>
                    </Modal.Header>
                    <ReactJson src={this.state.json} displayDataTypes={false} sortKeys={true} name={false} />
                </Modal>
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestName</th>
                        <th>Index</th>
                        <th>Time</th>
                        <th>Creater</th>
                        <th>Config</th>
                        <th>Status</th>
                        <th>Report</th>
                        <th>Cancel</th>
                        <th>Links</th>
                    </tr>
                </thead>
                <tbody>
                    {testStatuses.map(testStatus => {
                        var trkey = testStatus.partitionKey + testStatus.rowKey;
                        var colorstyle = testStatus.healthy ? "green" : "red";
                        var clz = "ui disabled mini button"
                        var data = JSON.stringify(testStatus)
                        if (!testStatus.healthy) {
                            clz = "ui red mini button"
                        } else if (testStatus.report) {
                            if (!testStatus.check || testStatus.check == "pass")
                                clz = "ui teal mini button"
                            else
                                clz = "ui yellow mini button"
                        }
                        return <tr key={trkey}>
                            <td>{testStatus.partitionKey}</td>
                            <td>{testStatus.rowKey}</td>
                            <td>{testStatus.lastModified===null?testStatus.timestamp:testStatus.lastModified}</td>
                            <td>{testStatus.user}</td>
                            <td><Icon size="large" name='file code outline' value={testStatus.config}
                                      onClick={this.handleJsonShow}/></td>
                            <td><font color={colorstyle}>{testStatus.status}</font></td>
                            <td>
                                <button className={clz} value={data} onClick={this.report}>Report</button>
                            </td>
                            <td><Button color="orange" size='mini'
                                        value={data} onClick={this.cancel}>Cancel</Button></td>
                            <td>
                                <a href={testStatus.grafanaPath ? testStatus.grafanaPath : '#'}
                                   target="_blank"
                                   style={testStatus.grafanaPath ? {} : {
                                       color: 'gray',
                                       pointerEvents: 'none',
                                       textDecoration: 'none'
                                   }}>
                                    Metrics
                                </a>
                                &nbsp;&nbsp;
                                <a href={testStatus.k8sPath ? testStatus.k8sPath : '#'}
                                   target="_blank"
                                   style={testStatus.k8sPath ? {} : {
                                       color: 'gray',
                                       pointerEvents: 'none',
                                       textDecoration: 'none'
                                   }}>
                                    K8s
                                </a>

                            </td>
                        </tr>
                        }
                    )}
                </tbody>
            </table>
            </div>
        );
    }
    render() {
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestStatusTable(this.state.testStatuses);
        return (
            <>
                <div>
                    <h1 id="tabelLabel" >Test Jobs</h1>
                    {contents}
                </div>

                <Modal show={this.state.show} dialogClassName="modalCss" onHide={() => this.setState({ show: false })}>
                    <Modal.Header closeButton>
                        <Modal.Title>Test Report: {this.state.currentTestStatus.testId}</Modal.Title>
                    </Modal.Header>
                    <Modal.Body>
                        <TestReport testStatus={this.state.currentTestStatus} />
                    </Modal.Body>
                    <Modal.Footer>
                    </Modal.Footer>


                </Modal>
            </>
        );
    }

    async populateTestStatusData(testStatus) {
        var dir = testStatus.props.match.params.dir;
        var index = testStatus.props.match.params.index;
        var url;
        if (dir === undefined) {
            var key = testStatus.props.match.params.key;
            url='teststatus/list/'
            if (key !== undefined){
                url +=key;
                if(index !==undefined){
                    url+= '/'+ index;
                }
            }
        } else {
            url = 'teststatus/dir/list/' + dir + "?index=" + index;
        }
        console.log(url)
        const response = await fetch(url, {
            redirect: 'manual'
        });
        await Util.CheckAuth(response)
        const data = await response.json();
        testStatus.setState({ testStatuses: data, loading: false });
    }


}