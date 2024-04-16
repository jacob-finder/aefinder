using AeFinder.Kubernetes.Adapter;
using AeFinder.Kubernetes.ResourceDefinition;
using k8s;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Kubernetes.Manager;

public class KubernetesAppManager:IKubernetesAppManager,ISingletonDependency
{
    // private readonly k8s.Kubernetes _k8sClient;
    private readonly KubernetesOptions _kubernetesOptions;
    private readonly ILogger<KubernetesAppManager> _logger;
    private readonly IKubernetesClientAdapter _kubernetesClientAdapter;

    public KubernetesAppManager(ILogger<KubernetesAppManager> logger, 
        // k8s.Kubernetes k8sClient,
        IKubernetesClientAdapter kubernetesClientAdapter,
        IOptionsSnapshot<KubernetesOptions> kubernetesOptions)
    {
        _logger = logger;
        // _k8sClient = k8sClient;
        _kubernetesClientAdapter = kubernetesClientAdapter;
        _kubernetesOptions = kubernetesOptions.Value;
    }

    public async Task<string> CreateNewAppPodAsync(string appId, string version, string imageName)
    {
        await CheckNameSpaceAsync();
        
        await CreateFullClientTypeAppPodAsync(appId, version, imageName);

        return await CreateQueryClientTypeAppPodAsync(appId, version, imageName);
    }

    private async Task CheckNameSpaceAsync()
    {
        var namespaces = await _kubernetesClientAdapter.ListNamespaceAsync();

        string nameSpace = KubernetesConstants.AppNameSpace;
        // 检查特定的命名空间是否存在
        var namespaceExists = namespaces.Items.Any(n => n.Metadata.Name == nameSpace);
        
        if (!namespaceExists)
        {
            _logger.LogInformation($"Namespace '{nameSpace}' does not exist.");
            var newNamespace = NameSpaceHelper.CreateNameSpaceDefinition(nameSpace);
            // 创建命名空间
            var result = await _kubernetesClientAdapter.CreateNamespaceAsync(newNamespace);
            _logger.LogInformation($"Namespace created: {result.Metadata.Name}");
        }
    }

