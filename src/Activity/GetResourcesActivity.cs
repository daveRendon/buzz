using System;
using System.Linq;
using Buzz.Extensions;
using Buzz.Model;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Buzz.Activity
{
    public class GetResourcesActivity
    {
        private static ILogger _log;

        [FunctionName("GetResourcesActivity")]
        public static string[] Run([ActivityTrigger] DurableActivityContext context, ILogger log,
            ExecutionContext executionContext)
        {
            _log = log;
            var input = new {Name = context.GetInput<string>()};
            var config = executionContext.BuildConfiguration();
            var clientId = config["ApplicationId"];
            var clientSecret = config["ApplicationSecret"];
            var tenantId = config["TenantId"];
            string subscriptionId = config["SubscriptionId"];
            log.LogInformation($"GetGroupsActivity Activity function start process {input.Name}");

            try
            {
                if (!AzureCredentials.Make(tenantId, clientId, clientSecret, subscriptionId)
                    .TryGetAzure(out IAzure azure, message => _log.LogError(message))) return Array.Empty<string>();
                return azure.GetVmssNames(input.Name).ToArray();
            }
            catch(Exception e)
            {
                _log.LogError($"GetGroupsActivity error processing function {e.Message}", e);
            }
            return Array.Empty<string>();
        }
    }
}