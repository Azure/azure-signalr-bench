import React, { Component, useEffect, useState } from 'react';
import Modal from 'react-bootstrap/Modal'
import { Util } from './Util'
export class TestStatus extends Component {
    constructor(props) {
        super(props);
        this.state = {
            loading: true,
            show: false,
            // report:[],
            errorShow: false,
            error: "",
            currentTestStatus: {}
        };
        this.report = this.report.bind(this)
        this.errorInfo = this.errorInfo.bind(this)
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
    async errorInfo(e) {
        var error = e.target.getAttribute("value")
        this.setState({ errorShow: true, error: error })
    }

    renderTestStatusTable(testStatuses) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestId</th>
                        <th>Index</th>
                        <th>Time</th>
                        <th>Creater</th>
                        <th>Status</th>
                        <th>Report</th>
                    </tr>
                </thead>
                <tbody>
                    {testStatuses.map(testStatus => {
                        var trkey = testStatus.partitionKey + testStatus.rowKey;
                        var colorstyle = testStatus.healthy ? "green" : "red";
                        var clz = "ui disabled mini button"
                        var data = JSON.stringify(testStatus)
                        var cb = this.report
                        if (!testStatus.healthy) {
                            clz = "ui red mini button"
                            data = testStatus.errorInfo
                            cb = this.errorInfo
                        } else if (testStatus.report) {
                            clz = "ui teal mini button"
                        }
                        return <tr key={trkey}>
                            <td>{testStatus.partitionKey}</td>
                            <td>{testStatus.rowKey}</td>
                            <td>{testStatus.timestamp}</td>
                            <td>{testStatus.user}</td>
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
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : this.renderTestStatusTable(this.state.testStatuses);
        const state = this.state.currentTestStatus;
        console.log("state is:")
        console.log(state)
        var report = [];
        var config = {};
        var label = "";
        if (state['report'] != undefined) {
            report = JSON.parse(state['report'])
        }
        if (state['config'] != undefined) {
            config = JSON.parse(state['config'])
            var totalCon = config['ClientCons'];
            var protocal = config['Protocol'];
            var scenario = config['Scenario'];
            var groupSize = config['GroupSize'];
            label = "{ Total connection:" + totalCon + ", Protocal: " + protocal + ", Scenario:" + scenario;
            if (scenario == "GroupBroadcast") {
                label += " [Size:" + groupSize + "]"
            }
            label += " }"
        }
        return (
            <>
                <div>
                    <h1 id="tabelLabel" >Test Jobs</h1>
                    {contents}
                </div>

                <Modal show={this.state.show} dialogClassName="modalCss" onHide={() => this.setState({ show: false })}>
                    <Modal.Header closeButton>
                        {/* <Modal.Title>Test Report </Modal.Title> */}
                    </Modal.Header>
                    <Modal.Body  >
                        <h4 class="configCss"> {label}</h4>
                        <table className='table table-striped' aria-labelledby="tabelLabel">
                            <thead>
                                <tr>
                                    <th>Round</th>
                                    <th>Connected</th>
                                    <th>Active</th>
                                    <th>Send</th>
                                    <th>Receive</th>
                                    <th>Reconnected</th>
                                    <th>Reconnecting</th>
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
                                    report.map((v, i) => {
                                        return <tr key={i}>
                                            <td>{i + 1}</td>
                                            <td>{v.ConnectedCount}/{v.RoundConnected}</td>
                                            <th>{v.ActiveConnection}</th>
                                            <td>{v.MessageSent}</td>
                                            <td>{v.MessageRecieved}/{v.ExpectedRecievedMessageCount}</td>
                                            <td>{v.TotalReconnectCount}</td>
                                            <td>{v.ReconnectingCount}</td>
                                            <td>{(parseFloat(v.Latency.LessThan50ms / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan100ms / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan200ms / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan500ms / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan1s / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan2s / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.LessThan5s / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
                                            <td>{(parseFloat(v.Latency.MoreThan5s / v.MessageRecieved * 100).toFixed(2) + "%")}</td>
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
                <Modal show={this.state.errorShow} size="lg" onHide={() => this.setState({ errorShow: false })}>
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
        var dir = testStatus.props.match.params.dir;
        var url;
        if (dir === undefined) {
            var key = testStatus.props.match.params.key;
            if (key === undefined)
                key = "";
            url = 'teststatus/list/' + key;
        } else {
            var index = testStatus.props.match.params.index;
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