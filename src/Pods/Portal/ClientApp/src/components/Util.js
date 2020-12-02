import React, { Component } from 'react';

export class Util {

   static async CheckAuth(response) {
      console.log(response)
      if (response.type == "opaqueredirect") {
         await new Promise(r => setTimeout(r, 1000));
         console.log("need to login")
         window.location.href = "/signin-oidc"
      }
   }
}