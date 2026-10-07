using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using WbTnvedManager.Models;

namespace WbTnvedManager.Data
{
    public class WbCardCacheRepository
    {
        private readonly string _connectionString;

        public WbCardCacheRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? DatabaseInitializer.ConnectionString;
        }

        public int GetCount()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM WbCardCache";
            var result = cmd.ExecuteScalar();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        public List<AuditResultItem> LoadCachedAuditResults()
        {
            var results = new List<AuditResultItem>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT NmId, VendorCode, Title, SubjectId, SubjectName,
                       CurrentTnved, CurrentGender, DetectedMaterial,
                       SuggestedTnved, SuggestedGender, MatchReason,
                       Status, StatusMessage, CardJson
                FROM WbCardCache
                ORDER BY UpdatedAt DESC";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var nmId = reader.GetInt64(0);
                var vendorCode = reader.GetString(1);
                var title = reader.GetString(2);
                var subjectId = reader.GetInt32(3);
                var subjectName = reader.GetString(4);
                var currentTnved = reader.GetString(5);
                var currentGender = reader.GetString(6);
                var detectedMaterial = reader.GetString(7);
                var suggestedTnved = reader.GetString(8);
                var suggestedGender = reader.GetString(9);
                var matchReason = reader.GetString(10);
                var status = (AuditStatus)reader.GetInt32(11);
                var statusMessage = reader.GetString(12);
                var cardJson = reader.GetString(13);

                WbCardItem? card = null;
                try
                {
                    card = JsonSerializer.Deserialize<WbCardItem>(cardJson);
                }
                catch { }

                card ??= new WbCardItem
                {
                    NmId = nmId,
                    VendorCode = vendorCode,
                    Title = title,
                    SubjectId = subjectId,
                    SubjectName = subjectName
                };

                var auditItem = new AuditResultItem
                {
                    Card = card,
                    CurrentTnved = currentTnved,
                    CurrentGender = currentGender,
                    DetectedMaterial = detectedMaterial,
                    SuggestedTnved = suggestedTnved,
                    SuggestedGender = suggestedGender,
                    MatchReason = matchReason,
                    Status = status,
                    StatusMessage = statusMessage,
                    IsSelected = (status == AuditStatus.TnvedMismatch ||
                                  status == AuditStatus.GenderMismatch ||
                                  status == AuditStatus.BothMismatch)
                };

                results.Add(auditItem);
            }

            return results;
        }

        public void SaveAuditResults(IEnumerable<AuditResultItem> items)
        {
            if (items == null) return;

            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                INSERT INTO WbCardCache (
                    NmId, VendorCode, Title, SubjectId, SubjectName,
                    CurrentTnved, CurrentGender, DetectedMaterial,
                    SuggestedTnved, SuggestedGender, MatchReason,
                    Status, StatusMessage, CardJson, UpdatedAt
                ) VALUES (
                    @nmId, @vendorCode, @title, @subjectId, @subjectName,
                    @currentTnved, @currentGender, @detectedMaterial,
                    @suggestedTnved, @suggestedGender, @matchReason,
                    @status, @statusMessage, @cardJson, CURRENT_TIMESTAMP
                )
                ON CONFLICT(NmId) DO UPDATE SET
                    VendorCode = excluded.VendorCode,
                    Title = excluded.Title,
                    SubjectId = excluded.SubjectId,
                    SubjectName = excluded.SubjectName,
                    CurrentTnved = excluded.CurrentTnved,
                    CurrentGender = excluded.CurrentGender,
                    DetectedMaterial = excluded.DetectedMaterial,
                    SuggestedTnved = excluded.SuggestedTnved,
                    SuggestedGender = excluded.SuggestedGender,
                    MatchReason = excluded.MatchReason,
                    Status = excluded.Status,
                    StatusMessage = excluded.StatusMessage,
                    CardJson = excluded.CardJson,
                    UpdatedAt = CURRENT_TIMESTAMP";

            var pNmId = cmd.Parameters.Add("@nmId", SqliteType.Integer);
            var pVendorCode = cmd.Parameters.Add("@vendorCode", SqliteType.Text);
            var pTitle = cmd.Parameters.Add("@title", SqliteType.Text);
            var pSubjectId = cmd.Parameters.Add("@subjectId", SqliteType.Integer);
            var pSubjectName = cmd.Parameters.Add("@subjectName", SqliteType.Text);
            var pCurrentTnved = cmd.Parameters.Add("@currentTnved", SqliteType.Text);
            var pCurrentGender = cmd.Parameters.Add("@currentGender", SqliteType.Text);
            var pDetectedMaterial = cmd.Parameters.Add("@detectedMaterial", SqliteType.Text);
            var pSuggestedTnved = cmd.Parameters.Add("@suggestedTnved", SqliteType.Text);
            var pSuggestedGender = cmd.Parameters.Add("@suggestedGender", SqliteType.Text);
            var pMatchReason = cmd.Parameters.Add("@matchReason", SqliteType.Text);
            var pStatus = cmd.Parameters.Add("@status", SqliteType.Integer);
            var pStatusMessage = cmd.Parameters.Add("@statusMessage", SqliteType.Text);
            var pCardJson = cmd.Parameters.Add("@cardJson", SqliteType.Text);

            foreach (var item in items)
            {
                pNmId.Value = item.NmId;
                pVendorCode.Value = item.VendorCode ?? string.Empty;
                pTitle.Value = item.Title ?? string.Empty;
                pSubjectId.Value = item.SubjectId;
                pSubjectName.Value = item.SubjectName ?? string.Empty;
                pCurrentTnved.Value = item.CurrentTnved ?? string.Empty;
                pCurrentGender.Value = item.CurrentGender ?? string.Empty;
                pDetectedMaterial.Value = item.DetectedMaterial ?? string.Empty;
                pSuggestedTnved.Value = item.SuggestedTnved ?? string.Empty;
                pSuggestedGender.Value = item.SuggestedGender ?? string.Empty;
                pMatchReason.Value = item.MatchReason ?? string.Empty;
                pStatus.Value = (int)item.Status;
                pStatusMessage.Value = item.StatusMessage ?? string.Empty;
                pCardJson.Value = JsonSerializer.Serialize(item.Card);

                cmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public void UpdateAuditStatus(long nmId, AuditStatus status, string statusMessage, string? newTnved = null, string? newGender = null, WbCardItem? updatedCard = null)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            if (updatedCard != null)
            {
                cmd.CommandText = @"
                    UPDATE WbCardCache
                    SET Status = @status,
                        StatusMessage = @statusMessage,
                        CurrentTnved = COALESCE(@newTnved, CurrentTnved),
                        CurrentGender = COALESCE(@newGender, CurrentGender),
                        CardJson = @cardJson,
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE NmId = @nmId";
                cmd.Parameters.AddWithValue("@cardJson", JsonSerializer.Serialize(updatedCard));
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE WbCardCache
                    SET Status = @status,
                        StatusMessage = @statusMessage,
                        CurrentTnved = COALESCE(@newTnved, CurrentTnved),
                        CurrentGender = COALESCE(@newGender, CurrentGender),
                        UpdatedAt = CURRENT_TIMESTAMP
                    WHERE NmId = @nmId";
            }

            cmd.Parameters.AddWithValue("@status", (int)status);
            cmd.Parameters.AddWithValue("@statusMessage", statusMessage);
            cmd.Parameters.AddWithValue("@newTnved", (object?)newTnved ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@newGender", (object?)newGender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nmId", nmId);

            cmd.ExecuteNonQuery();
        }

        public void ClearCache()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM WbCardCache";
            cmd.ExecuteNonQuery();
        }
    }
}