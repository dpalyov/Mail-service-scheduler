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

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {

                    var configFile = args.Length == 0
                                        ? throw new ArgumentNullException("Missing configuration path argument!")
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
                    var logPath = Path.Combine(dir, "serilog.txt");

                    services.AddLogging(config =>
                    {
                        config.AddConfiguration(configuration.GetSection("Logging"))
                        .AddSerilog(new LoggerConfiguration().WriteTo.File(logPath).CreateLogger())
                        .AddConsole();
                    });

                    services.AddSingleton<IMailService, MailClient>();

                    var sp = services.BuildServiceProvider();
                    var mailService = (IMailService)sp.GetService(typeof(IMailService));
                    var loggerService = (ILogger<TimedService>)sp.GetService((typeof(ILogger<TimedService>)));
                    mailService.ConfigureClient("vistsmtp.visteon.com", 25, true);

                    services.AddHostedService<TimedService>(p => new TimedService(args, mailService, configuration,loggerService));


                });
    }
}
