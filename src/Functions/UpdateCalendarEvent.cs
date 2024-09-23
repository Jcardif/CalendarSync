using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Models;
using CalendarSync.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CalendarSync.Utils.Constants;

namespace CalendarSync.Functions;

public class UpdateCalendarEvent(ILoggerFactory loggerFactory, AppDbContext context, KeyVaultService keyVaultService, GoogleCalendarService googleCalendarService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<UpdateCalendarEvent>();

    private GoogleCalendarService? GoogleCalendarService { get; set; }

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
        
        // Authenticate to Google Cloud Console
        var privateKeyFile = await keyVaultService.GetSecretAsync(GOOGLE_CALENDAR_PRIVATE_KEY_SECRET_NAME);
        var calendarService = googleCalendarService.AuthenticateAsync(privateKeyFile);

        // Check if the token was acquired successfully
        if (calendarService is null)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.Unauthorized,
                "Unable to authenticate to Google Cloud Console");

            return response;
        }
        
        // get the calendar id from key vault
        var calendarId = await keyVaultService.GetSecretAsync(GOOGLE_CALENDAR_ID_SECRET_NAME);

        // get the calendar event from the database
        var existingCalendarEvent = context.CalendarEvents?.FirstOrDefault(e => e.WorkAccEventId == workAccEventId);

        // check if the calendar event was found
        if (existingCalendarEvent is null || existingCalendarEvent.PersonalAccEventId == null)
        {
            // add event to database
            var calEvent=await context.CalendarEvents.AddAsync(calendarEvent);
            await context.SaveChangesAsync();
            
            // add event to Google Calendar
            if (calendarEvent.Subject.Contains("Take a break") ||
                calendarEvent.Subject.Contains("Catch up on messages")) ;
            
            calendarEvent.Body = calendarEvent.Subject.Contains("Focus time") ? "#focustime" : "#meeting";
            
            var newEvent = await googleCalendarService.CreateNewEventAsync(calendarEvent, calendarId);

            if (newEvent == null)
            {
                response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.InternalServerError,
                    "Calendar Event was not created in Google Calendar");
            }
            
            // update database with Google calendar event id
            if (newEvent != null) calEvent.Entity.PersonalAccEventId = newEvent.Id;
            await context.SaveChangesAsync();

            return await req.CreateFunctionReturnResponseAsync(HttpStatusCode.Created,
                "Calendar event created successfully");
        }

        // update the calendar in the database
        existingCalendarEvent.Start = calendarEvent.Start;
        existingCalendarEvent.End = calendarEvent.End;
        existingCalendarEvent.StartTimeWithTimeZone = calendarEvent.StartTimeWithTimeZone;
        existingCalendarEvent.EndTimeWithTimeZone = calendarEvent.EndTimeWithTimeZone;
        existingCalendarEvent.Subject = calendarEvent.Subject;
        existingCalendarEvent.Importance = calendarEvent.Importance;
        existingCalendarEvent.Body = calendarEvent.Subject.Contains("Focus time") ? "#focustime" : "#meeting";

        context.CalendarEvents?.Update(existingCalendarEvent);
        await context.SaveChangesAsync();

        // update the calendar event in Google Calendar
        var updatedCalendarEvent =
            await GoogleCalendarService.UpdateEventAsync(existingCalendarEvent, calendarId);

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