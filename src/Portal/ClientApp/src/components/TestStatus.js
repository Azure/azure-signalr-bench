import React, { Component, useEffect, useState } from 'react';
//import { Modal, Button } from 'antd';
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'

export class TestStatus extends Component {
    constructor(props) {
        super(props);
        this.state = {
            loading: true
        };
       // this.populateTestsData.bind(this)
    }
 

    componentDidMount() {
        this.populateTestStatusData();
    }

    static renderTestStatusTable(testStatuses) {
        return (
            <table className='table table-striped' aria-labelledby="tabelLabel">
                <thead>
                    <tr>
                        <th>TestId</th>
                        <th>Time</th>
                        <th>Status</th>

                    </tr>
                </thead>
                <tbody>
                    {testStatuses.map(testStatus =>
                        <tr key={testStatus.partitionKey+test.rowKey}>
                            <td>{testStatus.partitionKey}</td>
                            <td>{testStatus.rowKey}</td>
                            <td>{testStatus.status}</td>

                        </tr>
                    )}
                </tbody>
            </table>
        );
    }
    render() {
        console.log("render")
        let contents = this.state.loading
            ? <p><em>Loading...</em></p>
            : TestStatus.renderTestStatusTable(this.state.testStatuses);
        return (
            <>
                <div>
                    <h1 id="tabelLabel" >Test Jobs</h1>
                    {contents}
                </div>
            </>
        );
    }

    async populateTestStatusData() {
        const response = await fetch('teststatus');
        const data = await response.json();
        this.setState({ testStatuses: data, loading: false });
    }


}