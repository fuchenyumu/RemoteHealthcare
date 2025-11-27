// Copyright © 2023-present https://github.com/dymproject/purest-admin作者以及贡献者

using System.Net.Http.Headers;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using PurestAdmin.Application.RemoteHealthcare.Sfu;
using PurestAdmin.BackgroundService;
using PurestAdmin.Core.Mapster;
using PurestAdmin.Multiplex;

using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Modularity;

namespace PurestAdmin.Application
{
    [DependsOn(typeof(AdminBackgroundModule),
        typeof(AdminMultiplexModule))]
    public class AdminApplicationModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();

            context.Services.AddMapsterIRegister(Assembly.GetExecutingAssembly());
            Configure<AbpAspNetCoreMvcOptions>(options =>
            {
                options.ConventionalControllers.Create(typeof(AdminApplicationModule).Assembly, opts =>
                {
                    opts.RootPath = "v1";
                    //opts.UrlActionNameNormalizer = (action) =>
                    //{
                    //    return action.ActionNameInUrl;
                    //};
                });
            });

            context.Services.Configure<SfuOptions>(configuration.GetSection("SfuOptions"));
            context.Services.AddHttpClient<ISfuApiClient, SfuApiClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<SfuOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            });
        }
    }
}

