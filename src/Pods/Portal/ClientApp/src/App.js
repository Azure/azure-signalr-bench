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
                <Route path='/test-status/:key?' component={TestStatus} />
                <Route exact path='/' component={Home} />
            </Layout>
        );
    }
}
