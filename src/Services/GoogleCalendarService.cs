using CalendarSync.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;

namespace CalendarSync.Services;

public class GoogleCalendarService
{
    // Calendar service property
    public CalendarService? CalendarService { get; private set; }


    public CalendarService? AuthenticateAsync(string privateKeyFile)
    {
        // Use the Google .NET Client Library to make the API request
        string[] scopes = [CalendarService.Scope.Calendar];

        var credential = GoogleCredential.FromJson(privateKeyFile)
            .CreateScoped(scopes);


        // Create the Calendar service using the service account credential
        CalendarService = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "CalendarSyncService"
        });

        // if the credential is null, the authentication failed
        return credential is null ? null : CalendarService;
    }

    // Add a new event to the user's calendar
    public async Task<Event?> CreateNewEventAsync(CalendarEvent calendarEvent, string calendarId)
    {
        // return if calendar service is null
        if (CalendarService is null)
            return null;

        // Insert the new event into the user's calendar
        var calendar = await CalendarService.Calendars.Get(calendarId).ExecuteAsync();

        // Create a new event
        var newEvent = new Event
        {
            Summary = calendarEvent.Subject,
            Start = new EventDateTime
            {
                DateTime = new DateTime(calendarEvent.StartTimeWithTimeZone.Ticks, DateTimeKind.Utc)
            },
            End = new EventDateTime
            {
                DateTime = new DateTime(calendarEvent.EndTimeWithTimeZone.Ticks, DateTimeKind.Utc)
            },

            Description = calendarEvent.Body
        };

        var request = CalendarService.Events.Insert(newEvent, calendar.Id);
        var createdEvent = await request.ExecuteAsync();

        // return the created event
        return createdEvent;
    }

    // Delete an event from the user's calendar
    public async Task DeleteEvent(string eventId, string calendarId)
    {
        // return if calendar service is null
        if (CalendarService is null)
            return;

        // Delete the event from the user's calendar
        var request = await CalendarService.Events.Delete(calendarId, eventId).ExecuteAsync();
    }

    // Update an event in the user's calendar
    public async Task<Event?> UpdateEventAsync(CalendarEvent calendarEvent, string calendarId)
    {
        // return if calendar service is null
        if (CalendarService is null)
            return null;

        // Insert the new event into the user's calendar
        var calendar = CalendarService.Calendars.Get(calendarId).Execute();

        // Get the event from the user's calendar
        var eventToUpdate = CalendarService.Events.Get(calendarId, calendarEvent.PersonalAccEventId).Execute();

        // return if the event is null
        if (eventToUpdate is null)
            return null;

        // Update the event
        eventToUpdate.Summary = calendarEvent.Subject;
        eventToUpdate.Start = new EventDateTime
        {
            DateTime = new DateTime(calendarEvent.StartTimeWithTimeZone.Ticks, DateTimeKind.Utc)
        };
        eventToUpdate.End = new EventDateTime
        {
            DateTime = new DateTime(calendarEvent.EndTimeWithTimeZone.Ticks, DateTimeKind.Utc)
        };
        eventToUpdate.Description = calendarEvent.Body;

        // Update the event in the user's calendar
        var request = CalendarService.Events.Update(eventToUpdate, calendarId, eventToUpdate.Id);
        var updatedEvent = await request.ExecuteAsync();

        // return the updated event
        return updatedEvent;
    }
}