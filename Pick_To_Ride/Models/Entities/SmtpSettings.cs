namespace Pick_To_Ride.Models.Entities
{
    public class SmtpSettings
    {
        public string Host { get; set; }
        public int Port { get; set; } = 587;
        public string Username { get; set; }
        public string Password { get; set; }
        public bool UseSSL { get; set; } = true;
    }
}
