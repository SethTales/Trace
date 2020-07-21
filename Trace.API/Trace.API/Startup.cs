using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Trace.Adapters;
using Trace.Adapters.Helpers;
using Trace.Adapters.Helpers.Interfaces;
using Trace.Adapters.Interfaces;
using Trace.API.Extensions;

namespace Trace.API
{
    public class Startup
    {
        public IConfigurationRoot Configuration { get; }

        private readonly string _environment;

        public Startup(IHostingEnvironment hostingEnvironment)
        {
            _environment = hostingEnvironment.EnvironmentName;

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile($"appsettings.{_environment}.json")
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public virtual void ConfigureServices(IServiceCollection services)
        {
            var cognitoConfigKey = Configuration.GetSection("AWS").GetSection("Secrets")["CognitoConfig"];

            var secretsClient = new AmazonSecretsManagerClient(RegionEndpoint.USWest2);
            var cognitoConfigSecretResponse = secretsClient.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = cognitoConfigKey
            }).Result;
            var cognitoAdapterConfig = JsonConvert.DeserializeObject<AwsCognitoAdapterConfig>(cognitoConfigSecretResponse.SecretString);
            var validIssuer = Configuration.GetValidIssuer(cognitoAdapterConfig.UserPoolId);

            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                //options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });
            services
                .AddMvc().AddApplicationPart(typeof(Startup).Assembly) //need this to make the integrations tests work: https://stackoverflow.com/a/58079778
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
                

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        IssuerSigningKeyResolver = (s, token, identifier, parameters) =>
                        {
                            var json = new WebClient().DownloadString($"{validIssuer}/.well-known/jwks.json");
                            var keys = JsonConvert.DeserializeObject<JsonWebKeySet>(json).Keys;
                            return keys;
                        },
                        ClockSkew = TimeSpan.FromMinutes(5),
                        LifetimeValidator = (notBefore, expires, token, parameters) =>
                        {
                            if (expires != null)
                            {
                                if (DateTime.UtcNow < expires) return true;
                            }
                            return false;
                        },
                        ValidateIssuer = true,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = validIssuer
                    };
                });

            services.AddScoped(s => cognitoAdapterConfig);
            services.AddScoped<IAwsCognitoAdapterHelper, AwsCognitoAdapterHelper>();
            services.AddScoped<IAmazonCognitoIdentityProvider, AmazonCognitoIdentityProviderClient>();
            services.AddScoped<IAuthAdapter, AwsCognitoAdapter>();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseMvc();
        }
    }
}
