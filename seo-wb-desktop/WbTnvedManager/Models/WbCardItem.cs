using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WbTnvedManager.Models
{
    public class WbCardsListResponse
    {
        [JsonPropertyName("cards")]
        public List<WbCardItem> Cards { get; set; } = new();

        [JsonPropertyName("cursor")]
        public WbCursor? Cursor { get; set; }
    }

    public class WbCursor
    {
        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }

        [JsonPropertyName("nmID")]
        public long? NmId { get; set; }

        [JsonPropertyName("total")]
        public int? Total { get; set; }
    }

    public class WbCardItem
    {
        [JsonPropertyName("nmID")]
        public long NmId { get; set; }

        [JsonPropertyName("imtID")]
        public long ImtId { get; set; }

        [JsonPropertyName("subjectID")]
        public int SubjectId { get; set; }

        [JsonPropertyName("subjectName")]
        public string SubjectName { get; set; } = string.Empty;

        [JsonPropertyName("vendorCode")]
        public string VendorCode { get; set; } = string.Empty;

        [JsonPropertyName("brand")]
        public string Brand { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("photos")]
        public List<JsonElement>? Photos { get; set; }

        [JsonPropertyName("dimensions")]
        public JsonElement? Dimensions { get; set; }

        [JsonPropertyName("characteristics")]
        public List<WbCharacteristic> Characteristics { get; set; } = new();

        [JsonPropertyName("sizes")]
        public List<JsonElement>? Sizes { get; set; }

        [JsonPropertyName("tags")]
        public List<JsonElement>? Tags { get; set; }

        [JsonPropertyName("createdAt")]
        public string? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public string? UpdatedAt { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JsonElement>? AdditionalProperties { get; set; }

        // Helper getters
        [JsonIgnore]
        public string? CurrentTnved => GetCharacteristicValue("ТНВЭД", 5);

        [JsonIgnore]
        public string? CurrentGender => GetCharacteristicValue("Пол", 8);

        [JsonIgnore]
        public string? CurrentMaterial => GetCharacteristicValue("Состав") ?? GetCharacteristicValue("Материал");

        public string? GetCharacteristicValue(string name, int? id = null)
        {
            if (Characteristics == null) return null;
            foreach (var ch in Characteristics)
            {
                if ((id.HasValue && ch.Id == id.Value) || 
                    (!string.IsNullOrEmpty(ch.Name) && ch.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    return ch.GetFormattedValue();
                }
            }
            return null;
        }
    }

    public class WbCharacteristic
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        public string GetFormattedValue()
        {
            if (Value == null) return string.Empty;
            if (Value is JsonElement elem)
            {
                if (elem.ValueKind == JsonValueKind.Array)
                {
                    var items = new List<string>();
                    foreach (var item in elem.EnumerateArray())
                    {
                        var str = item.ToString();
                        if (!string.IsNullOrWhiteSpace(str)) items.Add(str);
                    }
                    return string.Join(", ", items);
                }
                return elem.ToString();
            }
            return Value.ToString() ?? string.Empty;
        }
    }
}
