using EventStoreAPI.Models.Request;
using EventStoreAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

[ApiController]
[Route("[controller]")]
public class EventsController : ControllerBase
{
    private readonly EventStoreService _eventStoreService;

    public EventsController(EventStoreService eventStoreService)
    {
        _eventStoreService = eventStoreService;
    }

    [HttpPost]
    public async Task<IActionResult> PostEvent([FromBody] CreateEvent createEventData)
    {
        await _eventStoreService.AppendEventAsync(createEventData);
        return Ok();
    }

    [HttpGet("{streamName}/{pageNumber?}/{pageSize?}")]
    public async Task<IActionResult> ReadEvents(string streamName, int? pageNumber, int? pageSize)
    {
        List<EventDetail> result = await _eventStoreService.ReadEventsAsync(streamName, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{streamName}/{eventType}/{pageNumber?}/{pageSize?}")]
    public async Task<IActionResult> ReadEventsByEventType(string streamName, string eventType, int? pageNumber, int? pageSize)
    {
        List<EventDetail> result = await _eventStoreService.ReadEventsByEventTypeAsync(streamName, eventType, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{streamName}/{startDate}/{endDate}/{pageNumber?}/{pageSize?}")]
    public async Task<IActionResult> ReadEventsByDateTimeAsync(string streamName, string startDate, string endDate, int? pageNumber, int? pageSize)
    {
        List<EventDetail> result = await _eventStoreService.ReadEventsByDateTimeAsync(streamName, DateTime.Parse(startDate), DateTime.Parse(endDate), pageNumber, pageSize);
        return Ok(result);
    }
}
