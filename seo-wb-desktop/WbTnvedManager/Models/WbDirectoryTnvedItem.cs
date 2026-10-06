using System.Text.Json.Serialization;

namespace WbTnvedManager.Models
{
    public class WbDirectoryTnvedItem
    {
        [JsonPropertyName("subjectID")]
        public int SubjectId { get; set; }

        [JsonPropertyName("subjectName")]
        public string? SubjectName { get; set; }

        [JsonPropertyName("tnved")]
        public string? Tnved { get; set; }

        [JsonPropertyName("code")]
        public string? Code { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        public string GetTnvedCode() => !string.IsNullOrWhiteSpace(Tnved) ? Tnved : (Code ?? string.Empty);
    }

    public class WbSubjectItem
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("parentID")]
        public int? ParentId { get; set; }

        [JsonPropertyName("parentName")]
        public string? ParentName { get; set; }

        public override string ToString() => $"[{Id}] {Name}";
    }

    public class WbCardErrorItem
    {
        [JsonPropertyName("imtID")]
        public long ImtId { get; set; }

        [JsonPropertyName("nmID")]
        public long? NmId { get; set; }

        [JsonPropertyName("vendorCode")]
        public string? VendorCode { get; set; }

        [JsonPropertyName("errors")]
        public object? Errors { get; set; }

        [JsonPropertyName("object")]
        public string? Object { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }
    }
}
