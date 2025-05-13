namespace WebhookReceiver
{

    public class User
    {
        public string Name { get; set; }
        public string ApiKey { get; set; } = "не задано";
        public string SecretKey { get; set; } = "не задано";
        public string Passphrase { get; set; } = "не задано";
    }

}
