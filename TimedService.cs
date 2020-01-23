using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MailService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MailServiceWorker
{
    public class TimedService : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly ILogger<TimedService> _logger;

        private IMailService _mailService;

        private IConfigurationRoot _configuration;

        public TimedService(IMailService mailService, IConfigurationRoot configuration, ILogger<TimedService> logger)
        {
            _logger = logger;
            _configuration = configuration;
            _mailService = mailService;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }


        /// <summary>
        /// Starting the service and registering a timer with the configured time interval,
        /// basically saying at what interval the service will run and query the Email register.
        ///  </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {

            _logger.LogInformation($"Mail Service doing background work.");
            var interval = 0;
            try
            {
                int.TryParse(_configuration.GetSection("HoursInterval").Value, out interval);
            }
            catch (InvalidCastException ex)
            {
                _logger.LogError(ex.Message);
            }

            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(interval));
            return Task.CompletedTask;
        }
        /// <summary>
        /// Stopping the service
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StopAsync(CancellationToken cancellationToken)
        {

            _logger.LogInformation("Mail service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }
        /// <summary>
        /// Method that passes email from a given collection through the smtp server
        /// </summary>
        /// <param name="state"></param>
        /// <returns>Void</returns>
        private async void DoWork(object state)
        {

            var emails = _mailService.ReadEmails();
            //build the message body
            try
            {
                foreach (var e in emails)
                {
                    var isDue = _mailService.IsDue(e.LastNotificationDate, e.ReminderInterval);

                    if (e.MessageBody != "" && e.From != "" && e.To != "" && isDue)
                    {
                        _mailService.ConfigureMessage(e);

                        //add logging options
                        var logging = new Dictionary<string, string>
                        {
                            {"To",e.To},
                            {"Subject",e.Subject},

                        };

                        //send the email
                        await _mailService.SendMessageAsync(logging);

                        //update the last notification date
                        e.LastNotificationDate = DateTime.Now;
                        _mailService.UpdateEmail(e);
                        // Environment.Exit(0);

                    }
                }



            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                Environment.Exit(1);
            }


        }
        // /// <summary>
        // ///     Method that return either static or dynamic HTML
        // /// </summary>
        // /// <param name="useStatic" typeparam="bool"></param>
        // /// <param name="configuration" typeparam="IConfiguration"></param>
        // /// <returns>String containing HTML markup</returns>
        // private string BuildHtml(bool useStatic)
        // {

        //     if (useStatic != true)
        //     {

        //         var connStr = _configuration.GetSection("ConnectionString").Value;
        //         var queryCollection = Enumerable.Reverse(_configuration.GetSection("DataQuery").AsEnumerable());
        //         var sb = new StringBuilder();

        //         foreach (var kv in queryCollection)
        //         {
        //             sb.AppendLine(kv.Value);
        //         }

        //         var result = ExecuteQuery(connString: connStr, query: sb.ToString());
        //         sb.Clear();

        //         if (result != null)
        //         {
        //             var htmlTemplate = Enumerable.Reverse(_configuration.GetSection("HtmlTemplate").AsEnumerable());
        //             foreach (var kv in htmlTemplate)
        //             {
        //                 sb.AppendLine(kv.Value);
        //             }

        //             var headers = "";
        //             var body = "";

        //             for (var i = 0; i < result.Columns.Count; i++)
        //             {
        //                 headers += $"<th style='text-align:left'>{result.Columns[i]}</th>\n";
        //             };

        //             foreach (DataRow row in result.Rows)
        //             {
        //                 body += "<tr>\n";

        //                 for (var i = 0; i < result.Columns.Count; i++)
        //                 {
        //                     body += $"<td>{row.ItemArray[i]}</td>\n";
        //                 };

        //                 body += "</tr>\n";

        //             };

        //             sb = sb.Replace("{headers}", headers)
        //                    .Replace("{body}", body);

        //         }

        //         return sb.ToString();
        //     }
        //     else
        //     {
        //         return _configuration.GetSection("StaticHtml").Value;
        //     }

        // }
        // /// <summary>
        // ///     Method that queries a database and returns the result as Datatable
        // /// </summary>
        // /// <param name="connString" typeparam="string"></param>
        // /// <param name="query" typeparam="string"></param>
        // /// <returns>Datatable</returns>
        // private DataTable ExecuteQuery(string connString, string query)
        // {

        //     try
        //     {
        //         using (var connection = new SqlConnection(connString))
        //         {
        //             var command = new SqlCommand(query, connection);
        //             command.Connection.Open();

        //             var dt = new DataTable();

        //             using (var adapter = new SqlDataAdapter(command))
        //             {
        //                 adapter.Fill(dt);
        //             };

        //             return dt;
        //         }

        //     }
        //     catch (SqlException ex)
        //     {
        //         _logger.LogError($"SQL exception: {ex.Message}");
        //         Environment.Exit(2);
        //     }

        //     return null;


        // }


    }
}