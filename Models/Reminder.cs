namespace Voicer.Models
{
    public class Reminder
    {
        public int Id { get; set; }
        public string Text { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime ReminderTime { get; set; }
    }
}
