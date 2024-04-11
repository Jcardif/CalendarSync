using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Models;
using CalendarSync.Services.GoogleCloudConsole;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CalendarSync.Helpers.Helpers;

namespace CalendarSync.Functions;

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
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        var calendarEvent = JsonConvert.DeserializeObject<CalendarEvent>(requestBody);

        // check if valid calendar event has been passed
        if (calendarEvent is null || string.IsNullOrEmpty(calendarEvent.WorkAccEventId))
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.BadRequest,
                "No valid calendar event was passed");

            return response;
        }

        // Get app settings
        MyAppSettings = GetAppSettings();

        // confirm that the app settings were retrieved
        if (MyAppSettings is null || string.IsNullOrEmpty(MyAppSettings.ConnectionString) ||
            string.IsNullOrEmpty(MyAppSettings.KeyVaultUri) || string.IsNullOrEmpty(MyAppSettings.KeyVaultSecretName) ||
            string.IsNullOrEmpty(MyAppSettings.CalendarId))
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.InternalServerError,
                "App settings were not retrieved");

            return response;
        }

        // Authenticate to Google Cloud Console and get an access token for Google Calendar
        GoogleCalendarService = new GoogleCalendarService();
        var googleCalendarService =
            await GoogleCalendarService.AuthenticateGoogleCloudAsync(MyAppSettings.KeyVaultUri,
                MyAppSettings.KeyVaultSecretName);

        // Check if the token was acquired successfully
        if (googleCalendarService is null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.Unauthorized,
                "Unable to authenticate to Google Cloud Console");

            return response;
        }

        // get the calendar event from the database
        var context = new AppDbContext();
        var existingCalendarEvent = context?.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

        // check if the calendar event was found
        if (existingCalendarEvent is null || existingCalendarEvent.PersonalAccEventId == null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.NotFound, "Event not found", workAccEventId);
            return response;
        }

        // update the calendar in the database
        existingCalendarEvent.Start = calendarEvent.Start;
        existingCalendarEvent.End = calendarEvent.End;
        existingCalendarEvent.StartTimeWithTimeZone = calendarEvent.StartTimeWithTimeZone;
        existingCalendarEvent.EndTimeWithTimeZone = calendarEvent.EndTimeWithTimeZone;
        existingCalendarEvent.Subject = calendarEvent.Subject;
        existingCalendarEvent.Importance = calendarEvent.Importance;
        existingCalendarEvent.Body = calendarEvent.Subject.Contains("Focus time") ? "#focustime" : "#meeting";

        context?.CalendarEvents?.Update(existingCalendarEvent);
        context?.SaveChanges();

        // update the calendar event in Google Calendar
        var updatedCalendarEvent =
            await GoogleCalendarService.UpdateEventAsync(existingCalendarEvent, MyAppSettings.CalendarId);

        // check if the calendar event was updated successfully
        if (updatedCalendarEvent is null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.InternalServerError,
                "Unable to update calendar event", workAccEventId);
            return response;
        }

        response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.OK, "Calendar event updated successfully", new
        {
            calendarEvent = updatedCalendarEvent,
            googleCalendarEvent = updatedCalendarEvent
        });


        return response;
    }
}