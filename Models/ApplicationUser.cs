using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;

namespace Voicer.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Address { get; set; }

        public virtual ICollection<Reminder> Reminders { get; set; }
        public virtual ICollection<VoiceQuery> VoiceQueries { get; set; }
    }
}
