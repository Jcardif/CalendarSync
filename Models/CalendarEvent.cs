using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CalendarSync.Models
{
    public class CalendarEvent
    {
        private string _id;

        [Key]
        public string? Id 
        {
             get
             {
                return _id;
             }

             set
             {
                WorkAccEventId=value;
                _id = CreateHash($"{Subject}{WorkAccEventId}{StartTime}{EndTime}");
             }
        }
        public string WorkAccEventId { get; set; }
        public string? PersonalAccEventId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTimeOffset StartTimeWithTimeZone { get; set; }
        public DateTime EndTime { get; set; }
        public DateTimeOffset EndTimeWithTimeZone { get; set; }
        public string? Subject { get; set; }
        public string? Importance { get; set; }

        
        public string CreateHash(string input)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
                return hash.ToString();
            }
        }


    }
}