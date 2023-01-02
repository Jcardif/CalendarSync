using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System.IO;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CalendarSync.Data;
using CalendarSync.Models;

namespace CalendarSync.Functions
{
    public class SyncNewEvents
    {
        private readonly ILogger _logger;

        public SyncNewEvents(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SyncNewEvents>();
        }

        [Function("SyncNewEvents")]
        public HttpResponseData Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            HttpResponseData response;

            // Read the values for the new row from the body of the request
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            var calendarEvent = JsonConvert.DeserializeObject<CalendarEvent>(requestBody);

            if(calendarEvent is null || String.IsNullOrEmpty(calendarEvent.Id))
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.BadRequest, "CalendarEvent is null");

                return response;
            }

            // Use EF Core to insert a record into the table
            using (var context = new AppDbContext())
            {
                // Apply any pending migrations
                context.Database.Migrate();
                context.Database.EnsureCreated();

                context.CalendarEvents?.Add(calendarEvent);
                context?.SaveChanges();
            }

            // get response from helper method
            response = CreateResponse(req, HttpStatusCode.OK, "CalendarEvent added successfully");

            return response;
        }

        private HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            // add json content type to the response
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // write the error message to the response body
            response.WriteString(JsonConvert.SerializeObject(new { message =$"{message}"}));
            return response;
        }
    }
}
