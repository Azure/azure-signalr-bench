import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { TestConfig } from './components/TestConfig';
import {TestStatus} from "./components/TestStatus"

import './custom.css'

export default class App extends Component {
    static displayName = App.name;

    render() {
        return (
            <Layout>
                <Route path='/test-config' component={TestConfig} />
                <Route exact path='/test-status' component={TestStatus} />
                <Route path='/test-status/testname/:key?' component={TestStatus} />
                <Route path='/test-status/dir/:dir/:index' component={TestStatus} />
                <Route exact path='/' component={Home} />
            </Layout>
        );
    }
}
