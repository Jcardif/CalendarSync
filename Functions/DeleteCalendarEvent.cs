using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Services.GoogleCloudConsole;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CalendarSync.Helpers;
using static CalendarSync.Helpers.Helpers;

namespace CalendarSync.Functions
{
    public class DeleteCalendarEvent
    {
        private readonly ILogger _logger;

        public DeleteCalendarEvent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DeleteCalendarEvent>();
        }

        public AppSettings MyAppSettings {get; set;}
        public GoogleCalendarService? GoogleCalendarService { get; private set; }
        

        [Function("DeleteCalendarEvent")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Get query string parameters
            var query = HttpUtility.ParseQueryString(req.Url.Query);

            HttpResponseData response;

            var workAccEventId = query["WorkAccEventId"];

            // check if query string parameter is null or empty
            if (String.IsNullOrEmpty(workAccEventId))
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.BadRequest, "No event id was passed");

                return response;
            }

            // Get app settings
            MyAppSettings =  GetAppSettings();

            // confilrm that the app settings were retrieved
            if (MyAppSettings is null
                || String.IsNullOrEmpty(MyAppSettings.ConnectionString)
                || String.IsNullOrEmpty(MyAppSettings.KeyVaultUri)
                || String.IsNullOrEmpty(MyAppSettings.KeyVaultSecretName)
                || String.IsNullOrEmpty(MyAppSettings.CalendarId))
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.InternalServerError, "App settings were not retrieved");

                return response;
            }

            // Authenticate to Google Cloud Console and get an access token for Google Calendar
            GoogleCalendarService = new GoogleCalendarService();
            var googleCalendarService = await GoogleCalendarService.AuthenticateGoogleCloudAsync(MyAppSettings.KeyVaultUri, MyAppSettings.KeyVaultSecretName);

            // Check if the token was acquired successfully
            if (googleCalendarService is null)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.Unauthorized, "Unable to authenticate to Google Cloud Console");

                return response;
            }


            // get the calendar event from the database
            var context = new AppDbContext();
            var calendarEvent = context?.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

            // check if the calendar event was found
            if (calendarEvent == null || calendarEvent.PersonalAccEventId == null)
            {
                // get response from helper method
                response = CreateResponse(req, HttpStatusCode.NotFound, "Event not found", workAccEventId);

                return response;
            }

            // delete the calendar event from google calendar
            await GoogleCalendarService.DeleteEvent(calendarEvent.PersonalAccEventId, MyAppSettings.CalendarId);

            // delete the calendar event from the database
            context?.CalendarEvents?.Remove(calendarEvent);
            context?.SaveChanges();


            response= CreateResponse(req, HttpStatusCode.OK, "Event deleted", true);

            return response;
        }

        private HttpResponseData CreateResponse(HttpRequestData req, HttpStatusCode statusCode, string message, object? data = null)
        {
            var response = req.CreateResponse(statusCode);
            // add json content type to the response
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // write the error message to the response body
            var responseMessage = new
            {
                Message = message,
                Data = data
            };
            response.WriteString(JsonConvert.SerializeObject(responseMessage));
            return response;
        }


    }
}