    private async Task CreateFullClientTypeAppPodAsync(string appId, string version, string imageName)
    {
        //Create full app appsetting config map
        string configMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version,KubernetesConstants.AppClientTypeFull);
        string appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeFull);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        bool configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap,KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {configMapName} created", configMapName);
        }

        //Create full app filebeat config map
        string sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId,version,KubernetesConstants.AppClientTypeFull);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeFull.ToLower());
        bool sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(sideCarConfigMap,KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {sideCarConfigName} created", sideCarConfigName);
        }

        //Create full app deployment
        string deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull);
        string containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeFull);
        int replicasCount = _kubernetesOptions.AppPodReplicas;
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        {
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(imageName,
                deploymentName, replicasCount, containerName, configMapName, sideCarConfigName);
            // Create Deployment
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Deployment {deploymentName} created", deploymentName);
        }
    }

    private async Task<string> CreateQueryClientTypeAppPodAsync(string appId, string version, string imageName)
    {
        //Create query app appsetting config map
        string configMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery);
        string appSettingsContent = File.ReadAllText(KubernetesConstants.AppSettingTemplateFilePath);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderAppId, appId);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderVersion, version);
        appSettingsContent = appSettingsContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeQuery);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        bool configMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == configMapName);
        if (!configMapExists)
        {
            var configMap =
                ConfigMapHelper.CreateAppSettingConfigMapDefinition(configMapName, appSettingsContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(configMap,KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {configMapName} created", configMapName);
        }
        
        //Create query app filebeat config map
        string sideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId,version,KubernetesConstants.AppClientTypeQuery);
        var sideCarConfigContent = File.ReadAllText(KubernetesConstants.AppFileBeatConfigTemplateFilePath);
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderAppId, appId.ToLower());
        sideCarConfigContent = sideCarConfigContent.Replace(KubernetesConstants.PlaceHolderClientType,
            KubernetesConstants.AppClientTypeQuery.ToLower());
        bool sideCarConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == sideCarConfigName);
        if (!sideCarConfigMapExists)
        {
            var sideCarConfigMap =
                ConfigMapHelper.CreateFileBeatConfigMapDefinition(sideCarConfigName, sideCarConfigContent);
            // Submit the ConfigMap to the cluster
            await _kubernetesClientAdapter.CreateConfigMapAsync(sideCarConfigMap,KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {sideCarConfigName} created", sideCarConfigName);
        }
        
        //Create query app deployment
        string deploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery);
        string containerName =
            ContainerHelper.GetAppContainerName(appId, version, KubernetesConstants.AppClientTypeQuery);
        int replicasCount = _kubernetesOptions.AppPodReplicas;
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool deploymentExists = deployments.Items.Any(item => item.Metadata.Name == deploymentName);
        if (!deploymentExists)
        {
            var deployment = DeploymentHelper.CreateAppDeploymentWithFileBeatSideCarDefinition(imageName,
                deploymentName, replicasCount, containerName, configMapName, sideCarConfigName);
            // Create Deployment
            await _kubernetesClientAdapter.CreateDeploymentAsync(deployment, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Deployment {deploymentName} created", deploymentName);
        }

        //Create query app service
        string serviceName = ServiceHelper.GetAppServiceName(appId, version);
        int targetPort = KubernetesConstants.AppContainerTargetPort;
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        bool sericeExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (!sericeExists)
        {
            var service =
                ServiceHelper.CreateAppClusterIPServiceDefinition(serviceName, deploymentName, targetPort);
            // Create Service
            await _kubernetesClientAdapter.CreateServiceAsync(service, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} created", serviceName);
        }

        //Create query app ingress
        string ingressName = IngressHelper.GetAppIngressName(appId, version);
        string hostName = _kubernetesOptions.HostName;
        // string rulePath = $"/{appId}";
        string rulePath = $"/{appId}/{version}";
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        bool ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (!ingressExists)
        {
            var ingress =
                IngressHelper.CreateAppIngressDefinition(ingressName, hostName,
                    rulePath, serviceName, targetPort);
            // Submit the Ingress to the cluster
            await _kubernetesClientAdapter.CreateIngressAsync(ingress, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} created", ingressName);
        }

        return hostName + rulePath + "/graphql";
    }

    public async Task DestroyAppPodAsync(string appId, string version)
    {
        //Delete full app deployment
        string fullTypeAppDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool fullTypeAppDeploymentExists = deployments.Items.Any(item => item.Metadata.Name == fullTypeAppDeploymentName);
        if (fullTypeAppDeploymentExists)
        {
            // Delete the existing Deployment
            await _kubernetesClientAdapter.DeleteDeploymentAsync(
                fullTypeAppDeploymentName,
                KubernetesConstants.AppNameSpace
            );
            _logger.LogInformation("[KubernetesAppManager]Deployment {fullTypeAppDeploymentName} deleted.", fullTypeAppDeploymentName);
        }

        //Delete full app appsetting config map
        string fullTypeAppConfigMapName = ConfigMapHelper.GetAppSettingConfigMapName(appId, version,KubernetesConstants.AppClientTypeFull);
        var configMaps = await _kubernetesClientAdapter.ListConfigMapAsync(KubernetesConstants.AppNameSpace);
        bool fullTypeAppConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == fullTypeAppConfigMapName);
        if (fullTypeAppConfigMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(fullTypeAppConfigMapName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {fullTypeAppConfigMapName} deleted.", fullTypeAppConfigMapName);
        }

        //Delete full app filebeat config map
        string fullTypeAppSideCarConfigName =
            ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeFull);
        bool fullTypeAppSideCarConfigExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == fullTypeAppSideCarConfigName);
        if (fullTypeAppSideCarConfigExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(fullTypeAppSideCarConfigName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {fullTypeAppSideCarConfigName} deleted.", fullTypeAppSideCarConfigName);
        }

        //Delete query app deployment
        string queryTypeAppDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery);
        bool queryTypeAppDeploymentExists = deployments.Items.Any(item => item.Metadata.Name == queryTypeAppDeploymentName);
        if (queryTypeAppDeploymentExists)
        {
            // Delete the existing Deployment
            await _kubernetesClientAdapter.DeleteDeploymentAsync(
                queryTypeAppDeploymentName,
                KubernetesConstants.AppNameSpace
            );
            _logger.LogInformation("[KubernetesAppManager]Deployment {queryTypeAppDeploymentName} deleted.", queryTypeAppDeploymentName);
        }

        //Delete query app appsetting config map
        string queryTypeAppConfigMapName =
            ConfigMapHelper.GetAppSettingConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery);
        bool queryTypeAppConfigMapExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppConfigMapName);
        if (queryTypeAppConfigMapExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppConfigMapName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppConfigMapName} deleted.", queryTypeAppConfigMapName);
        }

        //Delete query app filebeat config map
        string queryTypeAppSideCarConfigName = ConfigMapHelper.GetAppFileBeatConfigMapName(appId, version, KubernetesConstants.AppClientTypeQuery);
        bool queryTypeAppSideCarConfigExists = configMaps.Items.Any(configMap => configMap.Metadata.Name == queryTypeAppSideCarConfigName);
        if (queryTypeAppSideCarConfigExists)
        {
            await _kubernetesClientAdapter.DeleteConfigMapAsync(queryTypeAppSideCarConfigName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]ConfigMap {queryTypeAppSideCarConfigName} deleted.", queryTypeAppSideCarConfigName);
        }

        //Delete query app service
        string serviceName = ServiceHelper.GetAppServiceName(appId, version);
        var services = await _kubernetesClientAdapter.ListServiceAsync(KubernetesConstants.AppNameSpace);
        bool sericeExists = services.Items.Any(item => item.Metadata.Name == serviceName);
        if (sericeExists)
        {
            await _kubernetesClientAdapter.DeleteServiceAsync(serviceName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Service {serviceName} deleted.", serviceName);
        }

        //Delete query app ingress
        string ingressName = IngressHelper.GetAppIngressName(appId, version);
        var ingresses = await _kubernetesClientAdapter.ListIngressAsync(KubernetesConstants.AppNameSpace);
        bool ingressExists = ingresses.Items.Any(item => item.Metadata.Name == ingressName);
        if (ingressExists)
        {
            await _kubernetesClientAdapter.DeleteIngressAsync(ingressName, KubernetesConstants.AppNameSpace);
            _logger.LogInformation("[KubernetesAppManager]Ingress {ingressName} deleted.", ingressName);
        }
    }

    public async Task RestartAppPodAsync(string appId, string version)
    {
        //Restart Full Client Type App Pod
        string fullClientDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeFull);
        var deployments = await _kubernetesClientAdapter.ListDeploymentAsync(KubernetesConstants.AppNameSpace);
        bool fullClientDeploymentExists = deployments.Items.Any(item => item.Metadata.Name == fullClientDeploymentName);
        if (fullClientDeploymentExists)
        {
            var deployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(fullClientDeploymentName, KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;

            // Update Deployment
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, fullClientDeploymentName, KubernetesConstants.AppNameSpace);
        }
        else
        {
            _logger.LogError($"Deployment {fullClientDeploymentName} is not exists!");
        }
        
        
        //Restart Query Client Type App Pod
        string queryClientDeploymentName =
            DeploymentHelper.GetAppDeploymentName(appId, version, KubernetesConstants.AppClientTypeQuery);
        bool queryClientDeploymentExists = deployments.Items.Any(item => item.Metadata.Name == queryClientDeploymentName);
        if (queryClientDeploymentExists)
        {
            var deployment = await _kubernetesClientAdapter.ReadNamespacedDeploymentAsync(queryClientDeploymentName, KubernetesConstants.AppNameSpace);
            // Add or update annotations to trigger rolling updates
            var annotations = deployment.Spec.Template.Metadata.Annotations ?? new Dictionary<string, string>();
            annotations["kubectl.kubernetes.io/restartedAt"] = DateTime.UtcNow.ToString("s");
            deployment.Spec.Template.Metadata.Annotations = annotations;

            // Update Deployment
            await _kubernetesClientAdapter.ReplaceNamespacedDeploymentAsync(deployment, queryClientDeploymentName, KubernetesConstants.AppNameSpace);
        }
        else
        {
            _logger.LogError($"Deployment {queryClientDeploymentName} is not exists!");
        }
    }
}