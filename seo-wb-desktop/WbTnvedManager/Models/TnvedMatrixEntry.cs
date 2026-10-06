using System;

namespace WbTnvedManager.Models
{
    public class TnvedMatrixEntry
    {
        public int Id { get; set; }
        public int SubjectId { get; set; }
        public string SubjectName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;       // "Женский", "Мужской", "Девочки", "Мальчики", "Унисекс"
        public string Material { get; set; } = string.Empty;     // "Хлопок", "Полиэстер", "Шерсть", "Лен", "Шелк", "Вискоза", "Кожа", etc.
        public string KnitType { get; set; } = string.Empty;     // "Трикотаж", "Ткань"
        public string TnvedCode { get; set; } = string.Empty;    // 10 digits, e.g. "6204623100"
        public string Description { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public override string ToString()
        {
            return $"[{SubjectId}] {SubjectName} | {Gender} | {Material} -> {TnvedCode}";
        }
    }
}
