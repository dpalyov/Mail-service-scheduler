using System;
using System.IO;
using MailService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace MailServiceWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            using (var host = CreateHostBuilder(args).Build())
            {
                host.Run();
            }

        }
        /// <summary>
        /// Building the hosted service - Configuration, Logger, registering services to the DI Container
        /// </summary>
        /// <param name="args">1 arg to specify which would be the locaton of the configuration file</param>
        /// <returns></returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    //System.Diagnostics.Debugger.Launch();

                    var configFile = args.Length == 0
                                        ? throw new ArgumentException("Missing configuration path argument!")
                                        : args[0];

                    if (!File.Exists(configFile))
                    {
                        throw new IOException("Config file not found!");
                    }

                    var builder = new ConfigurationBuilder();
                    builder.AddJsonFile(
                        path: configFile,
                        optional: false,
                        reloadOnChange: true
                    );

                    var configuration = builder.Build();

                    var dir = Path.GetDirectoryName(Path.Combine(Environment.CurrentDirectory, configFile));
                    var logPath = Path.Combine("Logs", "serilog.txt");

                    services.AddLogging(config =>
                    {
                        config.AddConfiguration(configuration.GetSection("Logging"))
                        .AddSerilog(new LoggerConfiguration().WriteTo.File(logPath, rollingInterval: RollingInterval.Day).CreateLogger())
                        .AddConsole();
                    });

                    services.AddSingleton<IMailService, MailClient>();
                    // var mailService = new MailClient();

                    //mailService.ConfigureClient("vistsmtp.visteon.com", 25, true);
                    var sp = services.BuildServiceProvider();
                    var loggerService = sp.GetService<ILogger<TimedService>>();
                    var mailService = sp.GetService<IMailService>();

                    services.AddHostedService<TimedService>(p => new TimedService(mailService, configuration,loggerService));


                })
                .UseWindowsService();
    }
}
