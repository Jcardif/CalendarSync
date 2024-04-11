using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Services.GoogleCloudConsole;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static CalendarSync.Helpers.Helpers;

namespace CalendarSync.Functions;

public class DeleteCalendarEvent
{
    private readonly ILogger _logger;

    public DeleteCalendarEvent(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<DeleteCalendarEvent>();
    }

    public AppSettings? MyAppSettings { get; set; }
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
        if (string.IsNullOrEmpty(workAccEventId))
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.BadRequest, "No event id was passed");

            return response;
        }

        // Get app settings
        MyAppSettings = GetAppSettings();

        // confirm that the app settings were retrieved
        if (MyAppSettings is null
            || string.IsNullOrEmpty(MyAppSettings.ConnectionString)
            || string.IsNullOrEmpty(MyAppSettings.KeyVaultUri)
            || string.IsNullOrEmpty(MyAppSettings.KeyVaultSecretName)
            || string.IsNullOrEmpty(MyAppSettings.CalendarId))
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
        var calendarEvent = context.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

        // check if the calendar event was found
        if (calendarEvent == null || calendarEvent.PersonalAccEventId == null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.NotFound, "Event not found", workAccEventId);

            return response;
        }

        // delete the calendar event from google calendar
        await GoogleCalendarService.DeleteEvent(calendarEvent.PersonalAccEventId, MyAppSettings.CalendarId);

        // delete the calendar event from the database
        context.CalendarEvents?.Remove(calendarEvent);
        context.SaveChanges();


        response =await req.CreateFunctionReturnResponseAsync(HttpStatusCode.OK, "Event deleted", true);

        return response;
    }
}