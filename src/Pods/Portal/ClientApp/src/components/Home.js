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
          <li><a href='https://get.asp.net/'>ASP.NET Core</a> and <a href='https://msdn.microsoft.com/en-us/library/67ef8sbd.aspx'>C#</a> for cross-platform server-side code</li>
          <li><a href='https://facebook.github.io/react/'>React</a> for client-side code</li>
          <li><a href='http://getbootstrap.com/'>Bootstrap</a> for layout and styling</li>
        </ul>
        <p>To do List</p>
        <ul>
          <li><strong>Delete button</strong>. For example, delete <em>TestConfig</em> or <em>TestStatus</em></li>
          <li><strong>Search</strong> </li>
          <li><strong>Authentication</strong>  <code>ASRS</code> </li>
        </ul>
        {/* <p>The <code>ClientApp</code> subdirectory is a standard React application based on the <code>create-react-app</code> template. If you open a command prompt in that directory, you can run <code>npm</code> commands such as <code>npm test</code> or <code>npm install</code>.</p> */}
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
