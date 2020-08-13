using k8s;
using System.IO;
using System.Text;

namespace Coordinator
{
    class KubeCtlHelper
    {
        private Kubernetes kubernetes;

        public KubeCtlHelper()
        {
            kubernetes = getKubeClient();
        }
        public Kubernetes getKubeClient()
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(PerfConfig.KubeConfig)))
            {
                var config = KubernetesClientConfiguration.BuildConfigFromConfigFile(stream);
                return new Kubernetes(config);
            };
        }
    }
}
