import React from 'react';
import ReactDOM from 'react-dom';
import { act } from 'react-dom/test-utils';
import { TestReport } from './TestReport';

let container;

beforeEach(() => {
    container = document.createElement('div');
    document.body.appendChild(container);
});

afterEach(() => {
    ReactDOM.unmountComponentAtNode(container);
    container.remove();
});

function createRound(overrides = {}) {
    return {
        ActiveConnection: 100,
        RoundConnected: 100,
        ConnectedCount: 100,
        MessageSent: 100,
        MessageRecieved: 100,
        ExpectedRecievedMessageCount: 100,
        TotalReconnectCount: 0,
        ReconnectingCount: 0,
        ClientReceivedServerAckCount: 0,
        CheckFailures: [],
        Latency: {
            LessThan50ms: 100,
            LessThan100ms: 0,
            LessThan200ms: 0,
            LessThan500ms: 0,
            LessThan1s: 0,
            LessThan2s: 0,
            LessThan5s: 0,
            MoreThan5s: 0
        },
        ...overrides
    };
}

function renderReport(overrides = {}) {
    const testStatus = {
        healthy: true,
        check: '',
        jobState: 'Cleaned',
        status: 'Test Finishes',
        report: JSON.stringify([createRound()]),
        ...overrides
    };
    act(() => {
        ReactDOM.render(<TestReport testStatus={testStatus} />, container);
    });
}

it('displays saved reasons for every affected round without applying browser thresholds', () => {
    const failure = { Metric: 'Messages received', Required: '>= 95% of expected', Actual: '90.00% (90/100)' };
    renderReport({
        check: 'Suspicious',
        report: JSON.stringify([
            createRound({ CheckFailures: [failure] }),
            createRound({ CheckFailures: [
                { Metric: 'Messages with latency >= 1s', Required: '<= 5% of received', Actual: '6.00% (6/100)' }
            ] })
        ])
    });

    expect(container.querySelector('section strong').textContent).toBe('Thresholds not met');
    const checks = container.querySelector('table[aria-label="Checks below standard"]');
    expect(Array.from(checks.querySelectorAll('thead th')).map(cell => cell.textContent))
        .toEqual(['Round', 'Metric', 'Required', 'Actual']);
    expect(Array.from(checks.querySelectorAll('tbody tr')[0].cells).map(cell => cell.textContent))
        .toEqual(['1', failure.Metric, failure.Required, failure.Actual]);
    expect(checks.querySelectorAll('tbody tr')[1].textContent).toContain('2Messages with latency >= 1s');
    expect(container.querySelectorAll('table[aria-label="Round statistics"] tbody tr')).toHaveLength(2);
    expect(container.textContent).not.toContain('Test Finishes');
    expect(container.textContent).not.toContain('Later rounds may clear');
});

it('retains completed rounds and expandable raw errors for failed runs', () => {
    const summary = 'System.InvalidOperationException: round 4: connecting, command StartClientConnections failed: 429.';
    const errorInfo = '\n  \n' + summary + '\n   at TestRunner.SendAndWaitAsync()';
    renderReport({ healthy: false, check: 'Failed', status: 'Test failed [395sec]', errorInfo });

    expect(container.querySelector('section strong').textContent).toBe('Failed');
    expect(container.querySelector('section p').textContent).toBe(summary);
    expect(container.querySelector('details').open).toBe(false);
    expect(container.querySelector('details pre').textContent).toBe(errorInfo);
    expect(container.querySelectorAll('table[aria-label="Round statistics"] tbody tr')).toHaveLength(1);
});

it('uses the recorded developer summary instead of the exception wrapper', () => {
    const errorSummary = 'Connection target not reached: 387/1000 new connections failed after 3 attempts.\n' +
        'The endpoint throttled connection requests (HTTP 429).\nWhere: round 4: connecting; pod client-13.';
    const errorInfo = 'System.InvalidOperationException: long command wrapper\n   at TestRunner.RunAsync()';
    renderReport({ healthy: false, check: 'Failed', errorSummary, errorInfo });

    expect(container.querySelector('section p').textContent).toBe(errorSummary);
    expect(container.querySelector('details').open).toBe(false);
    expect(container.querySelector('details pre').textContent).toBe(errorInfo);
});

it('keeps earlier round reasons visible after Suspicious clears', () => {
    renderReport({
        report: JSON.stringify([
            createRound({ CheckFailures: [
                { Metric: 'Messages received', Required: '>= 95% of expected', Actual: '90.00% (90/100)' }
            ] }),
            createRound()
        ])
    });

    expect(container.querySelector('section strong').textContent).toBe('Passed');
    expect(container.querySelector('table[aria-label="Checks below standard"]').textContent).toContain('Messages received');
    expect(container.textContent).toContain('Earlier rounds below standard');
});

it('distinguishes cancellation before any round completes', () => {
    renderReport({ healthy: false, check: 'Cancelled', report: '' });

    expect(container.querySelector('section strong').textContent).toBe('Cancelled');
    expect(container.textContent).toContain('No rounds completed.');
    expect(container.querySelector('table')).toBeNull();
});

it('shows zero-message reasons without meaningless latency percentages', () => {
    renderReport({
        check: 'Suspicious',
        report: JSON.stringify([createRound({
            MessageRecieved: 0,
            CheckFailures: [{ Metric: 'Messages received', Required: '> 0 messages', Actual: '0 (expected 100)' }]
        })])
    });

    expect(container.querySelector('table[aria-label="Checks below standard"]').textContent).toContain('0 (expected 100)');
    expect(container.textContent).toContain('N/A');
    expect(container.textContent).not.toContain('NaN');
});
