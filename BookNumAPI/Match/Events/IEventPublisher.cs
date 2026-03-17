namespace BookNumAPI.Match.Events
{
    public interface IEventPublisher
    {
        Task Publish<T>(T @event);
    }
}
