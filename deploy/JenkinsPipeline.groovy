pipeline {
    agent any
    options {
        disableConcurrentBuilds()
    }
    parameters {
        string(name: 'BenchServerGroup', defaultValue: 'hz1wus2BenchResourceGroup', description: '')
        string(name: 'VnetName', defaultValue: 'hz1wus2VNet', description: '')
        string(name: 'SubnetName', defaultValue: 'hz1wus2Subnet', description: '')
        string(name: 'clientVmCount', defaultValue: '1', description: '')
        string(name: 'serverVmCount', defaultValue: '1', description: '')
        string(name: 'nginx_server_dns', defaultValue: 'hz1wus2benchdns0.westus2.cloudapp.azure.com', description: '')
        string(name: 'nginx_root', defaultValue: '/mnt/Data/NginxRoot', description: 'Stores performance results')
        string(name: 'bench_send_size', defaultValue: '2048', description: '')
        string(name: 'Sku', defaultValue: 'Basic_DS2', description: '')
        string(name: 'g_nginx_ns', defaultValue: 'ingress-nginx', description: '')
        string(name: 'copy_syslog', defaultValue: 'true', description: '')
        string(name: 'copy_nginx_log', defaultValue: 'true', description: '')
        string(name: 'VMLocation', defaultValue: 'westus2', description: '')
        string(name: 'sigbench_run_duration', defaultValue: '1', description: '')
        string(name: 'bench_config_hub', defaultValue: 'signalrbench', description: 'This is only used in gen_html.sh to display hub in html page')
        string(name: 'bench_scenario_list', defaultValue: 'sendToClient', description: 'echo, broadcast, sendToClient')
        string(name: 'bench_transport_list', defaultValue: 'Websockets', description: 'Valid transports: "Websockets" "LongPolling" "ServerSentEvents". Many transports are separated by whitespace')
        string(name: 'bench_encoding_list', defaultValue: 'json', description: 'json, messagepack')
        string(name: 'bench_serviceunit_list', defaultValue: '1', description: 'Dogfood instance size: "1 2 5 10 20 50 100"')
        string(name: 'ASRSLocation', defaultValue: 'eastus', description: '')
        string(name: 'useMaxConnection', defaultValue: 'false', description: '')
        string(name: 'Disable_UNIT_PER_POD', defaultValue: 'false', description: '')
        choice(name: 'ASRSEnv', choices: ['dogfood', 'production'], description: '')
        string(name: 'Disable_Connection_Throttling', defaultValue: 'true', description: '')
        string(name: 'sendToClientMsgSize', defaultValue: '256', description: '256 2k 16k 128k')
        string(name: 'VMUser', defaultValue: 'wanl', description: '')
        string(name: 'VMPassword', defaultValue: 'Adafserew#@145', description: '')
    }
    stages {
        stage('Preparation') {
            steps {
                git branch: 'wanl/jenkins-run-script', url: 'https://github.com/Azure/azure-signalr-bench'
            }        
        }
        stage('Build') {
            options {
                azureKeyVault([
                    [envVariable: 'kubeconfig_srprodacswestus2k_json', name: 'kubeconfig-srprodacswestus2k-json', secretType: 'Secret'],
                    [envVariable: 'kubeconfig_srdevacseastusa_json', name: 'kubeconfig-srdevacseastusa-json', secretType: 'Secret'],
                    [envVariable: 'kubeconfig_srdevacsseasiaa_json', name: 'kubeconfig-srdevacsseasiaa-json', secretType: 'Secret'],
                    [envVariable: 'sp_DF', name: 'sp-DF', secretType: 'Secret'],
                    [envVariable: 'sp_INT', name: 'sp-INT', secretType: 'Secret'],
                ])
            }
            steps {
                sh label: '', script: '''# DO NOT edit the parameters in Jenkins Job above, unless you know what you are doing.
                cd scripts && \\
                source ./jenkins_job_run.sh &&\\
                jenkins_job_run $GlobalResourceGroupPrefix $GlobalVmImageId $VMUser $VMPassword $VMLocation $clientVmCount $serverVmCount $nginx_root'''
            }
        }
    }
    
}