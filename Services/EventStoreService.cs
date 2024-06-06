namespace EventStoreAPI.Services
{
    using EventStore.Client;
    using EventStoreAPI.Models.Request;
    using Microsoft.Extensions.Options;
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

        public async Task<List<EventDetail>> ReadEventsAsync(string streamName, int? pageNumber, int? pageSize)
        {
            var start = pageSize ?? 10 * pageNumber ?? 1;

            var result = _client.ReadStreamAsync(
                        Direction.Forwards,
                        streamName,
                        StreamPosition.FromInt64((long)start),
                        maxCount: pageSize ?? 10,
                        resolveLinkTos: true
                    );

            List<EventDetail> events = new List<EventDetail>();
            await foreach (var @event in result)
            {
                events.Add(DeserializeEventData(@event.Event.Data.ToArray()));
            }

            return events;
        }

        public async Task<List<EventDetail>> ReadEventsByEventTypeAsync(string streamName, string eventType, int? pageNumber, int? pageSize)
        {
            return await GetFilteredEvents(
                streamName: streamName,
                eventType: eventType);
        }

        public async Task<List<EventDetail>> ReadEventsByDateTimeAsync(string streamName, DateTime startDate, DateTime endDate, int? pageNumber, int? pageSize)
        {
            return await GetFilteredEvents(
                streamName: streamName, 
                startDate: startDate, 
                endDate:endDate);
        }

        public async Task<List<EventDetail>> GetFilteredEvents(string streamName, string? eventType = null, DateTime? startDate = null, DateTime? endDate = null, int? pageNumber = null, int? pageSize = null)
        {
            var options = new StreamPosition(0);
            var events = new List<EventDetail>();

            var start = pageSize ?? 10 * pageNumber ?? 1;

            var result = _client.ReadStreamAsync(
                        Direction.Forwards,
                        streamName,
                        StreamPosition.FromInt64((long)start),
                        maxCount: pageSize ?? 10,
                        resolveLinkTos: true
                    );

            await foreach (var @event in result)
            {
                var recordedEvent = @event.Event;

                // If event type is passed
                if (eventType != null)
                {
                    // If dates are passed
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        if ((recordedEvent.EventType == eventType) && IsEventInDateRange(recordedEvent.Created, startDate.Value, endDate.Value))
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                    else
                    {
                        // If no dates, check event type
                        if (recordedEvent.EventType == eventType)
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                }
                else
                {
                    // If no event type, check dates
                    if (startDate.HasValue && endDate.HasValue)
                    {
                        if (IsEventInDateRange(recordedEvent.Created, startDate.Value, endDate.Value))
                        {
                            events.Add(DeserializeEventData(recordedEvent.Data.ToArray()));
                        }
                    }
                    else
                    {
                        // If no filter, then simply add the event to the list
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
