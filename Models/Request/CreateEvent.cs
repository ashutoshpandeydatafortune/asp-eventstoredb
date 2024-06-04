namespace EventStoreAPI.Models.Request
{
    public class CreateEvent
    {
        public string EventType { get; set; }
        public string StreamName { get; set; }
        public EventDetail EventDetail { get; set; }
    }

    public class EventDetail
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public string Country { get; set; }
    }
}
