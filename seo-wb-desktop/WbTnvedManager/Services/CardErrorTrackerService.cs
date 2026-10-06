using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public class CardErrorTrackerService
    {
        private readonly IWbApiClient _apiClient;

        public CardErrorTrackerService(IWbApiClient apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<List<WbCardErrorItem>> FetchRecentErrorsAsync(CancellationToken cancellationToken = default)
        {
            return await _apiClient.GetCardErrorsAsync(cancellationToken);
        }
    }
}
