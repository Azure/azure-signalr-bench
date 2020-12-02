import React, { Component } from 'react';
import {Util} from './Util';


export class Home extends Component {
  static displayName = Home.name;

  constructor(props) {
    super(props);
    this.state = { forecasts: [], loading: true };
  }


  componentDidMount() {
    this.info();
  }
  render () {
    return (
      <div>
        <h1>SignalR Perf V2</h1>
        <p>Welcome to use perf , Here are some useful links:</p>
        <ul>
          <li><a href='/k8s/' target="_blank">K8s Dashboard</a> to check client and server details</li>
        </ul>
        <p>To do List</p>
        <ul>
          <li><strong>Metrics</strong>. For example,  <em>CPU</em> and <em>memory</em> info for reviewing</li>
          <li><strong>Long run test</strong> </li>
          <li><strong>Periodic tests</strong>  </li>
        </ul>
        <p>Done List</p>
        <ul>
          <li><strong>On demand test</strong></li>
          <li><strong>Delete button</strong>. For example, delete <em>TestConfig</em> or <em>TestStatus</em></li>
          <li><strong>Search</strong> </li>
          <li><strong>Authentication</strong>  <code>ASRS</code> </li>
        </ul>
      </div>
    );
  }

  async info() {
    const response = await fetch('home/info',{
      redirect: 'manual'
    })
    console.log(response)
    Util.CheckAuth(response)
    const data =  response.json();
    this.setState({ info: data});
  }
}
