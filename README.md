# Setup

## Dependency
1. [WSL](https://docs.microsoft.com/en-us/windows/wsl/install) or Linux 
2. [az cli](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli-linux?pivots=apt) [Version >=2.26]
3. [.NET 5 sdk](https://dotnet.microsoft.com/en-us/download/dotnet/5.0)
4. A subscription which you have [owner](https://docs.microsoft.com/en-us/azure/role-based-access-control/role-assignments-portal-subscription-admin) permission
5. [jq](https://stedolan.github.io/jq/) , [nodejs](https://nodejs.org/en/) , [npm](https://www.npmjs.com/package/npm)

## Steps
SignalR performance tool uses AKS to run the tests. To setup the initial environment, you could
```bash
cd Initialize
./init.sh -p [prefix] -l location 
```
Enable AAD authentication

1. [create an application in AAD and add redirect web url in the final output of the init script](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-create-service-principal-portal#register-an-application-with-azure-ad-and-create-a-service-principal), then [enable ID tokens](./media/aad.png)
2. [Add a "Contributor" Role.](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps#app-roles-ui) and wait for afew minutes for AAD toy sync up, [assign this role to allowed users](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-add-app-roles-in-azure-ad-apps#assign-users-and-groups-to-roles)
3. Modify the Azure AAD config (clientID and TenantID) in src/Pods/Portal/appsettings.json accordingly(Use the clientID and tenantID of the Application you just created). 

After that, you need to init the deployments inside the aks

```bash
./publish -p [prefix] -a
```

**Now everything is ready, you could go to the printed url to create performance tests!**

### FAQs
1. Failured trying to add nodepool 
> VM quota issues: You need to change the VM size according to the quota in your subscription or [request VM quota](https://docs.microsoft.com/en-us/azure/azure-portal/supportability/per-vm-quota-requests)

> Outbound IP port issue: You need to [add more ips](https://docs.microsoft.com/en-us/azure/aks/load-balancer-standard#scale-the-number-of-managed-outbound-public-ips) to your load balancer if you trying to add a large node pool 

2. SSL cert not safe
> NSG issue: If the NSG in the created AKS resource group blocks outbound traffic, the SSL certificate verification would fail. 

3. What's the portal domain or redirect url? Run below command to get the urls
>  ./publish -p [prefix] 




# Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.microsoft.com.

When you submit a pull request, a CLA-bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., label, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
