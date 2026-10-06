using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public interface IWbApiClient
    {
        void UpdateConfiguration(string apiKey, string baseUrl, int rateLimitDelayMs);
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
        Task<List<WbCardItem>> GetAllCardsAsync(IProgress<string>? progress = null, CancellationToken cancellationToken = default);
        Task<(bool Success, string Message)> UpdateCardsBatchAsync(List<WbCardItem> cardsToUpdate, CancellationToken cancellationToken = default);
        Task<List<WbCardErrorItem>> GetCardErrorsAsync(CancellationToken cancellationToken = default);
        Task<List<WbSubjectItem>> GetSubjectsAsync(CancellationToken cancellationToken = default);
        Task<List<WbDirectoryTnvedItem>> GetTnvedDirectoryAsync(int? subjectId = null, string? search = null, CancellationToken cancellationToken = default);
        Task<List<WbDirectoryTnvedItem>> GetAllTnvedDirectoryAsync(CancellationToken cancellationToken = default);
    }
}
