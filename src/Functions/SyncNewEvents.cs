using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Models;
using CalendarSync.Service.AzureAD;
using CalendarSync.Services.GoogleCloudConsole;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CalendarSync.Helpers.Helpers;

namespace CalendarSync.Functions;

public class SyncNewEvents
{
    private readonly ILogger _logger;
    private List<CalendarEvent>? calendarEvents;

    public SyncNewEvents(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<SyncNewEvents>();
    }

    public OutlookCalendarService? OutlookCalendarService { get; set; }
    public GoogleCalendarService? GoogleCalendarService { get; set; }

    public AppSettings? MyAppSettings { get; set; }

    [Function("SyncNewEvents")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        HttpResponseData response;

        // Get query string parameters
        var query = HttpUtility.ParseQueryString(req.Url.Query);

        var bodyType = query["BodyType"];

        // Read the values for the new row from the body of the request
        var requestBody = new StreamReader(req.Body).ReadToEnd();

        try
        {
            if (bodyType == "list")
            {
                calendarEvents = JsonConvert.DeserializeObject<List<CalendarEvent>>(requestBody);

                // check if list is null or empty
                if (calendarEvents is null || calendarEvents.Count == 0)
                {
                    // get response from helper method
                    response = req.CreateFunctionReturnResponse(HttpStatusCode.BadRequest, "No events were passed");

                    return response;
                }
            }

            else
            {
                var calendarEvent = JsonConvert.DeserializeObject<CalendarEvent>(requestBody);

                // check if event is null
                if (calendarEvent is null)
                {
                    // get response from helper method
                    response = req.CreateFunctionReturnResponse(HttpStatusCode.BadRequest, "No event was passed");

                    return response;
                }

                calendarEvents = new List<CalendarEvent> { calendarEvent };
            }
        }


        catch (Exception ex)
        {
            // get response from helper method
            response = req.CreateFunctionReturnResponse(HttpStatusCode.BadRequest, ex.Message);

            return response;
        }


        // Get app settings
        MyAppSettings = GetAppSettings();

        // confirm that the app settings were retrieved
        if (MyAppSettings is null
            || string.IsNullOrEmpty(MyAppSettings.ConnectionString)
            || string.IsNullOrEmpty(MyAppSettings.ClientId)
            || string.IsNullOrEmpty(MyAppSettings.ClientSecret)
            || string.IsNullOrEmpty(MyAppSettings.TenantId)
            || string.IsNullOrEmpty(MyAppSettings.KeyVaultSecretName)
            || string.IsNullOrEmpty(MyAppSettings.KeyVaultUri)
            || string.IsNullOrEmpty(MyAppSettings.CalendarId)
            || string.IsNullOrEmpty(MyAppSettings.UserPrincipalName))
        {
            // get response from helper method
            response = req.CreateFunctionReturnResponse(HttpStatusCode.BadRequest, "App settings are missing");

            return response;
        }

        // Authenticate to Azure AD and get an access token for Microsoft Graph
        OutlookCalendarService = new OutlookCalendarService();
        var authenticated = await OutlookCalendarService.AuthenticateAzureAdAsync(MyAppSettings.ClientId,
            MyAppSettings.ClientSecret, MyAppSettings.TenantId);

        // Check if the token was acquired successfully
        if (!authenticated)
        {
            // get response from helper method
            response = req.CreateFunctionReturnResponse(HttpStatusCode.Unauthorized,
                "Unable to authenticate to Azure AD");

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
            response = req.CreateFunctionReturnResponse(HttpStatusCode.Unauthorized,
                "Unable to authenticate to Google Cloud Console");

            return response;
        }

        // Use EF Core to insert a record into the table & Apply any pending migrationn
        var context = new AppDbContext();

        context.Database.Migrate();
        context.Database.EnsureCreated();

        var cEvents = new List<object>();

        foreach (var calendarEvent in calendarEvents)
        {
            var existingCalendarEvent = context?.CalendarEvents?.Find(calendarEvent.Id);
            if (existingCalendarEvent != null)
            {
                cEvents.Add(new
                {
                    id = calendarEvent.Id,
                    message = "Calendar Event already exists"
                });
                continue;
            }

            context?.CalendarEvents?.Add(calendarEvent);
            context?.SaveChanges();

            calendarEvent.Body = calendarEvent.Subject.Contains("Focus time") ? "#focustime" : "#meeting";

            // create event in user's calendar
            var newEvent = await GoogleCalendarService.CreateNewEventAsync(calendarEvent, MyAppSettings.CalendarId);

            // check if event was created successfully
            if (newEvent is null)
            {
                cEvents.Add(new
                {
                    calendarEvent,
                    message = "Calendar Event was not created in Google Calendar"
                });

                continue;
            }

            // update database with google calendar event id
            existingCalendarEvent = context?.CalendarEvents?.Find(calendarEvent.Id);
            if (existingCalendarEvent != null)
            {
                existingCalendarEvent.PersonalAccEventId = newEvent.Id;
                context?.SaveChanges();
            }

            cEvents.Add(new
            {
                calendarEvent = existingCalendarEvent,
                googleCalendarEvent = newEvent,
                message = "CalendarEvent created"
            });
        }

        // return the new event
        response = req.CreateFunctionReturnResponse(HttpStatusCode.OK, "Successfully Executed", cEvents);
        return response;
    }
}