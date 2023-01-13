using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Models;
using CalendarSync.Services.GoogleCloudConsole;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CalendarSync.Helpers.Helpers;
using CalendarSync.Helpers;
using static CalendarSync.Helpers.Extensions;

namespace CalendarSync.Functions
{
    public class UpdateCalendarEvent
    {
        private readonly ILogger _logger;

        public UpdateCalendarEvent(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<UpdateCalendarEvent>();
        }

        public GoogleCalendarService? GoogleCalendarService { get; private set; }

        public AppSettings? MyAppSettings { get; private set; }

        [Function("UpdateCalendarEvent")]
        public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            // Get query string parameters
            var query = HttpUtility.ParseQueryString(req.Url.Query);

            HttpResponseData response;

            var workAccEventId = query["WorkAccEventId"];

            // Read the values for the new row from the body of the request
            string requestBody = new StreamReader(req.Body).ReadToEnd();

            var calendarEvent = JsonConvert.DeserializeObject<CalendarEvent>(requestBody);

            // check if valid calendar event has been passed
            if (calendarEvent is null || string.IsNullOrEmpty(calendarEvent.WorkAccEventId))
            {
                // get response from helper method
                response = req.CreateFunctionReturnResponse(HttpStatusCode.BadRequest, "No valid calendar event was passed");

                return response;
            }

            // Get app settings
            MyAppSettings = GetAppSettings();

            // confilrm that the app settings were retrieved
            if (MyAppSettings is null || string.IsNullOrEmpty(MyAppSettings.ConnectionString) || string.IsNullOrEmpty(MyAppSettings.KeyVaultUri) || string.IsNullOrEmpty(MyAppSettings.KeyVaultSecretName) || string.IsNullOrEmpty(MyAppSettings.CalendarId))
            {
                // get response from helper method
                response = req.CreateFunctionReturnResponse(HttpStatusCode.InternalServerError, "App settings were not retrieved");

                return response;
            }

            // Authenticate to Google Cloud Console and get an access token for Google Calendar
            GoogleCalendarService = new GoogleCalendarService();
            var googleCalendarService = await GoogleCalendarService.AuthenticateGoogleCloudAsync(MyAppSettings.KeyVaultUri, MyAppSettings.KeyVaultSecretName);

            // Check if the token was acquired successfully
            if (googleCalendarService is null)
            {
                // get response from helper method
                response = req.CreateFunctionReturnResponse(HttpStatusCode.Unauthorized, "Unable to authenticate to Google Cloud Console");

                return response;
            }

            // get the calendar event from the database
            var context = new AppDbContext();
            var existingCalendarEvent = context?.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

            // check if the calendar event was found
            if (existingCalendarEvent is null || existingCalendarEvent.PersonalAccEventId == null)
            {
                // get response from helper method
                response = req.CreateFunctionReturnResponse(HttpStatusCode.NotFound, "Event not found", workAccEventId);
                return response;
            }

            // update the calendar in the database
            existingCalendarEvent.Start= calendarEvent.Start;
            existingCalendarEvent.End = calendarEvent.End;
            existingCalendarEvent.StartTimeWithTimeZone = calendarEvent.StartTimeWithTimeZone;
            existingCalendarEvent.EndTimeWithTimeZone = calendarEvent.EndTimeWithTimeZone;
            existingCalendarEvent.Subject = calendarEvent.Subject;
            existingCalendarEvent.Importance = calendarEvent.Importance;

            context?.CalendarEvents?.Update(existingCalendarEvent);
            context?.SaveChanges();

            // update the calendar event in Google Calendar
            var updatedCalendarEvent = await GoogleCalendarService.UpdateEventAsync(existingCalendarEvent, MyAppSettings.CalendarId);

            // check if the calendar event was updated successfully
            if (updatedCalendarEvent is null)
            {
                // get response from helper method
                response = req.CreateFunctionReturnResponse(HttpStatusCode.InternalServerError, "Unable to update calendar event", workAccEventId);
                return response;
            }

            response = req.CreateFunctionReturnResponse(HttpStatusCode.OK, "Calendar event updated successfully", new
             {
                calendarEvent = updatedCalendarEvent,
                googleCalendarEvent = updatedCalendarEvent
             });


            return response;
        }
    }
}
