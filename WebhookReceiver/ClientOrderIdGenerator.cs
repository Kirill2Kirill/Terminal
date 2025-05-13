namespace WebhookReceiver
{
    public static class ClientOrderIdGenerator
    {
        private static long _counter = 0;
        private static readonly object _lock = new object();

        public static string Generate()
        {
            lock (_lock)
            {
                _counter++;
                return $"BRIDGE:{_counter}";
            }
        }
    }
}
