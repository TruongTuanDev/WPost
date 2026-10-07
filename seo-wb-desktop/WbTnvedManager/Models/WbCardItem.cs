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

        [JsonPropertyName("documents")]
        public WbCardDocumentsContainer? Documents { get; set; }

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

    public class ProductPhotoItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _filePath = string.Empty;
        private string _url = string.Empty;
        private int _orderIndex = 1;
        private string _status = "Sẵn sàng";
        private System.Windows.Media.Imaging.BitmapImage? _thumbnailImage;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLocalFile));
                    OnPropertyChanged(nameof(DisplayName));
                    LoadThumbnail();
                }
            }
        }

        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsLocalFile));
                    OnPropertyChanged(nameof(DisplayName));
                }
            }
        }

        public bool IsLocalFile => !string.IsNullOrWhiteSpace(_filePath) && System.IO.File.Exists(_filePath);

        public int OrderIndex
        {
            get => _orderIndex;
            set
            {
                if (_orderIndex != value)
                {
                    _orderIndex = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayName
        {
            get
            {
                if (IsLocalFile)
                {
                    return System.IO.Path.GetFileName(_filePath);
                }
                if (!string.IsNullOrWhiteSpace(_url))
                {
                    return _url.Length > 35 ? _url.Substring(0, 32) + "..." : _url;
                }
                return "Ảnh";
            }
        }

        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        public System.Windows.Media.Imaging.BitmapImage? ThumbnailImage
        {
            get => _thumbnailImage;
            private set
            {
                _thumbnailImage = value;
                OnPropertyChanged();
            }
        }

        private void LoadThumbnail()
        {
            if (IsLocalFile)
            {
                try
                {
                    var bytes = System.IO.File.ReadAllBytes(_filePath);
                    using var stream = new System.IO.MemoryStream(bytes);
                    var bitmap = new System.Windows.Media.Imaging.BitmapImage();
                    bitmap.BeginInit();
                    bitmap.StreamSource = stream;
                    bitmap.DecodePixelWidth = 140;
                    bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    ThumbnailImage = bitmap;
                }
                catch
                {
                    ThumbnailImage = null;
                }
            }
            else
            {
                ThumbnailImage = null;
            }
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProductSizeItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _techSize = "S";
        private string _wbSize = "42";
        private decimal _price = 1500;
        private string _barcode = string.Empty;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public string TechSize
        {
            get => _techSize;
            set
            {
                if (_techSize != value)
                {
                    _techSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public string WbSize
        {
            get => _wbSize;
            set
            {
                if (_wbSize != value)
                {
                    _wbSize = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Price
        {
            get => _price;
            set
            {
                if (_price != value)
                {
                    _price = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Barcode
        {
            get => _barcode;
            set
            {
                if (_barcode != value)
                {
                    _barcode = value;
                    OnPropertyChanged();
                }
            }
        }

        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
    }

    public class WbCardDocumentsContainer
    {
        [JsonPropertyName("items")]
        public List<WbCardDocumentItem> Items { get; set; } = new();

        [JsonPropertyName("excludeDocuments")]
        public bool? ExcludeDocuments { get; set; }

        [JsonPropertyName("overallVerdict")]
        public string? OverallVerdict { get; set; }
    }

    public class WbCardDocumentItem
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("type")]
        public string Type { get; set; } = "Декларация соответствия";

        [JsonPropertyName("number")]
        public string Number { get; set; } = string.Empty;

        [JsonPropertyName("productNumber")]
        public string? ProductNumber { get; set; }

        [JsonPropertyName("tradeName")]
        public string? TradeName { get; set; }

        [JsonPropertyName("applicant")]
        public string? Applicant { get; set; }

        [JsonPropertyName("startDate")]
        public string? StartDate { get; set; }

        [JsonPropertyName("endDate")]
        public string? EndDate { get; set; }

        [JsonPropertyName("isEndless")]
        public bool IsEndless { get; set; }

        [JsonPropertyName("verdict")]
        public string? Verdict { get; set; }
    }
}
