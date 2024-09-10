using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using static CalendarSync.Utils.Constants;

namespace CalendarSync.Functions;

public class DeleteCalendarEvent(ILoggerFactory loggerFactory, AppDbContext context, GoogleCalendarService googleCalendarService, KeyVaultService keyVaultService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<DeleteCalendarEvent>();


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

        // Check if the token was acquired successfully
        if (googleCalendarService.CalendarService is null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.Unauthorized,
                "Unable to authenticate to Google Cloud Console");

            return response;
        }


        // get the calendar event from the database
        var calendarEvent = context.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

        // check if the calendar event was found
        if (calendarEvent == null || calendarEvent.PersonalAccEventId == null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.NotFound, "Event not found", workAccEventId);

            return response;
        }
        
        // get the calendar id from the key vault
        var calendarId = await keyVaultService.GetSecretAsync(GOOGLE_CALENDAR_ID_SECRET_NAME);

        // delete the calendar event from google calendar
        await googleCalendarService.DeleteEvent(calendarEvent.PersonalAccEventId, calendarId);

        // delete the calendar event from the database
        context.CalendarEvents?.Remove(calendarEvent);
        await context.SaveChangesAsync();


        response =await req.CreateFunctionReturnResponseAsync(HttpStatusCode.OK, "Event deleted", true);

        return response;
    }
}