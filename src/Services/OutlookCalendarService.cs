using System.Net.Http.Headers;
using CalendarSync.Models;
using Microsoft.Graph;
using Microsoft.Identity.Client;

namespace CalendarSync.Services;

public class OutlookCalendarService
{
    public GraphServiceClient? GraphClient { get; set; }


    // Authenticate to Azure AD and get an access token for Microsoft Graph
    public async Task<bool> AuthenticateAzureAdAsync(string clientId, string clientSecret, string tenantId)
    {
        // Create a new confidential client application
        var confidentialClientApplication = ConfidentialClientApplicationBuilder
            .Create(clientId)
            .WithClientSecret(clientSecret)
            .WithAuthority(AzureCloudInstance.AzurePublic, tenantId)
            .Build();

        // Request the Calendars.ReadWrite scope
        string[] scopes = { "https://graph.microsoft.com/.default" };

        // Acquire an access token for the user
        var authResult = await confidentialClientApplication.AcquireTokenForClient(scopes)
            .ExecuteAsync();

        // Check if the token was acquired successfully
        if (authResult == null) return false;

        // Initialize the GraphServiceClient class with the access token
        GraphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
            requestMessage =>
            {
                requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", authResult.AccessToken);
                return Task.CompletedTask;
            }));

        return true;
    }

    // Add a new event to the user's calendar
    public async Task<Event?> CreateNewEvent(CalendarEvent calendarEvent, string userPrincipalName)
    {
        // Create a new event
        var newEvent = new Event
        {
            Subject = calendarEvent.Subject,
            Start = new DateTimeTimeZone
            {
                DateTime = calendarEvent.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = calendarEvent.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },

            Body = new ItemBody
            {
                Content = calendarEvent.Body,
                ContentType = BodyType.Text
            }
        };

        // return if graph client is null
        if (GraphClient is null)
            return null;

        // Add the event to the user's calendar
        var user = await GraphClient.Users["josh.cardif@outlook.com"].Events.Request().GetAsync();
        var result = await GraphClient.Users[userPrincipalName].Events.Request().AddAsync(newEvent);

        return result;
    }

    // Delete an event from the user's calendar
    public async Task DeleteEvent(string eventId)
    {
        if (GraphClient is null)
            return;

        await GraphClient.Me.Events[eventId].Request().DeleteAsync();
    }

    // update an event in the user's calendar
    public async Task<Event?> UpdateEvent(string eventId, CalendarEvent calendarEvent)
    {
        // Create a new event
        var newEvent = new Event
        {
            Subject = calendarEvent.Subject,
            Start = new DateTimeTimeZone
            {
                DateTime = calendarEvent.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },
            End = new DateTimeTimeZone
            {
                DateTime = calendarEvent.EndTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                TimeZone = "UTC"
            },

            Body = new ItemBody
            {
                Content = calendarEvent.Body,
                ContentType = BodyType.Text
            }
        };

        // return if graph client is null
        if (GraphClient is null)
            return null;


        // update the event to the user's calendar
        var result = await GraphClient.Me.Events[eventId].Request().UpdateAsync(newEvent);

        return result;
    }
}