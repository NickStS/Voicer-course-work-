using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using AudioSwitcher.AudioApi.CoreAudio;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Voicer.Controllers;
using Voicer.Data;
using Voicer.Models;

namespace Voicer.Service
{
    public class VoiceService : IVoiceService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RemindersController _remindersController;
        private readonly ILogger<VoiceService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public VoiceService(HttpClient httpClient, string apiKey, ApplicationDbContext context, UserManager<IdentityUser> userManager, RemindersController remindersController, ILogger<VoiceService> logger , IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = apiKey ?? throw new ArgumentNullException(nameof(apiKey));
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _remindersController = remindersController ?? throw new ArgumentNullException(nameof(remindersController));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> ProcessQuery(string query)
        {
            try
            {
                _logger.LogInformation("Processing query: {query}", query);

                var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext.User);
                var username = user?.UserName ?? "гость";


                if (query.ToLower().Contains("привет"))
                {
                    return $"Привет {username}! Как я могу помочь вам сегодня?";
                }

                if (query.ToLower().StartsWith("измени громкость на"))
                {
                    if (int.TryParse(query.Substring("измени громкость на".Length).Trim(), out int volume))
                    {
                        SetVolume(volume);
                        return $"Громкость изменена на {volume}%";
                    }
                    else
                    {
                        return "Не удалось определить уровень громкости. Пожалуйста, укажите число от 0 до 100.";
                    }
                }

                if (query.ToLower().Contains("сколько время") || query.ToLower().Contains("текущее время"))
                {
                    return $"Текущее время: {DateTime.Now:HH:mm}";
                }

                if (query.ToLower().Contains("какой сегодня день") || query.ToLower().Contains("текущая дата"))
                {
                    return $"Сегодня: {DateTime.Now:dddd, d MMMM yyyy}";
                }

                if (query.ToLower().Contains("запусти калькулятор"))
                {
                    Process.Start("calc.exe");
                    return "Калькулятор запущен.";
                }

                if (query.ToLower().StartsWith("создай напоминание"))
                {
                    int timeStartIndex = query.IndexOf("на ");
                    string reminderText = timeStartIndex != -1
                        ? query.Substring("создай напоминание".Length, timeStartIndex - "создай напоминание".Length).Trim()
                        : query.Substring("создай напоминание".Length).Trim();

                    DateTime reminderTime = DateTime.Now;
                    if (timeStartIndex != -1)
                    {
                        string timeString = query.Substring(timeStartIndex + 3).Trim();

                        if (timeString.StartsWith("завтра"))
                        {
                            reminderTime = DateTime.Now.AddDays(1);
                            timeString = timeString.Substring("завтра".Length).Trim();
                        }

                        if (DateTime.TryParse(timeString, out DateTime parsedTime))
                        {
                            reminderTime = reminderTime.Date.Add(parsedTime.TimeOfDay);
                        }
                        else if (TimeSpan.TryParse(timeString, out TimeSpan parsedTimeSpan))
                        {
                            reminderTime = reminderTime.Date.Add(parsedTimeSpan);
                        }
                        else
                        {
                            return $"Некорректный формат времени: {timeString}. Пожалуйста, укажите время в правильном формате.";
                        }

                        if (reminderTime < DateTime.Now)
                        {
                            return "Время напоминания должно быть в будущем.";
                        }
                    }

                    var reminder = new Reminder
                    {
                        Text = reminderText,
                        ReminderTime = reminderTime,
                        CreatedAt = DateTime.Now
                    };

                    var result = await _remindersController.CreateReminder(reminder);
                    if (result is OkObjectResult)
                    {
                        return $"Напоминание создано: {reminder.Text}, Время: {reminder.ReminderTime:HH:mm}";
                    }
                    else
                    {
                        return $"Ошибка при создании напоминания.";
                    }
                }

                if (query.ToLower().Contains("покажи все напоминания"))
                {
                    var reminders = await _context.Reminders.ToListAsync();
                    if (reminders.Any())
                    {
                        var remindersList = string.Join("\n", reminders.Select(r => $"{r.Id}: {r.Text} (Создано: {r.CreatedAt}, Напоминание: {r.ReminderTime})"));
                        return $"Ваши напоминания:\n{remindersList}";
                    }
                    else
                    {
                        return "У вас нет напоминаний.";
                    }
                }

                _logger.LogInformation("Processing query: {query}", query);

                var requestData = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "user", content = query }
                    },
                    max_tokens = 150,
                    temperature = 0.7
                };

                var jsonRequestData = JsonSerializer.Serialize(requestData);
                var content = new StringContent(jsonRequestData, Encoding.UTF8, "application/json");

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _apiKey);

                var response = await _httpClient.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogInformation("Response Body: " + responseBody);

                    using (JsonDocument doc = JsonDocument.Parse(responseBody))
                    {
                        var root = doc.RootElement;

                        if (root.TryGetProperty("choices", out JsonElement choices) && choices.GetArrayLength() > 0)
                        {
                            var firstChoice = choices[0];

                            if (firstChoice.TryGetProperty("message", out JsonElement message) && message.TryGetProperty("content", out JsonElement contentJson))
                            {
                                string chatGptResponse = contentJson.GetString();
                                if (!string.IsNullOrEmpty(chatGptResponse))
                                {
                                    _logger.LogInformation("ChatGPT Response: " + chatGptResponse);
                                    return chatGptResponse;
                                }
                            }
                        }
                    }
                    return "Произошла ошибка при обработке ответа.";
                }
                else
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    return $"Ошибка при обработке запроса. Код состояния: {response.StatusCode}. Тело ответа: {errorBody}";
                }
            }
            catch (Exception ex)
            {
                return "Произошла ошибка: " + ex.Message;
            }
        }

        private void SetVolume(int volume)
        {
            var audioController = new CoreAudioController();
            audioController.DefaultPlaybackDevice.Volume = volume;
        }

        public async Task<bool> ChangeUsernameAsync(string userId, string newUsername)
        {
            _logger.LogInformation("Attempting to change username for user ID: {UserId} to new username: {NewUsername}", userId, newUsername);

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    user.UserName = newUsername;
                    var result = await _userManager.UpdateAsync(user);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("Username successfully changed for user ID: {UserId}", userId);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("Failed to update username for user ID: {UserId}. Errors: {Errors}", userId, string.Join(", ", result.Errors));
                    }
                }
                else
                {
                    _logger.LogError("User not found for user ID: {UserId}", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing the username for user ID: {UserId}", userId);
            }
            return false;
        }



        public Task<IdentityUser> GetUserByIdAsync(string userId)
        {
            return _userManager.FindByIdAsync(userId);
        }
    }
}
