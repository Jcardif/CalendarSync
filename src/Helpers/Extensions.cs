using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using Newtonsoft.Json;

namespace CalendarSync.Helpers
{
    public static class Extensions
    {
        public static HttpResponseData CreateFunctionReturnResponse(this HttpRequestData req, HttpStatusCode statusCode, string message, object? data = null)
        {
            var response = req.CreateResponse(statusCode);
            // add json content type to the response
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            // write the error message to the response body
            var responseMessage = new
            {
                Message = message,
                Data = data
            };
            response.WriteString(JsonConvert.SerializeObject(responseMessage));
            return response;
        }


    }
}