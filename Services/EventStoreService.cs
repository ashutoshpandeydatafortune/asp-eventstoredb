namespace EventStoreAPI.Services
{
    using EventStore.Client;
    using EventStoreAPI.Models.Request;
    using System;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class EventStoreService
    {
        private readonly EventStoreClient _client;

        public EventStoreService(string connectionString)
        {
            var settings = EventStoreClientSettings.Create(connectionString);
            _client = new EventStoreClient(settings);
        }

        public async Task AppendEventAsync(CreateEvent createEventData)
        {
            var eventDataJson = JsonSerializer.Serialize(createEventData.EventDetail);
            var eventDataBytes = Encoding.UTF8.GetBytes(eventDataJson);

            var eventStoreData = new EventData(
                Uuid.NewUuid(),
                createEventData.EventType, 
                eventDataBytes,                
                metadata: null,
                contentType: "application/json"
            );

            await _client.AppendToStreamAsync(
                createEventData.StreamName,
                StreamState.Any,
                new[] { eventStoreData }
            );
        }

        public async Task<List<EventDetail>> ReadEventsAsync(string streamName)
        {
            var result = _client.ReadStreamAsync(Direction.Forwards, streamName, StreamPosition.Start);

            List<EventDetail> events = new List<EventDetail>();
            await foreach (var @event in result)
            {
                events.Add(DeserializeEventData(@event.Event.Data.ToArray()));
            }

            return events;
        }

        public async Task<List<EventDetail>> ReadEventsByEventTypeAsync(string streamName, string eventType)
        {
            return await GetFilteredEvents(streamName, eventType);
        }

        public async Task<List<EventDetail>> ReadEventsByDateTimeAsync(string streamName, DateTime startDate, DateTime endDate)
        {
            return await GetFilteredEvents(streamName, null, startDate, endDate);
        }

        public async Task<List<EventDetail>> GetFilteredEvents(string streamName, string? eventType, DateTime? startDate = null, DateTime? endDate = null)
        {
            var options = new StreamPosition(0);
            var events = new List<EventDetail>();

            var result = _client.ReadStreamAsync(Direction.Forwards, streamName, options, resolveLinkTos: true);

            await foreach (var @event in result)
            {
                var recordedEvent = @event.Event;

                if (eventType != null)
                {
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        if ((recordedEvent.EventType == eventType) && IsEventInDateRange(recordedEvent.Created, startDate.Value, endDate.Value))
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                    else
                    {
                        if (recordedEvent.EventType == eventType)
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                }
                else
                {
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        if (IsEventInDateRange(recordedEvent.Created, startDate.Value, endDate.Value))
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                    else
                    {
                        events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                    }
                }
            }

            return events;
        }

        private bool IsEventInDateRange(DateTime eventDate, DateTime startDate, DateTime endDate)
        {
            return eventDate >= startDate && eventDate <= endDate;
        }

        private EventDetail DeserializeEventData(byte[] data)
        {
            var json = Encoding.UTF8.GetString(data);
            var eventDetail = JsonSerializer.Deserialize<EventDetail>(json);

            if (eventDetail == null)
            {
                // Handle the case where deserialization returns null
                // For example, log an error, throw an exception, or handle it according to your business logic.
                throw new InvalidOperationException("Failed to deserialize event data.");
            }

            return eventDetail;
        }
    }
}
