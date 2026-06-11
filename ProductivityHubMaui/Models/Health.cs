using System.ComponentModel.DataAnnotations;

namespace ProductivityHub.Models;

public class WaterEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public int AmountMl { get; set; }
    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

public class Meal
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public AppUser? User { get; set; }
    public DateOnly Date { get; set; }
    public MealType Type { get; set; }
    public List<FoodEntry> Items { get; set; } = [];
}

public class FoodEntry
{
    public int Id { get; set; }
    public int MealId { get; set; }
    public Meal? Meal { get; set; }
    [Required, MaxLength(150)] public string Name { get; set; } = "";
    public int Calories { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }
    public bool IsFavorite { get; set; }
}
