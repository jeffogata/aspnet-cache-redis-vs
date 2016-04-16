namespace AspNetCacheRedis
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;
    using Microsoft.AspNet.Builder;
    using Microsoft.AspNet.Http;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.Extensions.Caching.Redis;
    using Microsoft.Extensions.DependencyInjection;

    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRedisCache();
            services.AddOptions();
            services.Configure<RedisCacheOptions>(opt =>
            {
                opt.Configuration = "localhost";
                opt.InstanceName = "test";
            });
        }

        public void Configure(IApplicationBuilder app, IDistributedCache cache)
        {
            app.UseIISPlatformHandler();

            app.Use(async (context, next) =>
            {
                await context.Response.WriteAsync("ASP.NET Core:  Redis Cache");
                await next();
            });

            app.Map(new PathString("/set"), branch =>
            {
                branch.Run(async context =>
                {
                    foreach (var value in context.Request.Query)
                    {
                        await cache.SetAsync(value.Key, Encoding.UTF8.GetBytes(value.Value.First()));
                    }

                    await context.Response.WriteAsync("<br>values set");
                });
            });

            app.Map(new PathString("/get"), branch =>
            {
                branch.Run(async context =>
                {
                    var keys = context.Request.Query["key"];

                    foreach (var key in keys)
                    {
                        var value = await cache.GetAsync(key);
                        var valueString = value == null ? "(not found)" : Encoding.UTF8.GetString(value);
                        await context.Response.WriteAsync($"<br>{key} :: {valueString}");
                    }
                });
            });

            app.Map(new PathString("/del"), branch =>
            {
                branch.Run(async context =>
                {
                    var keys = context.Request.Query["key"];

                    foreach (var key in keys)
                    {
                        await cache.RemoveAsync(key);
                    }

                    await context.Response.WriteAsync("<br>values removed");
                });
            });

            app.Map(new PathString("/perf"), branch =>
            {
                branch.Run(async context =>
                {
                    var key = Guid.NewGuid().ToString();
                    var value = Guid.NewGuid().ToByteArray();

                    await cache.SetAsync(key, value);

                    Guid cacheValue;

                    var reps = 10000;

                    var sw = Stopwatch.StartNew();

                    for (var i = 0; i < reps; i++)
                    {
                        cacheValue = new Guid(await cache.GetAsync(key));
                    }

                    sw.Stop();

                    await
                        context.Response.WriteAsync(
                            $"<br>average time to retrieve value: {sw.ElapsedTicks/(decimal) reps} ticks");
                });
            });
        }
    }
}