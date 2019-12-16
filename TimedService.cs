using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
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
        private string[] _args;

        private IMailService _mailService;

        private IConfigurationRoot _configuration;

        public TimedService(string[] args, IMailService mailService, IConfigurationRoot configuration, ILogger<TimedService> logger)
        {
            _logger = logger;
            _args = args;
            _configuration = configuration;
            _mailService = mailService;
        }

        public void Dispose()
        {
            _timer.Dispose();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation($"Mail Service doing background work.");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(60));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Mail service is stopping.");
            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var useHtml = false;
            var useStaticHtml =  false;

            try
            {
                bool.TryParse(_configuration.GetSection("UseHtml").Value, out useHtml);
                bool.TryParse(_configuration.GetSection("UseStaticHtml").Value, out useStaticHtml);
            }
            catch (ArgumentException)
            {
                _logger.LogWarning("Missing or wrong argument value(s)...application will use defaults");
            }

            //build the message body
            try
            {
                var message = "";
                if (useHtml == true)
                {
                    message = BuildHtml(useStaticHtml);
                }
                else
                {
                    message = _configuration.GetSection("BodyText").Value;
                }

                //get the recepients and subject from the config
                var recepients = _configuration.GetSection("Recepients").Value;
                var subject = _configuration.GetSection("Subject").Value;
                var from = _configuration.GetSection("From").Value;

                if (message != "" && recepients != "" && from != "")
                {
                    //configure the message to send
                    _mailService.ConfigureMessage(recepients,
                                             _configuration.GetSection("From").Value,
                                             subject,
                                             message,
                                             useHtml);

                    //add logging options
                    var logging = new Dictionary<string, string>
                    {
                        {"Sent on",DateTime.UtcNow.ToString("dd-MMM-yyyy hh:mm:ss")},
                        {"To",recepients},
                        {"Subject",subject},

                    };

                    //send the email
                    _mailService.SendMessageSync(logging);
                    // Environment.Exit(0);

                }
                else
                {
                    _logger.LogError("Missing mandatory configuration or message is empty");
                    Environment.Exit(0);
                }


            }
            catch (NullReferenceException)
            {
                _logger.LogError("Possibly configuration file is not in the right format");
                Environment.Exit(1);
            }


        }
        /// <summary>
        ///     Method that return either static or dynamic HTML
        /// </summary>
        /// <param name="useStatic" typeparam="bool"></param>
        /// <param name="configuration" typeparam="IConfiguration"></param>
        /// <returns>String containing HTML markup</returns>
        private string BuildHtml(bool useStatic)
        {

            if (useStatic != true)
            {

                var connStr = _configuration.GetSection("ConnectionString").Value;
                var queryCollection = Enumerable.Reverse(_configuration.GetSection("DataQuery").AsEnumerable());
                var sb = new StringBuilder();

                foreach (var kv in queryCollection)
                {
                    sb.AppendLine(kv.Value);
                }

                var result = ExecuteQuery(connString: connStr, query: sb.ToString());
                sb.Clear();

                if (result != null)
                {
                    var htmlTemplate = Enumerable.Reverse(_configuration.GetSection("HtmlTemplate").AsEnumerable());
                    foreach (var kv in htmlTemplate)
                    {
                        sb.AppendLine(kv.Value);
                    }

                    var headers = "";
                    var body = "";

                    for (var i = 0; i < result.Columns.Count; i++)
                    {
                        headers += $"<th style='text-align:left'>{result.Columns[i]}</th>\n";
                    };

                    foreach (DataRow row in result.Rows)
                    {
                        body += "<tr>\n";

                        for (var i = 0; i < result.Columns.Count; i++)
                        {
                            body += $"<td>{row.ItemArray[i]}</td>\n";
                        };

                        body += "</tr>\n";

                    };

                    sb = sb.Replace("{headers}", headers)
                           .Replace("{body}", body);

                }

                return sb.ToString();
            }
            else
            {
                return _configuration.GetSection("StaticHtml").Value;
            }

        }
        /// <summary>
        ///     Method that queries a database and returns the result as Datatable
        /// </summary>
        /// <param name="connString" typeparam="string"></param>
        /// <param name="query" typeparam="string"></param>
        /// <returns>Datatable</returns>
        private DataTable ExecuteQuery(string connString, string query)
        {
            
            try
            {
                using (var connection = new SqlConnection(connString))
                {
                    var command = new SqlCommand(query, connection);
                    command.Connection.Open();

                    var dt = new DataTable();

                    using (var adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(dt);
                    };

                    return dt;
                }

            }
            catch (SqlException ex)
            {
                _logger.LogError($"SQL exception: {ex.Message}");
                Environment.Exit(2);
            }

            return null;


        }


    }
}