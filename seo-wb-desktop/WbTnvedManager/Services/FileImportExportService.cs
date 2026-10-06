using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class FileImportResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<WbCardItem> ImportedCards { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int TotalRows { get; set; }
    }

    public static class FileImportExportService
    {
        /// <summary>
        /// Imports products from CSV / TSV with zero-loss text preservation (preserves leading zeros, prevents formula execution).
        /// </summary>
        public static FileImportResult ImportFromDelimitedText(string filePath, char delimiter = ',')
        {
            var result = new FileImportResult();

            if (!File.Exists(filePath))
            {
                result.Success = false;
                result.Message = $"File không tồn tại: {filePath}";
                return result;
            }

            try
            {
                var lines = File.ReadAllLines(filePath, Encoding.UTF8);
                if (lines.Length <= 1)
                {
                    result.Success = false;
                    result.Message = "File không có dữ liệu (chỉ có tiêu đề hoặc rỗng).";
                    return result;
                }

                var header = lines[0].Split(delimiter).Select(h => h.Trim().Trim('"')).ToArray();
                int vendorCodeIdx = FindColumnIndex(header, "vendorCode", "Артикул", "Mã SKU", "SKU", "Артикул продавца");
                int titleIdx = FindColumnIndex(header, "title", "Наименование", "Tiêu đề", "Tên sản phẩm");
                int subjectIdx = FindColumnIndex(header, "subjectName", "Категория", "Danh mục", "Предмет");
                int tnvedIdx = FindColumnIndex(header, "tnved", "ТНВЭД", "ТН ВЭД", "TNVED");
                int genderIdx = FindColumnIndex(header, "gender", "Пол", "Giới tính");
                int materialIdx = FindColumnIndex(header, "material", "Состав", "Chất liệu", "Материал");
                int barcodeIdx = FindColumnIndex(header, "barcode", "Баркод", "Barcode", "GTIN", "skus");
                int priceIdx = FindColumnIndex(header, "price", "Цена", "Giá");

                for (int i = 1; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    var cols = SplitCsvLine(line, delimiter);
                    result.TotalRows++;

                    string vendorCode = CleanFormulaString(GetColValue(cols, vendorCodeIdx, $"ITEM-{i:D4}"));
                    string title = GetColValue(cols, titleIdx, "Sản phẩm mới");
                    string subjectName = GetColValue(cols, subjectIdx, "Одежда");
                    string tnved = CleanCodeString(GetColValue(cols, tnvedIdx, ""));
                    string gender = GetColValue(cols, genderIdx, "");
                    string material = GetColValue(cols, materialIdx, "");
                    string barcode = CleanCodeString(GetColValue(cols, barcodeIdx, ""));

                    // Check for scientific notation (e.g. 4.607E+12)
                    if (barcode.Contains("E+") || barcode.Contains("e+"))
                    {
                        result.Warnings.Add($"Dòng {i}: Mã Barcode '{barcode}' bị lỗi dạng số lũy thừa (Scientific notation). Cần xuất định dạng Text từ Excel.");
                    }

                    var card = new WbCardItem
                    {
                        NmId = 10000000 + i,
                        VendorCode = vendorCode,
                        Title = title,
                        SubjectName = subjectName,
                        SubjectId = 105, // Default apparel subject
                        Characteristics = new List<WbCharacteristic>
                        {
                            new() { Id = 5, Name = "ТНВЭД", Value = tnved },
                            new() { Id = 8, Name = "Пол", Value = gender },
                            new() { Id = 10, Name = "Состав", Value = material }
                        }
                    };

                    if (!string.IsNullOrEmpty(barcode))
                    {
                        card.Sizes = new List<JsonElement>
                        {
                            JsonSerializer.SerializeToElement(new
                            {
                                techSize = "OneSize",
                                wbSize = "",
                                skus = new[] { barcode },
                                price = 1000
                            })
                        };
                    }

                    result.ImportedCards.Add(card);
                }

                result.Success = true;
                result.Message = $"Nhập thành công {result.ImportedCards.Count} sản phẩm từ file.";
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = $"Lỗi xử lý file: {ex.Message}";
            }

            return result;
        }

        /// <summary>
        /// Exports audit results to CSV with UTF-8 BOM and formula injection protection.
        /// </summary>
        public static string ExportAuditResultsToCsv(IEnumerable<AuditResultItem> items, string destinationPath)
        {
            var sb = new StringBuilder();
            // Header
            sb.AppendLine("NmID,VendorCode,Danh mục (Категория),Tiêu đề (Наименование),ТНВЭД Hiện tại,ТНВЭД Chuẩn,Пол Hiện tại,Пол Chuẩn,Chất liệu (Состав),Trạng thái,Lý do chi tiết");

            foreach (var item in items)
            {
                sb.AppendLine(string.Join(",",
                    EscapeCsv(item.NmId.ToString()),
                    EscapeCsv(item.VendorCode),
                    EscapeCsv(item.SubjectName),
                    EscapeCsv(item.Title),
                    EscapeCsv(item.CurrentTnved),
                    EscapeCsv(item.SuggestedTnved),
                    EscapeCsv(item.CurrentGender),
                    EscapeCsv(item.SuggestedGender),
                    EscapeCsv(item.DetectedMaterial),
                    EscapeCsv(item.StatusBadge),
                    EscapeCsv(item.StatusMessage)
                ));
            }

            // Write with UTF-8 BOM for Excel compatibility
            File.WriteAllText(destinationPath, sb.ToString(), new UTF8Encoding(true));
            return destinationPath;
        }

        private static int FindColumnIndex(string[] header, params string[] candidates)
        {
            for (int i = 0; i < header.Length; i++)
            {
                foreach (var candidate in candidates)
                {
                    if (header[i].Equals(candidate, StringComparison.OrdinalIgnoreCase) ||
                        header[i].Contains(candidate, StringComparison.OrdinalIgnoreCase))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }

        private static string GetColValue(string[] cols, int idx, string defaultValue)
        {
            if (idx >= 0 && idx < cols.Length)
            {
                var val = cols[idx].Trim().Trim('"');
                return string.IsNullOrEmpty(val) ? defaultValue : val;
            }
            return defaultValue;
        }

        private static string CleanFormulaString(string val) { if (string.IsNullOrEmpty(val)) return string.Empty; if (val.StartsWith("=") || val.StartsWith("+") || val.StartsWith("-") || val.StartsWith("@")) return "'" + val; return val; }

        private static string CleanCodeString(string val)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            // Prevent formula injection (symbols =, +, -, @ at beginning)
            if (val.StartsWith("=") || val.StartsWith("+") || val.StartsWith("-") || val.StartsWith("@"))
            {
                val = val.Substring(1);
            }
            return val.Trim();
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            // Protect against Excel formula injection
            if (value.StartsWith("=") || value.StartsWith("+") || value.StartsWith("-") || value.StartsWith("@"))
            {
                value = "'" + value;
            }
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        private static string[] SplitCsvLine(string line, char delimiter)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var current = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == delimiter && !inQuotes)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }
            result.Add(current.ToString());
            return result.ToArray();
        }
    }
}
