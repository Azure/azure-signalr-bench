# Setup

SignalR performance tool uses AKS to run the tests. To setup the initial  environment, you could
```bash
cd Initialize
./init.sh -p [prefix] -l location 
```
After that, the resource group [prefix]rg will be created. The keyvault, service principal and etc.. will also be inited.
Then, create a different 
After that, you need to init the deployments inside the aks
```bash
./publish -p [prefix] -a
```
After that, you could find the portal domain in the output.


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
