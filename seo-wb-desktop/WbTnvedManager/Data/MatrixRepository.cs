using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using WbTnvedManager.Models;

namespace WbTnvedManager.Data
{
    public class MatrixRepository
    {
        private readonly string _connectionString;

        public MatrixRepository(string? connectionString = null)
        {
            _connectionString = connectionString ?? DatabaseInitializer.ConnectionString;
        }

        public List<TnvedMatrixEntry> GetAll()
        {
            var list = new List<TnvedMatrixEntry>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Id, SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description, UpdatedAt FROM TnvedMatrix ORDER BY SubjectName, Gender, Material";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadEntry(reader));
            }
            return list;
        }

        public List<TnvedMatrixEntry> Search(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return GetAll();

            var list = new List<TnvedMatrixEntry>();
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description, UpdatedAt 
                FROM TnvedMatrix 
                WHERE SubjectName LIKE @q OR SubjectId LIKE @q OR Gender LIKE @q OR Material LIKE @q OR TnvedCode LIKE @q OR Description LIKE @q
                ORDER BY SubjectName, Gender, Material";
            cmd.Parameters.AddWithValue("@q", $"%{query}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(ReadEntry(reader));
            }
            return list;
        }

        public TnvedMatrixEntry? FindBestMatch(int subjectId, string gender, string material, string knitType = "")
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description, UpdatedAt 
                FROM TnvedMatrix 
                WHERE SubjectId = @subjectId 
                  AND (Gender = @gender OR Gender = 'Унисекс' OR @gender = '' OR @gender = 'Унисекс')
                  AND (Material LIKE @material OR @material = '' OR @material LIKE Material)
                ORDER BY 
                  CASE WHEN Gender = @gender THEN 1 ELSE 2 END,
                  CASE WHEN Material = @material THEN 1 ELSE 2 END,
                  CASE WHEN KnitType = @knitType THEN 1 ELSE 2 END
                LIMIT 1";

