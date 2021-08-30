# Setup

### Dependency
1. WSL or Linux 
2. az cli [Version >=2.26]
3. .NET 5 sdk
4. A subscription which you have owner permission


SignalR performance tool uses AKS to run the tests. To setup the initial  environment, you could
```bash
cd Initialize
./init.sh -p [prefix] -l location 
```
Enable AAD authentication

1. create an application in AAD
2. Add the redirect url in the final output of the init script to the application's Web redirect URIs
3. Add a "Contributor Role"  and assign this role to allowed users
4.  change the Azure AD config (clientID and TenantID) in src/Pods/Portal/appsettings.json accordingly. 

After that, you need to init the deployments inside the aks

```bash
./publish -p [prefix] -a
```
### Note 

1. You need to change the VM size according to the quota in your subscription.
2. If the NSG in the created AKS resource group blocks outbound traffic, the SSL certificate verification would fail. 


Now everything is ready, you could go to the printed url to create performance tests!

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