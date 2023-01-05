using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace CalendarSync.Functions
{
    public class DeleteCalendarEvent
    {
        private readonly ILogger _logger;

        public DeleteCalendarEvent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteCalendarEvent>();
        }

        [Function("DeleteCalendarEvent")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "text/plain; charset=utf-8");

            return response;
        }
    }
}