            cmd.Parameters.AddWithValue("@subjectId", subjectId);
            cmd.Parameters.AddWithValue("@gender", gender ?? string.Empty);
            cmd.Parameters.AddWithValue("@material", string.IsNullOrEmpty(material) ? "" : $"%{material}%");
            cmd.Parameters.AddWithValue("@knitType", knitType ?? string.Empty);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return ReadEntry(reader);
            }
            return null;
        }

        public void Insert(TnvedMatrixEntry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO TnvedMatrix (SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description, UpdatedAt)
                VALUES (@subjectId, @subjectName, @gender, @material, @knitType, @tnvedCode, @description, CURRENT_TIMESTAMP)";

            cmd.Parameters.AddWithValue("@subjectId", entry.SubjectId);
            cmd.Parameters.AddWithValue("@subjectName", entry.SubjectName ?? string.Empty);
            cmd.Parameters.AddWithValue("@gender", entry.Gender ?? string.Empty);
            cmd.Parameters.AddWithValue("@material", entry.Material ?? string.Empty);
            cmd.Parameters.AddWithValue("@knitType", entry.KnitType ?? string.Empty);
            cmd.Parameters.AddWithValue("@tnvedCode", entry.TnvedCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", entry.Description ?? string.Empty);

            cmd.ExecuteNonQuery();
        }

        public void Update(TnvedMatrixEntry entry)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                UPDATE TnvedMatrix 
                SET SubjectId = @subjectId,
                    SubjectName = @subjectName,
                    Gender = @gender,
                    Material = @material,
                    KnitType = @knitType,
                    TnvedCode = @tnvedCode,
                    Description = @description,
                    UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @id";

            cmd.Parameters.AddWithValue("@id", entry.Id);
            cmd.Parameters.AddWithValue("@subjectId", entry.SubjectId);
            cmd.Parameters.AddWithValue("@subjectName", entry.SubjectName ?? string.Empty);
            cmd.Parameters.AddWithValue("@gender", entry.Gender ?? string.Empty);
            cmd.Parameters.AddWithValue("@material", entry.Material ?? string.Empty);
            cmd.Parameters.AddWithValue("@knitType", entry.KnitType ?? string.Empty);
            cmd.Parameters.AddWithValue("@tnvedCode", entry.TnvedCode ?? string.Empty);
            cmd.Parameters.AddWithValue("@description", entry.Description ?? string.Empty);

            cmd.ExecuteNonQuery();
        }

        public void Delete(int id)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM TnvedMatrix WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();
        }

        public void ClearAll()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM TnvedMatrix";
            cmd.ExecuteNonQuery();
        }

        public void BulkInsertOrUpdate(IEnumerable<TnvedMatrixEntry> entries)
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();

            var checkCmd = connection.CreateCommand();
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = @"
                SELECT Id FROM TnvedMatrix 
                WHERE SubjectId = @subjectId AND Gender = @gender AND Material = @material AND KnitType = @knitType 
                LIMIT 1";
            var pSubjectId = checkCmd.Parameters.Add("@subjectId", SqliteType.Integer);
            var pGender = checkCmd.Parameters.Add("@gender", SqliteType.Text);
            var pMaterial = checkCmd.Parameters.Add("@material", SqliteType.Text);
            var pKnitType = checkCmd.Parameters.Add("@knitType", SqliteType.Text);

            var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT INTO TnvedMatrix (SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description, UpdatedAt)
                VALUES (@sId, @sName, @gen, @mat, @knit, @code, @desc, CURRENT_TIMESTAMP)";
            var insSId = insertCmd.Parameters.Add("@sId", SqliteType.Integer);
            var insSName = insertCmd.Parameters.Add("@sName", SqliteType.Text);
            var insGen = insertCmd.Parameters.Add("@gen", SqliteType.Text);
            var insMat = insertCmd.Parameters.Add("@mat", SqliteType.Text);
            var insKnit = insertCmd.Parameters.Add("@knit", SqliteType.Text);
            var insCode = insertCmd.Parameters.Add("@code", SqliteType.Text);
            var insDesc = insertCmd.Parameters.Add("@desc", SqliteType.Text);

            var updateCmd = connection.CreateCommand();
            updateCmd.Transaction = transaction;
            updateCmd.CommandText = @"
                UPDATE TnvedMatrix 
                SET TnvedCode = @code, Description = @desc, UpdatedAt = CURRENT_TIMESTAMP
                WHERE Id = @id";
            var updCode = updateCmd.Parameters.Add("@code", SqliteType.Text);
            var updDesc = updateCmd.Parameters.Add("@desc", SqliteType.Text);
            var updId = updateCmd.Parameters.Add("@id", SqliteType.Integer);

            foreach (var item in entries)
            {
                pSubjectId.Value = item.SubjectId;
                pGender.Value = item.Gender ?? string.Empty;
                pMaterial.Value = item.Material ?? string.Empty;
                pKnitType.Value = item.KnitType ?? string.Empty;

                var existingId = checkCmd.ExecuteScalar();
                if (existingId != null && existingId != DBNull.Value)
                {
                    updId.Value = Convert.ToInt32(existingId);
                    updCode.Value = item.TnvedCode;
                    updDesc.Value = item.Description ?? string.Empty;
                    updateCmd.ExecuteNonQuery();
                }
                else
                {
                    insSId.Value = item.SubjectId;
                    insSName.Value = item.SubjectName ?? string.Empty;
                    insGen.Value = item.Gender ?? string.Empty;
                    insMat.Value = item.Material ?? string.Empty;
                    insKnit.Value = item.KnitType ?? string.Empty;
                    insCode.Value = item.TnvedCode;
                    insDesc.Value = item.Description ?? string.Empty;
                    insertCmd.ExecuteNonQuery();
                }
            }

            transaction.Commit();
        }

        private static TnvedMatrixEntry ReadEntry(SqliteDataReader reader)
        {
            return new TnvedMatrixEntry
            {
                Id = reader.GetInt32(0),
                SubjectId = reader.GetInt32(1),
                SubjectName = reader.GetString(2),
                Gender = reader.GetString(3),
                Material = reader.GetString(4),
                KnitType = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                TnvedCode = reader.GetString(6),
                Description = reader.IsDBNull(7) ? string.Empty : reader.GetString(7),
                UpdatedAt = reader.IsDBNull(8) ? DateTime.UtcNow : reader.GetDateTime(8)
            };
        }
    }
}
