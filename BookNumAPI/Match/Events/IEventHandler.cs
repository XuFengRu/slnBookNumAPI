namespace BookNumAPI.Match.Events
{
    public interface IEventHandler<T>
    {
        Task Handle(T @event);
    }
}
