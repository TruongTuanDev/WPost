using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class WbApiClient : IWbApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly RateLimiter _rateLimiter;
        private string _apiKey = string.Empty;
        private string _baseUrl = "https://content-api.wildberries.ru";

        public bool HasApiKey => !string.IsNullOrWhiteSpace(_apiKey);

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public WbApiClient(string apiKey = "", string baseUrl = "https://content-api.wildberries.ru", int rateLimitDelayMs = 350)
        {
            _apiKey = apiKey;
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://content-api.wildberries.ru" : baseUrl.TrimEnd('/');
            _rateLimiter = new RateLimiter(rateLimitDelayMs);

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(45)
            };
        }

        public void UpdateConfiguration(string apiKey, string baseUrl, int rateLimitDelayMs)
        {
            _apiKey = apiKey;
            _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? "https://content-api.wildberries.ru" : baseUrl.TrimEnd('/');
            _rateLimiter.DelayMs = rateLimitDelayMs;
        }

        public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_apiKey)) return false;

                var request = CreateRequest(HttpMethod.Get, $"{_baseUrl}/content/v2/cards/limits");
                var response = await SendWithRetryAsync(request, cancellationToken);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<WbCardItem>> GetAllCardsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default)
        {
            var allCards = new List<WbCardItem>();
            string? updatedAt = null;
            long? nmId = null;
            int page = 1;

            while (!cancellationToken.IsCancellationRequested)
            {
                progress?.Report($"Đang tải trang {page} (đã lấy {allCards.Count} sản phẩm)...");

                var payload = new Dictionary<string, object>
                {
                    ["settings"] = new Dictionary<string, object>
                    {
                        ["sort"] = new Dictionary<string, object>
                        {
                            ["ascending"] = true
                        },
                        ["filter"] = new Dictionary<string, object>
                        {
                            ["withPhoto"] = -1
                        },
                        ["cursor"] = new Dictionary<string, object>
                        {
                            ["limit"] = 100
                        }
                    }
                };

                var cursorDict = (Dictionary<string, object>)((Dictionary<string, object>)payload["settings"])["cursor"];
                if (!string.IsNullOrEmpty(updatedAt))
                {
                    cursorDict["updatedAt"] = updatedAt;
                }
                if (nmId.HasValue && nmId.Value > 0)
                {
                    cursorDict["nmID"] = nmId.Value;
                }

                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v2/get/cards/list", payload);
                var response = await SendWithRetryAsync(request, cancellationToken);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                    throw new HttpRequestException($"Lỗi API WB ({response.StatusCode}): {errorContent}");
                }

                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                var result = JsonSerializer.Deserialize<WbCardsListResponse>(jsonString, JsonOptions);

                if (result?.Cards == null || result.Cards.Count == 0)
                {
                    break;
                }

                allCards.AddRange(result.Cards);
                page++;

                if (result.Cards.Count < 100 || result.Cursor == null || 
                    (result.Cursor.NmId == nmId && result.Cursor.UpdatedAt == updatedAt))
                {
                    break;
                }

                updatedAt = result.Cursor.UpdatedAt;
                nmId = result.Cursor.NmId;
            }

            progress?.Report($"Hoàn tất tải dữ liệu. Tổng cộng: {allCards.Count} sản phẩm.");
            return allCards;
        }

        public async Task<(bool Success, string Message)> UpdateCardsBatchAsync(List<WbCardItem> cardsToUpdate, CancellationToken cancellationToken = default)
        {
            if (cardsToUpdate == null || cardsToUpdate.Count == 0)
                return (true, "Không có sản phẩm nào cần cập nhật.");

            try
            {
                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v2/cards/update", cardsToUpdate);
                var response = await SendWithRetryAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"Gửi yêu cầu cập nhật {cardsToUpdate.Count} sản phẩm thành công.");
                }
                else
                {
                    return (false, $"WB từ chối ({response.StatusCode}): {content}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task<List<WbCardErrorItem>> GetCardErrorsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v2/cards/error/list", new { });
                var response = await SendWithRetryAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode) return new List<WbCardErrorItem>();

                var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(jsonString);

                if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                {
                    var list = new List<WbCardErrorItem>();
                    foreach (var elem in dataElem.EnumerateArray())
                    {
                        var item = JsonSerializer.Deserialize<WbCardErrorItem>(elem.GetRawText(), JsonOptions);
                        if (item != null) list.Add(item);
                    }
                    return list;
                }
            }
            catch
            {
                // Ignore error parsing
            }
            return new List<WbCardErrorItem>();
        }

        public async Task<List<WbSubjectItem>> GetSubjectsAsync(CancellationToken cancellationToken = default)
        {
            var subjects = new List<WbSubjectItem>();
            try
            {
                int limit = 1000;
                int offset = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var url = $"{_baseUrl}/content/v2/object/all?locale=ru&limit={limit}&offset={offset}";
                    var request = CreateRequest(HttpMethod.Get, url);
                    var response = await SendWithRetryAsync(request, cancellationToken);
                    if (!response.IsSuccessStatusCode) break;

                    var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                    {
                        int batchCount = 0;
                        foreach (var item in dataElem.EnumerateArray())
                        {
                            var s = JsonSerializer.Deserialize<WbSubjectItem>(item.GetRawText(), JsonOptions);
                            if (s != null)
                            {
                                subjects.Add(s);
                                batchCount++;
                            }
                        }
                        if (batchCount < limit) break;
                        offset += limit;
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch
            {
                // Fallback
            }
            return subjects;
        }

        public async Task<List<WbDirectoryTnvedItem>> GetTnvedDirectoryAsync(int? subjectId = null, string? search = null, CancellationToken cancellationToken = default)
        {
            var items = new List<WbDirectoryTnvedItem>();
            try
            {
                var queryParams = new List<string> { "locale=ru" };
                if (subjectId.HasValue) queryParams.Add($"subjectID={subjectId.Value}");
                if (!string.IsNullOrWhiteSpace(search)) queryParams.Add($"search={Uri.EscapeDataString(search)}");

                var url = $"{_baseUrl}/content/v2/directory/tnved?{string.Join("&", queryParams)}";
                var request = CreateRequest(HttpMethod.Get, url);
                var response = await SendWithRetryAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in dataElem.EnumerateArray())
                        {
                            var item = JsonSerializer.Deserialize<WbDirectoryTnvedItem>(elem.GetRawText(), JsonOptions);
                            if (item != null) items.Add(item);
                        }
                    }
                }
            }
            catch
            {
                // Return empty list on failure
            }
            return items;
        }

        public async Task<List<WbDirectoryTnvedItem>> GetAllTnvedDirectoryAsync(CancellationToken cancellationToken = default)
        {
            var items = new List<WbDirectoryTnvedItem>();
            try
            {
                // Try /content/v2/directory/tnved/all or /content/v2/directory/tnved
                var url = $"{_baseUrl}/content/v2/directory/tnved/all?locale=ru";
                var request = CreateRequest(HttpMethod.Get, url);
                var response = await SendWithRetryAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in dataElem.EnumerateArray())
                        {
                            var item = JsonSerializer.Deserialize<WbDirectoryTnvedItem>(elem.GetRawText(), JsonOptions);
                            if (item != null) items.Add(item);
                        }
                    }
                }
            }
            catch
            {
                // Return collected items
            }
            return items;
        }

        public async Task<List<string>> GenerateBarcodesAsync(int count = 1, CancellationToken cancellationToken = default)
        {
            var barcodes = new List<string>();
            try
            {
                var payload = new { count = Math.Max(1, count) };
                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v2/barcodes", payload);
                var response = await SendWithRetryAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync(cancellationToken);
                    using var doc = JsonDocument.Parse(jsonString);
                    if (doc.RootElement.TryGetProperty("data", out var dataElem) && dataElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var elem in dataElem.EnumerateArray())
                        {
                            var code = elem.GetString();
                            if (!string.IsNullOrWhiteSpace(code))
                            {
                                barcodes.Add(code);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Return whatever collected or empty
            }
            return barcodes;
        }

        public async Task<(bool Success, string Message, string RawResponse)> UploadCardsAsync(object cardUploadPayload, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v2/cards/upload", cardUploadPayload);
                var response = await SendWithRetryAsync(request, cancellationToken);
                var content = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, "Tạo thẻ sản phẩm lên Wildberries thành công!", content);
                }
                else
                {
                    return (false, $"WB từ chối (HTTP {(int)response.StatusCode}): {content}", content);
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối khi đăng bài: {ex.Message}", string.Empty);
            }
        }

        public async Task<(bool Success, string Message)> UploadMediaFileAsync(long nmId, int photoNumber, string fileName, byte[] content, CancellationToken cancellationToken = default)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/content/v3/media/file");
                if (!string.IsNullOrWhiteSpace(_apiKey))
                {
                    request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
                }
                request.Headers.TryAddWithoutValidation("X-Nm-Id", nmId.ToString());
                request.Headers.TryAddWithoutValidation("X-Photo-Number", photoNumber.ToString());

                var multipartContent = new MultipartFormDataContent();
                var fileContent = new ByteArrayContent(content);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                multipartContent.Add(fileContent, "uploadfile", fileName);
                request.Content = multipartContent;

                var response = await SendWithRetryAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"Upload ảnh #{photoNumber} thành công.");
                }
                else
                {
                    return (false, $"Lỗi upload ảnh #{photoNumber} (HTTP {(int)response.StatusCode}): {responseText}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối khi upload ảnh: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> UploadMediaLinksAsync(long nmId, List<string> links, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = new { nmId = nmId, data = links };
                var request = CreateRequest(HttpMethod.Post, $"{_baseUrl}/content/v3/media/save", payload);
                var response = await SendWithRetryAsync(request, cancellationToken);
                var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    return (true, $"Cập nhật {links.Count} link ảnh cho NM ID {nmId} thành công.");
                }
                else
                {
                    return (false, $"Lỗi lưu link ảnh (HTTP {(int)response.StatusCode}): {responseText}");
                }
            }
            catch (Exception ex)
            {
                return (false, $"Lỗi kết nối lưu link ảnh: {ex.Message}");
            }
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url, object? body = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (!string.IsNullOrWhiteSpace(_apiKey))
            {
                request.Headers.TryAddWithoutValidation("Authorization", _apiKey);
            }
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            return request;
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            int maxRetries = 3;
            int backoffDelayMs = 1000;

            for (int i = 0; i < maxRetries; i++)
            {
                var response = await _rateLimiter.ExecuteAsync(async () =>
                {
                    // Clone request for retry if necessary
                    return await _httpClient.SendAsync(request, cancellationToken);
                }, cancellationToken);

                if ((int)response.StatusCode == 429 || (int)response.StatusCode >= 500)
                {
                    if (i == maxRetries - 1) return response;
                    await Task.Delay(backoffDelayMs, cancellationToken);
                    backoffDelayMs *= 2;
                    
                    // Re-create request for retry
                    request = CloneRequest(request);
                    continue;
                }

                return response;
            }

            throw new InvalidOperationException("Failed after max retries");
        }

        private static HttpRequestMessage CloneRequest(HttpRequestMessage req)
        {
            var clone = new HttpRequestMessage(req.Method, req.RequestUri);
            foreach (var header in req.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
            if (req.Content != null)
            {
                var contentBytes = req.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                var clonedContent = new ByteArrayContent(contentBytes);
                foreach (var header in req.Content.Headers)
                {
                    clonedContent.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                clone.Content = clonedContent;
            }
            return clone;
        }
    }
}
