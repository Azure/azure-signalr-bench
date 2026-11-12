import React from 'react';

const latencyColumns = [
    ['LessThan50ms', '0-50ms'],
    ['LessThan100ms', '50-100ms'],
    ['LessThan200ms', '100-200ms'],
    ['LessThan500ms', '200-500ms'],
    ['LessThan1s', '500-1000ms'],
    ['LessThan2s', '1-2s'],
    ['LessThan5s', '2-5s'],
    ['MoreThan5s', '5+s']
];

export function TestReport({ testStatus }) {
    const report = testStatus.report ? JSON.parse(testStatus.report) : [];
    const config = testStatus.config ? JSON.parse(testStatus.config) : null;
    const result = testStatus.check || (!testStatus.healthy ? 'Failed' :
        testStatus.jobState === 'Cleaned' ? 'Passed' : 'In progress');
    const resultStyle = !testStatus.healthy ? 'danger' :
        testStatus.check ? 'warning' : testStatus.jobState === 'Cleaned' ? 'success' : 'info';
    const errorSummary = testStatus.errorSummary ||
        (testStatus.errorInfo && testStatus.errorInfo.trim().split(/\r?\n/)[0]);
    const hasCheckFailures = report.some(round => round.CheckFailures.length > 0);

    return (
        <>
            {config && <h4 className="configCss">
                {'{ Total connections: ' + config.ClientCons + ', Protocol: ' + config.Protocol +
                    ', Scenario: ' + config.Scenario +
                    (config.Scenario === 'GroupBroadcast' ? ' [Size: ' + config.GroupSize + ']' : '') + ' }'}
            </h4>}
            <section className={'alert alert-' + resultStyle} aria-label="Test result">
                <strong>{result === 'Suspicious' ? 'Thresholds not met' : result}</strong>
                {errorSummary && <p style={{ whiteSpace: 'pre-line' }}>{errorSummary}</p>}
                {hasCheckFailures && <>
                    {result === 'Passed' && <div>Earlier rounds below standard</div>}
                    <div className="table-responsive">
                        <table className="table table-sm mb-0" aria-label="Checks below standard">
                            <thead>
                                <tr><th>Round</th><th>Metric</th><th>Required</th><th>Actual</th></tr>
                            </thead>
                            <tbody>
                                {report.map((round, roundIndex) => round.CheckFailures.map((failure, failureIndex) =>
                                    <tr key={roundIndex + '-' + failureIndex}>
                                        <td>{roundIndex + 1}</td>
                                        <td>{failure.Metric}</td>
                                        <td>{failure.Required}</td>
                                        <td>{failure.Actual}</td>
                                    </tr>))}
                            </tbody>
                        </table>
                    </div>
                </>}
                {testStatus.errorInfo && <details>
                    <summary>Full error details</summary>
                    <pre style={{ whiteSpace: 'pre-wrap', overflowWrap: 'anywhere' }}>{testStatus.errorInfo}</pre>
                </details>}
            </section>
            {report.length === 0 ? <p>No rounds completed.</p> :
                <div className="table-responsive">
                <table className="table table-striped" aria-label="Round statistics">
                    <thead>
                        <tr>
                            <th>Round</th>
                            <th>Connected</th>
                            <th>Active</th>
                            <th>Send</th>
                            <th>Receive</th>
                            <th>Reconnected</th>
                            <th>Reconnecting</th>
                            {latencyColumns.map(([key, label]) => <th key={key}>{label}</th>)}
                        </tr>
                    </thead>
                    <tbody>
                        {report.map((round, index) => <tr key={index}>
                            <td>{index + 1}</td>
                            <td>{round.ConnectedCount}/{round.RoundConnected}</td>
                            <th scope="row">{round.ActiveConnection}</th>
                            <td>{round.ClientReceivedServerAckCount > 0
                                ? round.ClientReceivedServerAckCount + '/' + round.MessageSent : round.MessageSent}</td>
                            <td>{round.MessageRecieved}/{round.ExpectedRecievedMessageCount}</td>
                            <td>{round.TotalReconnectCount}</td>
                            <td>{round.ReconnectingCount}</td>
                            {latencyColumns.map(([key]) => <td key={key}>{round.MessageRecieved > 0
                                ? (round.Latency[key] / round.MessageRecieved * 100).toFixed(2) + '%' : 'N/A'}</td>)}
                        </tr>)}
                    </tbody>
                </table>
                </div>}
        </>
    );
}
