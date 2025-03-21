using DalamudControlApp.Data.Enums;
namespace DalamudControlApp.Data.Models
{
    public class CommandResponse
    {
        public CommandResponseType Type { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
}