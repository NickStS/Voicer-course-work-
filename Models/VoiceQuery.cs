namespace Voicer.Models
{
    public class VoiceQuery
    {
        public int Id { get; set; }
        public string Query { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
    }
}
