namespace BookNumAPI.Match.Events
{
    public class InMemoryEventPublisher : IEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;

        public InMemoryEventPublisher(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Publish<T>(T @event)
        {
            // 直接解析所有符合 T 的 handlers
            var handlers = _serviceProvider.GetServices<IEventHandler<T>>();

            foreach (var handler in handlers)
            {
                await handler.Handle(@event);
            }
        }
    }

}
