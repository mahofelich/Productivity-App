using ProductivityHub.Models;

namespace ProductivityHub.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db)
    {
        if (!db.ReflectionPrompts.Any())
        {
            db.ReflectionPrompts.AddRange(
                new ReflectionPrompt { Text = "What are your top 3 priorities today?", Time = PromptTime.Morning, SortOrder = 1 },
                new ReflectionPrompt { Text = "What are you grateful for?", Time = PromptTime.Morning, SortOrder = 2 },
                new ReflectionPrompt { Text = "What would make today successful?", Time = PromptTime.Morning, SortOrder = 3 },
                new ReflectionPrompt { Text = "What went well today?", Time = PromptTime.Evening, SortOrder = 1 },
                new ReflectionPrompt { Text = "What could have gone better?", Time = PromptTime.Evening, SortOrder = 2 },
                new ReflectionPrompt { Text = "What did you learn?", Time = PromptTime.Evening, SortOrder = 3 },
                new ReflectionPrompt { Text = "What are tomorrow's priorities?", Time = PromptTime.Evening, SortOrder = 4 });
        }

        if (!db.Achievements.Any())
        {
            db.Achievements.AddRange(
                new Achievement { Code = "STREAK_7", Name = "7 Day Streak", Description = "Stay active 7 days in a row.", Icon = "fa-fire", XpReward = 100 },
                new Achievement { Code = "HYDRATION_MASTER", Name = "Hydration Master", Description = "Hit your water goal 7 days in a row.", Icon = "fa-droplet", XpReward = 100 },
                new Achievement { Code = "TASK_CRUSHER", Name = "Task Crusher", Description = "Complete 50 tasks.", Icon = "fa-bolt", XpReward = 150 },
                new Achievement { Code = "CONSISTENCY_KING", Name = "Consistency King", Description = "Complete every habit for 30 days.", Icon = "fa-crown", XpReward = 300 },
                new Achievement { Code = "FIRST_TASK", Name = "Getting Started", Description = "Complete your first task.", Icon = "fa-seedling", XpReward = 25 },
                new Achievement { Code = "JOURNAL_10", Name = "Reflective Mind", Description = "Write 10 journal entries.", Icon = "fa-book", XpReward = 75 });
        }
        db.SaveChanges();

        if (!db.Users.Any(u => u.Id == Local.UserId))
        {
            db.Users.Add(new AppUser { Id = Local.UserId, DisplayName = "You" });

            var today = DateTime.Today;
            db.Tasks.AddRange(
                new TaskItem { UserId = Local.UserId, Title = "Plan the week", DueDate = today, Priority = Priority.High, IsImportant = true, IsUrgent = true, IsFocusTask = true, Category = "Planning", EstimatedMinutes = 30 },
                new TaskItem { UserId = Local.UserId, Title = "Ship dashboard widget", DueDate = today.AddDays(2), Priority = Priority.Urgent, IsImportant = true, Category = "Work", EstimatedMinutes = 120, State = TaskState.InProgress },
                new TaskItem { UserId = Local.UserId, Title = "Read 20 pages", DueDate = today, Priority = Priority.Low, Category = "Personal", EstimatedMinutes = 25 });

            db.Events.AddRange(
                new CalendarEvent { UserId = Local.UserId, Title = "Team standup", StartTime = today.AddHours(9), EndTime = today.AddHours(9.25), Category = "Work", Color = "#6366f1", Recurrence = RecurrenceType.Daily },
                new CalendarEvent { UserId = Local.UserId, Title = "Gym", StartTime = today.AddHours(18), EndTime = today.AddHours(19), Category = "Health", Color = "#10b981" });

            db.Habits.AddRange(
                new Habit { UserId = Local.UserId, Name = "Read", Icon = "fa-book-open", Color = "#f59e0b" },
                new Habit { UserId = Local.UserId, Name = "Gym", Icon = "fa-dumbbell", Color = "#ef4444" },
                new Habit { UserId = Local.UserId, Name = "Meditate", Icon = "fa-spa", Color = "#8b5cf6" });

            db.Notes.Add(new Note { UserId = Local.UserId, Title = "Welcome to ProductivityHub", Content = "## Tips\n- Use the **quick add** buttons on the dashboard\n- Move tasks across the Kanban board\n- Log water with one tap", Kind = NoteKind.Quick, IsPinned = true });

            db.SaveChanges();
        }
    }
}
