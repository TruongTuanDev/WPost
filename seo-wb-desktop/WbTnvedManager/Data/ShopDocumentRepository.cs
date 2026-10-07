using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Data.Sqlite;
using WbTnvedManager.Models;

namespace WbTnvedManager.Data
{
    public class ShopDocumentRepository
    {
        private readonly string _connectionString;

        public ShopDocumentRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? DatabaseInitializer.ConnectionString;
        }

        public List<ShopProfile> GetShops()
        {
            var result = new List<ShopProfile>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT ShopId, ShopName, Inn, ApiKey, AutoApplyOnCreate FROM ShopProfiles ORDER BY ShopName";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                result.Add(new ShopProfile
                {
                    ShopId = reader.GetString(0),
                    ShopName = reader.GetString(1),
                    Inn = reader.IsDBNull(2) ? "" : reader.GetString(2),
                    ApiKey = reader.IsDBNull(3) ? "" : reader.GetString(3),
                    AutoApplyOnCreate = !reader.IsDBNull(4) && reader.GetInt32(4) == 1
                });
            }
            return result;
        }

        public void SaveShop(ShopProfile shop)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO ShopProfiles (ShopId, ShopName, Inn, ApiKey, AutoApplyOnCreate, UpdatedAt)
                VALUES (@shopId, @shopName, @inn, @apiKey, @autoApply, CURRENT_TIMESTAMP)
            ";
            cmd.Parameters.AddWithValue("@shopId", shop.ShopId);
            cmd.Parameters.AddWithValue("@shopName", shop.ShopName);
            cmd.Parameters.AddWithValue("@inn", shop.Inn ?? "");
            cmd.Parameters.AddWithValue("@apiKey", shop.ApiKey ?? "");
            cmd.Parameters.AddWithValue("@autoApply", shop.AutoApplyOnCreate ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public List<ShopDocumentPackage> GetPackagesByShop(string shopId)
        {
            var result = new List<ShopDocumentPackage>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, ShopId, PackageName, DocType, DocNumber, StartDate, EndDate, IsEndless, TargetScope, ScopeFilterValue, AutoApplyOnCreate 
                FROM ShopDocumentPackages 
                WHERE ShopId = @shopId 
                ORDER BY PackageName
            ";
            cmd.Parameters.AddWithValue("@shopId", shopId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var pkg = new ShopDocumentPackage
                {
                    Id = reader.GetString(0),
                    ShopId = reader.GetString(1),
                    PackageName = reader.GetString(2),
                    DocType = reader.GetString(3),
                    DocNumber = reader.GetString(4),
                    IsEndless = reader.GetInt32(7) == 1,
                    TargetScope = Enum.TryParse<DocumentApplyScope>(reader.GetString(8), out var scope) ? scope : DocumentApplyScope.AllShop,
                    ScopeFilterValue = reader.IsDBNull(9) ? "" : reader.GetString(9),
                    AutoApplyOnCreate = !reader.IsDBNull(10) && reader.GetInt32(10) == 1
                };

                var startStr = reader.GetString(5);
                if (DateTime.TryParseExact(startStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var sDate))
                {
                    pkg.StartDate = sDate;
                }

                var endStr = reader.IsDBNull(6) ? "" : reader.GetString(6);
                if (!string.IsNullOrEmpty(endStr) && DateTime.TryParseExact(endStr, "dd.MM.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var eDate))
                {
                    pkg.EndDate = eDate;
                }

                result.Add(pkg);
            }
            return result;
        }

        public void SavePackage(ShopDocumentPackage pkg)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT OR REPLACE INTO ShopDocumentPackages (Id, ShopId, PackageName, DocType, DocNumber, StartDate, EndDate, IsEndless, TargetScope, ScopeFilterValue, AutoApplyOnCreate, UpdatedAt)
                VALUES (@id, @shopId, @packageName, @docType, @docNumber, @startDate, @endDate, @isEndless, @targetScope, @scopeFilter, @autoApply, CURRENT_TIMESTAMP)
            ";
            cmd.Parameters.AddWithValue("@id", pkg.Id);
            cmd.Parameters.AddWithValue("@shopId", pkg.ShopId);
            cmd.Parameters.AddWithValue("@packageName", pkg.PackageName);
            cmd.Parameters.AddWithValue("@docType", pkg.DocType);
            cmd.Parameters.AddWithValue("@docNumber", pkg.DocNumber);
            cmd.Parameters.AddWithValue("@startDate", pkg.StartDate.ToString("dd.MM.yyyy"));
            cmd.Parameters.AddWithValue("@endDate", pkg.IsEndless ? "" : pkg.EndDate.ToString("dd.MM.yyyy"));
            cmd.Parameters.AddWithValue("@isEndless", pkg.IsEndless ? 1 : 0);
            cmd.Parameters.AddWithValue("@targetScope", pkg.TargetScope.ToString());
            cmd.Parameters.AddWithValue("@scopeFilter", pkg.ScopeFilterValue ?? "");
            cmd.Parameters.AddWithValue("@autoApply", pkg.AutoApplyOnCreate ? 1 : 0);

            cmd.ExecuteNonQuery();
        }

        public void DeletePackage(string packageId)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM ShopDocumentPackages WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", packageId);
            cmd.ExecuteNonQuery();
        }

        public void SaveLog(string shopId, long nmId, string vendorCode, string docNumber, string writeStatus, string wbCheckStatus, string errorMessage)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO ShopDocumentCardLogs (ShopId, NmId, VendorCode, DocNumber, WriteStatus, WbCheckStatus, ErrorMessage, UpdatedAt)
                VALUES (@shopId, @nmId, @vendorCode, @docNumber, @writeStatus, @wbCheckStatus, @errorMessage, CURRENT_TIMESTAMP)
            ";
            cmd.Parameters.AddWithValue("@shopId", shopId);
            cmd.Parameters.AddWithValue("@nmId", nmId);
            cmd.Parameters.AddWithValue("@vendorCode", vendorCode ?? "");
            cmd.Parameters.AddWithValue("@docNumber", docNumber ?? "");
            cmd.Parameters.AddWithValue("@writeStatus", writeStatus ?? "");
            cmd.Parameters.AddWithValue("@wbCheckStatus", wbCheckStatus ?? "");
            cmd.Parameters.AddWithValue("@errorMessage", errorMessage ?? "");

            cmd.ExecuteNonQuery();
        }
    }
}
