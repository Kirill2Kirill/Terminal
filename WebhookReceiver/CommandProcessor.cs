namespace WebhookReceiver
{
    public class CommandProcessor
    {
        public async Task ProcessCommandAsync(string user, string command)
        {
            // Здесь будет логика обработки команд
            await Task.Delay(100); // Имитация обработки команды
            Console.WriteLine($"{DateTime.Now:dd-MM-yyyy HH:mm:ss} | {user}: {command}");
        }
    }
}
