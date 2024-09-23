using System.Net;
using System.Web;
using CalendarSync.Data;
using CalendarSync.Helpers;
using CalendarSync.Models;
using CalendarSync.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CalendarSync.Utils.Constants;

namespace CalendarSync.Functions;

public class SyncNewEvents(ILoggerFactory loggerFactory, AppDbContext context, GoogleCalendarService googleCalendarService, KeyVaultService keyVaultService)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<SyncNewEvents>();
    private List<CalendarEvent>? _calendarEvents;

    [Function("SyncNewEvents")]
    public async Task<HttpResponseData> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request.");

        HttpResponseData response;

        // Get query string parameters
        var query = HttpUtility.ParseQueryString(req.Url.Query);

        var bodyType = query["BodyType"];

        // Read the values for the new row from the body of the request
        var requestBody = await new StreamReader(req.Body).ReadToEndAsync();

        try
        {
            if (bodyType == "list")
            {
                _calendarEvents = JsonConvert.DeserializeObject<List<CalendarEvent>>(requestBody);

                // check if list is null or empty
                if (_calendarEvents is null || _calendarEvents.Count == 0)
                {
                    // get response from helper method
                    response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.BadRequest, "No events were passed");

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
                    response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.BadRequest, "No event was passed");

                    return response;
                }

                _calendarEvents = new List<CalendarEvent> { calendarEvent };
            }
        }


        catch (Exception ex)
        {
            // get response from helper method
            response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.BadRequest, ex.Message);

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

        // Use EF Core to insert a record into the table 
        var cEvents = new List<object>();

        foreach (var calendarEvent in _calendarEvents)
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
            
            await context.CalendarEvents.AddAsync(calendarEvent);
            await context.SaveChangesAsync();

            // do no add viva generated appointments 
            if (calendarEvent.Subject.Contains("Take a break") ||
                calendarEvent.Subject.Contains("Catch up on messages"))
                continue;

            calendarEvent.Body = calendarEvent.Subject.Contains("Focus time") ? "#focustime" : "#meeting";

            // get calendar id from key vault
            var calendarId = await keyVaultService.GetSecretAsync(GOOGLE_CALENDAR_ID_SECRET_NAME);

            // create event in user's calendar
            var newEvent = await googleCalendarService.CreateNewEventAsync(calendarEvent, calendarId);

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
                await context.SaveChangesAsync();
            }

            cEvents.Add(new
            {
                calendarEvent = existingCalendarEvent,
                googleCalendarEvent = newEvent,
                message = "CalendarEvent created"
            });
        }

        // return the new event
        response = await req.CreateFunctionReturnResponseAsync(HttpStatusCode.OK, "Successfully Executed", cEvents);
        return response;
    }
}