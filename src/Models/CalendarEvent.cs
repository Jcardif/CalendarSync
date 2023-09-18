using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;

namespace CalendarSync.Models;

public class CalendarEvent
{
    private string? _end;
    private string? _endWithTimeZone;
    private string _id = string.Empty;
    private string? _start;
    private string? _startWithTimeZone;


    [Key]
    public string Id
    {
        get => _id;

        set
        {
            WorkAccEventId = value;
            _id = CreateHash($"{Subject}{WorkAccEventId}{StartTime}{EndTime}");
        }
    }

    [Required] 
    public required string WorkAccEventId { get; set; }

    public string? PersonalAccEventId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTimeOffset StartTimeWithTimeZone { get; set; }
    public DateTime EndTime { get; set; }
    public DateTimeOffset EndTimeWithTimeZone { get; set; }
    [Required]
    public required string Subject { get; set; }
    public string? Importance { get; set; }

    [NotMapped]
    public string? Start
    {
        get => _start;
        set
        {
            _start = value;
            if (!string.IsNullOrEmpty(_start) && DateTime.TryParse(_start, out var startTime)) StartTime = startTime;
        }
    }

    [NotMapped]
    public string? End
    {
        get => _end;
        set
        {
            _end = value;
            if (!string.IsNullOrEmpty(_end) && DateTime.TryParse(_end, out var endTime)) EndTime = endTime;
        }
    }

    [NotMapped]
    public string? StartWithTimeZone
    {
        get => _startWithTimeZone;
        set
        {
            _startWithTimeZone = value;
            if (!string.IsNullOrEmpty(_startWithTimeZone) &&
                DateTimeOffset.TryParse(_startWithTimeZone, out var startTimeWithTimeZone))
                StartTimeWithTimeZone = startTimeWithTimeZone;
        }
    }

    [NotMapped]
    public string? EndWithTimeZone
    {
        get => _endWithTimeZone;
        set
        {
            _endWithTimeZone = value;
            if (!string.IsNullOrEmpty(_endWithTimeZone) &&
                DateTimeOffset.TryParse(_endWithTimeZone, out var endTimeWithTimeZone))
                EndTimeWithTimeZone = endTimeWithTimeZone;
        }
    }

    [NotMapped] public string? Body { get; set; }


    public string CreateHash(string input)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var hash = BitConverter.ToString(hashedBytes).Replace("-", "").ToLowerInvariant();
            return hash;
        }
    }
}