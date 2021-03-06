﻿using System.Reflection;
using System.Web.Http;
using Amazon.DynamoDBv2;
using API.Config;
using API.Controllers;
using API.Data;
using API.Filters;
using Autofac;
using Autofac.Integration.WebApi;
using AutofacSerilogIntegration;
using Serilog;

namespace API
{
    public static class ContainerConfig
    {
        internal static IContainer ConfigureContainer(HttpConfiguration config)
        {
            var builder = new ContainerBuilder();

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            builder.RegisterWebApiFilterProvider(config);

            builder.RegisterType<Context>().As<IContext>().SingleInstance();

            builder.Register(c => new AmazonDynamoDBClient(DynamoDbHelper.ConfigureDynamoDb())).SingleInstance();

            builder.RegisterType<ReviewTable>().AsSelf();

            // TODO: configure different sinks for information and error and stuff in between
            Log.Logger = new LoggerConfiguration()
                //.WriteTo.DynamoDB("Local-Log") // TODO: how do we configure this for local and different envs?
                .WriteTo.Trace()
                .MinimumLevel.Debug()
                .CreateLogger();

            builder.Register(c => new GlobalExceptionFilter(c.Resolve<ILogger>()))
                .AsWebApiExceptionFilterFor<ApiController>().SingleInstance(); // should this be single instance, that is the default in web api...?

            builder.Register(c => new IntegrationTestFilter())
               .AsWebApiActionFilterFor<TestController>().SingleInstance();

            builder.Register(c => new RequestLogFilter(c.Resolve<ILogger>()))
             .AsWebApiActionFilterFor<ApiController>().SingleInstance(); // single instance or not?

            builder.RegisterLogger();

            var container = builder.Build();
            return container;

        } 
    }
}