using CalendarSync.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using Microsoft.Azure.KeyVault;
using Microsoft.Azure.Services.AppAuthentication;

namespace CalendarSync.Services.GoogleCloudConsole;

public class GoogleCalendarService
{
    // Calendar service property
    public CalendarService? CalendarService { get; set; }

    // Get the private key file from azure vault
    private async Task<string> GetPrivateKeyFileAsync(string keyVaultUrl, string secretName)
    {
        // Authenticate the request using a service principal or a managed identity
        var azureServiceTokenProvider = new AzureServiceTokenProvider();
        var accessToken = await azureServiceTokenProvider.GetAccessTokenAsync("https://vault.azure.net");

        // Create the Key Vault client
        var keyVaultClient = new KeyVaultClient(
            (authority, resource, scope) =>
                azureServiceTokenProvider.KeyVaultTokenCallback(authority, resource, scope));

        // Get the secret value
        var secret = await keyVaultClient.GetSecretAsync(keyVaultUrl, secretName);

        return secret.Value;
    }

    // Authenticate to Google Cloud and get an access token for Google Calendar
    public async Task<CalendarService?> AuthenticateGoogleCloudAsync(string keyVaultUrl, string secretName)
    {
        // Get the private key file from azure vault
        var privateKeyFile = await GetPrivateKeyFileAsync(keyVaultUrl, secretName);

        // Use the Google .NET Client Library to make the API request
        string[] scopes = { CalendarService.Scope.Calendar };

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