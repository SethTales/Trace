using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;
using Trace.Adapters;
using Trace.Adapters.Interfaces;
using Trace.API.IntegrationTests.Adapters;
using Trace.API.IntegrationTests.Adapters.Interfaces;
using Trace.API.IntegrationTests.Models;

namespace Trace.API.IntegrationTests
{
    public class TestServer : WebApplicationFactory<Startup>
    {
        public IServiceProvider GetTestServiceProvider()
        {
            return TestStartup.GetServiceProvider();
        }

        protected override IWebHostBuilder CreateWebHostBuilder()
        {
            // Intercept and inject our own environment for tests
            return Program.CreateWebHostBuilder(new string[0])
                .UseEnvironment("Development")
                .UseStartup<TestStartup>();
        }
    }

    public class TestStartup : Startup
    {
        private static IServiceCollection _services;

        public TestStartup(IHostingEnvironment hostingEnvironment) : base(hostingEnvironment)
        {
        }

        public override void ConfigureServices(IServiceCollection services)
        {
            base.ConfigureServices(services);
            _services = services;
        }

        internal static IServiceProvider GetServiceProvider()
        {
            return _services.BuildServiceProvider();
        }
    }
}
