namespace Common.Models.Secrets
{
    public class SendGridSecret
    {
        public SendGridSecret(string token)
        {
            Token = token;
        }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public SendGridSecret()
        {
        }

        public string Token { get; set; }
    }
}
