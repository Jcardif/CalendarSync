using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace CalendarSync.Models
{
    public class CalendarEvent
    {
        private string? _id;
        private string? _start;
        private string? _end;
        private string? _startWithTimeZone;
        private string? _endWithTimeZone;


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

        [Required]
        public string? WorkAccEventId { get; set; }
        public string? PersonalAccEventId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTimeOffset StartTimeWithTimeZone { get; set; }
        public DateTime EndTime { get; set; }
        public DateTimeOffset EndTimeWithTimeZone { get; set; }
        public string? Subject { get; set; }
        public string? Importance { get; set; }

        [NotMapped]
        public string? Start
        {
            get { return _start; }
            set
            {
                _start = value;
                if (!string.IsNullOrEmpty(_start) && DateTime.TryParse(_start, out DateTime startTime))
                {
                    StartTime = startTime;
                }
            }
        }

        [NotMapped]
        public string? End
        {
            get { return _end; }
            set
            {
                _end = value;
                if (!string.IsNullOrEmpty(_end) && DateTime.TryParse(_end, out DateTime endTime))
                {
                    EndTime = endTime;
                }
            }
        }

        [NotMapped]
        public string? StartWithTimeZone
        {
            get { return _startWithTimeZone; }
            set
            {
                _startWithTimeZone = value;
                if (!string.IsNullOrEmpty(_startWithTimeZone) && DateTimeOffset.TryParse(_startWithTimeZone, out DateTimeOffset startTimeWithTimeZone))
                {
                    StartTimeWithTimeZone = startTimeWithTimeZone;
                }
            }
        }

        [NotMapped]
        public string? EndWithTimeZone
        {
            get { return _endWithTimeZone; }
            set
            {
                _endWithTimeZone = value;
                if (!string.IsNullOrEmpty(_endWithTimeZone) && DateTimeOffset.TryParse(_endWithTimeZone, out DateTimeOffset endTimeWithTimeZone))
                {
                    EndTimeWithTimeZone = endTimeWithTimeZone;
                }
            }
        }

        [NotMapped]
        public string? Body { get; set; }

        
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