using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using WbTnvedManager.Models;

namespace WbTnvedManager.Services
{
    public interface INationalCatalogConnector
    {
        void Configure(string apiKey, string baseUrl);
        Task<bool> TestConnectionAsync(string apiKeyOrToken, string legalEntityInn, CancellationToken cancellationToken = default);
        Task<List<NationalCatalogProduct>> GetProductsAsync(string legalEntityInn, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
        Task<NationalCatalogProduct?> GetProductByGtinAsync(string gtin14, CancellationToken cancellationToken = default);
        Task<List<MarkirovkaFinding>> GetFeedStatusErrorsAsync(string feedId, CancellationToken cancellationToken = default);
    }
}