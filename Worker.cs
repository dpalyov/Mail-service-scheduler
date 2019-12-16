// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.Logging;

// namespace MailServiceWorker
// {
//     public class Worker : BackgroundService
//     {
//         private readonly IHostedService _hostedService;
//         private readonly ILogger<Worker> _logger;
//         public Worker(ILogger<Worker> logger, IHostedService hostedService)
//         {
//             _hostedService = hostedService;
//             _logger = logger;
//         }

//         protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//         {
//             while (!stoppingToken.IsCancellationRequested)
//             {
//                 _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
//                 await _hostedService.StartAsync(stoppingToken);
//                 //await Task.Delay(1000, stoppingToken);

//                 if(stoppingToken.IsCancellationRequested)
//                 {
//                     _logger.LogInformation("Stopping the worker...");
//                     await _hostedService.StopAsync(stoppingToken);
//                 }
//             }

           
//         }
//     }
// }
