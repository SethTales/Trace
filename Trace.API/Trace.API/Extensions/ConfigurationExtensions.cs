using Microsoft.Extensions.Configuration;

namespace Trace.API.Extensions
{
    public static class ConfigurationExtensions
    {
        public static string GetValidIssuer(this IConfigurationRoot configuration, string userPoolId)
        {
            return configuration.GetSection("JWT")["ISSUER"].Replace("{USERPOOL_ID}", userPoolId);
        }
    }
}