using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Voicer.Data;
using Voicer.Models;

namespace Voicer.Controllers
{
    public class RemindersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RemindersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Reminders
        public async Task<IActionResult> Index()
        {
            return View(await _context.Reminders.ToListAsync());
        }

        // GET: Reminders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Reminders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Text,ReminderTime")] Reminder reminder)
        {
            if (ModelState.IsValid)
            {
                // Валидация времени напоминания
                if (reminder.ReminderTime < DateTime.Now)
                {
                    ModelState.AddModelError("ReminderTime", "Время напоминания должно быть в будущем.");
                    return View(reminder);
                }

                _context.Add(reminder);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(reminder);
        }

        // POST: api/Reminders/CreateReminder
        [HttpPost("CreateReminder")]
        public async Task<IActionResult> CreateReminder([FromBody] Reminder reminder)
        {
            if (reminder.ReminderTime < DateTime.Now)
            {
                return BadRequest("Время напоминания должно быть в будущем.");
            }

            _context.Reminders.Add(reminder);
            await _context.SaveChangesAsync();

            return Ok(reminder);
        }

        // GET: Reminders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null)
            {
                return NotFound();
            }
            return View(reminder);
        }

        // POST: Reminders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Text,ReminderTime")] Reminder reminder)
        {
            if (id != reminder.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(reminder);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ReminderExists(reminder.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(reminder);
        }

        // GET: Reminders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var reminder = await _context.Reminders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (reminder == null)
            {
                return NotFound();
            }

            return View(reminder);
        }

        // POST: Reminders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var reminder = await _context.Reminders.FindAsync(id);
            if (reminder == null)
            {
                return NotFound();
            }

            _context.Reminders.Remove(reminder);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ReminderExists(int id)
        {
            return _context.Reminders.Any(e => e.Id == id);
        }
    }
}
