using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using WbTnvedManager.Models;

namespace WbTnvedManager.Data
{
    public static class DatabaseInitializer
    {
        private static string? _resolvedDbPath;
        public static string DbPath
        {
            get
            {
                if (_resolvedDbPath != null) return _resolvedDbPath;
                try
                {
                    var basePath = AppDomain.CurrentDomain.BaseDirectory;
                    var testFile = Path.Combine(basePath, "write_test.tmp");
                    File.WriteAllText(testFile, "ok");
                    File.Delete(testFile);
                    _resolvedDbPath = Path.Combine(basePath, "tnved_matrix.db");
                }
                catch
                {
                    var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "WPost");
                    Directory.CreateDirectory(appDataDir);
                    _resolvedDbPath = Path.Combine(appDataDir, "tnved_matrix.db");
                }
                return _resolvedDbPath;
            }
        }
        public static string ConnectionString => $"Data Source={DbPath}";

        public static void InitializeDatabase()
        {
            using var connection = new SqliteConnection(ConnectionString);
            connection.Open();

            var createTableCmd = connection.CreateCommand();
            createTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS TnvedMatrix (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SubjectId INTEGER NOT NULL,
                    SubjectName TEXT NOT NULL,
                    Gender TEXT NOT NULL,
                    Material TEXT NOT NULL,
                    KnitType TEXT DEFAULT '',
                    TnvedCode TEXT NOT NULL,
                    Description TEXT DEFAULT '',
                    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                CREATE INDEX IF NOT EXISTS idx_tnved_subject_gender ON TnvedMatrix(SubjectId, Gender);
                CREATE INDEX IF NOT EXISTS idx_tnved_code ON TnvedMatrix(TnvedCode);
            ";
            createTableCmd.ExecuteNonQuery();

            // Check if seed data exists
            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM TnvedMatrix";
            long count = (long)(countCmd.ExecuteScalar() ?? 0);

            if (count < 20)
            {
                SeedDefaultMatrix(connection);
            }
        }

        private static void SeedDefaultMatrix(SqliteConnection connection)
        {
            using var transaction = connection.BeginTransaction();
            var insertCmd = connection.CreateCommand();
            insertCmd.Transaction = transaction;
            insertCmd.CommandText = @"
                INSERT OR REPLACE INTO TnvedMatrix (SubjectId, SubjectName, Gender, Material, KnitType, TnvedCode, Description)
                VALUES (@subjectId, @subjectName, @gender, @material, @knitType, @tnvedCode, @description)
            ";

            var pSubjectId = insertCmd.Parameters.Add("@subjectId", SqliteType.Integer);
            var pSubjectName = insertCmd.Parameters.Add("@subjectName", SqliteType.Text);
            var pGender = insertCmd.Parameters.Add("@gender", SqliteType.Text);
            var pMaterial = insertCmd.Parameters.Add("@material", SqliteType.Text);
            var pKnitType = insertCmd.Parameters.Add("@knitType", SqliteType.Text);
            var pTnvedCode = insertCmd.Parameters.Add("@tnvedCode", SqliteType.Text);
            var pDescription = insertCmd.Parameters.Add("@description", SqliteType.Text);

            var seedItems = GetDefaultSeeds();
            foreach (var item in seedItems)
            {
                pSubjectId.Value = item.SubjectId;
                pSubjectName.Value = item.SubjectName;
                pGender.Value = item.Gender;
                pMaterial.Value = item.Material;
                pKnitType.Value = item.KnitType;
                pTnvedCode.Value = item.TnvedCode;
                pDescription.Value = item.Description;
                insertCmd.ExecuteNonQuery();
            }

            transaction.Commit();
        }

        public static List<TnvedMatrixEntry> GetDefaultSeeds()
        {
            return new List<TnvedMatrixEntry>
            {
                // Футболки (T-shirts) - SubjectID 105
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6109100000", Description = "Футболки трикотажные мужские из хлопка" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6109100000", Description = "Футболки трикотажные женские из хлопка" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Мужской", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6109902000", Description = "Футболки трикотажные мужские из химических волокон" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Женский", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6109902000", Description = "Футболки трикотажные женские из химических волокон" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Унисекс", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6109100000", Description = "Футболки трикотажные унисекс из хлопка" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Мальчики", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6109100000", Description = "Футболки трикотажные для мальчиков из хлопка" },
                new() { SubjectId = 105, SubjectName = "Футболка", Gender = "Девочки", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6109100000", Description = "Футболки трикотажные для девочек из хлопка" },

                // Брюки (Trousers / Pants) - SubjectID 240
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Мужской", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6203423100", Description = "Брюки мужские хлопчатобумажные тканые" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Мужской", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6203431900", Description = "Брюки мужские из синтетических нитей" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Мужской", Material = "Шерсть", KnitType = "Ткань", TnvedCode = "6203411000", Description = "Брюки мужские из шерстяной пряжи" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204623100", Description = "Брюки женские хлопчатобумажные тканые" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Женский", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6204631800", Description = "Брюки женские из синтетических нитей" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Женский", Material = "Шерсть", KnitType = "Ткань", TnvedCode = "6204611000", Description = "Брюки женские из шерстяной пряжи" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6103420000", Description = "Брюки мужские трикотажные хлопчатобумажные" },
                new() { SubjectId = 240, SubjectName = "Брюки", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6104620000", Description = "Брюки женские трикотажные хлопчатобумажные" },

                // Джинсы (Jeans) - SubjectID 273
                new() { SubjectId = 273, SubjectName = "Джинсы", Gender = "Мужской", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6203423100", Description = "Джинсы мужские хлопчатобумажные из денима" },
                new() { SubjectId = 273, SubjectName = "Джинсы", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204623100", Description = "Джинсы женские хлопчатобумажные из денима" },
                new() { SubjectId = 273, SubjectName = "Джинсы", Gender = "Мальчики", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6203423100", Description = "Джинсы для мальчиков из денима" },
                new() { SubjectId = 273, SubjectName = "Джинсы", Gender = "Девочки", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204623100", Description = "Джинсы для девочек из денима" },

                // Платья (Dresses) - SubjectID 156
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204420000", Description = "Платья женские хлопчатобумажные тканые" },
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Женский", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6204430000", Description = "Платья женские из синтетических нитей" },
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Женский", Material = "Шерсть", KnitType = "Ткань", TnvedCode = "6204410000", Description = "Платья женские шерстяные" },
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6104420000", Description = "Платья женские трикотажные хлопчатобумажные" },
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Женский", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6104430000", Description = "Платья женские трикотажные синтетические" },
                new() { SubjectId = 156, SubjectName = "Платье", Gender = "Девочки", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204420000", Description = "Платья для девочек из хлопка" },

                // Юбки (Skirts) - SubjectID 157
                new() { SubjectId = 157, SubjectName = "Юбка", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204520000", Description = "Юбки женские хлопчатобумажные тканые" },
                new() { SubjectId = 157, SubjectName = "Юбка", Gender = "Женский", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6204530000", Description = "Юбки женские из синтетических нитей" },
                new() { SubjectId = 157, SubjectName = "Юбка", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6104520000", Description = "Юбки женские трикотажные хлопчатобумажные" },

                // Рубашки (Shirts) - SubjectID 138
                new() { SubjectId = 138, SubjectName = "Рубашка", Gender = "Мужской", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6205200000", Description = "Рубашки мужские из хлопчатобумажной ткани" },
                new() { SubjectId = 138, SubjectName = "Рубашка", Gender = "Мужской", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6205300000", Description = "Рубашки мужские из химических волокон" },
                new() { SubjectId = 138, SubjectName = "Рубашка", Gender = "Мужской", Material = "Лен", KnitType = "Ткань", TnvedCode = "6205908000", Description = "Рубашки мужские льняные" },

                // Блузки (Blouses) - SubjectID 139
                new() { SubjectId = 139, SubjectName = "Блузка", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6206300000", Description = "Блузки женские из хлопчатобумажной ткани" },
                new() { SubjectId = 139, SubjectName = "Блузка", Gender = "Женский", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6206400000", Description = "Блузки женские из химических волокон" },
                new() { SubjectId = 139, SubjectName = "Блузка", Gender = "Женский", Material = "Шелк", KnitType = "Ткань", TnvedCode = "6206100000", Description = "Блузки женские из натурального шелка" },
                new() { SubjectId = 139, SubjectName = "Блузка", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6106100000", Description = "Блузки женские трикотажные хлопчатобумажные" },

                // Худи / Толстовки (Hoodies / Sweatshirts) - SubjectID 248
                new() { SubjectId = 248, SubjectName = "Худи", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6110209100", Description = "Худи и толстовки трикотажные мужские из хлопка" },
                new() { SubjectId = 248, SubjectName = "Худи", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6110209900", Description = "Худи и толстовки трикотажные женские из хлопка" },
                new() { SubjectId = 248, SubjectName = "Худи", Gender = "Унисекс", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6110209900", Description = "Худи трикотажные унисекс из хлопка" },
                new() { SubjectId = 248, SubjectName = "Худи", Gender = "Мужской", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6110309100", Description = "Худи мужские трикотажные из хим волокон" },
                new() { SubjectId = 248, SubjectName = "Худи", Gender = "Женский", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6110309900", Description = "Худи женские трикотажные из хим волокон" },

                // Свитер / Джемпер (Sweaters) - SubjectID 247
                new() { SubjectId = 247, SubjectName = "Джемпер", Gender = "Мужской", Material = "Шерсть", KnitType = "Трикотаж", TnvedCode = "6110113000", Description = "Джемперы мужские шерстяные трикотажные" },
                new() { SubjectId = 247, SubjectName = "Джемпер", Gender = "Женский", Material = "Шерсть", KnitType = "Трикотаж", TnvedCode = "6110113000", Description = "Джемперы женские шерстяные трикотажные" },
                new() { SubjectId = 247, SubjectName = "Джемпер", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6110209100", Description = "Джемперы мужские хлопчатобумажные" },
                new() { SubjectId = 247, SubjectName = "Джемпер", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6110209900", Description = "Джемперы женские хлопчатобумажные" },

                // Куртки (Jackets) - SubjectID 217
                new() { SubjectId = 217, SubjectName = "Куртка", Gender = "Мужской", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6201400000", Description = "Куртки мужские из химических волокон" },
                new() { SubjectId = 217, SubjectName = "Куртка", Gender = "Женский", Material = "Синтетика", KnitType = "Ткань", TnvedCode = "6202400000", Description = "Куртки женские из химических волокон" },
                new() { SubjectId = 217, SubjectName = "Куртка", Gender = "Мужской", Material = "Кожа", KnitType = "Ткань", TnvedCode = "4203100001", Description = "Куртки мужские из натуральной кожи" },
                new() { SubjectId = 217, SubjectName = "Куртка", Gender = "Женский", Material = "Кожа", KnitType = "Ткань", TnvedCode = "4203100001", Description = "Куртки женские из натуральной кожи" },

                // Пальто (Coats) - SubjectID 216
                new() { SubjectId = 216, SubjectName = "Пальто", Gender = "Женский", Material = "Шерсть", KnitType = "Ткань", TnvedCode = "6202110000", Description = "Пальто женские шерстяные" },
                new() { SubjectId = 216, SubjectName = "Пальто", Gender = "Мужской", Material = "Шерсть", KnitType = "Ткань", TnvedCode = "6201110000", Description = "Пальто мужские шерстяные" },

                // Шорты (Shorts) - SubjectID 235
                new() { SubjectId = 235, SubjectName = "Шорты", Gender = "Мужской", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6203429000", Description = "Шорты мужские хлопчатобумажные" },
                new() { SubjectId = 235, SubjectName = "Шорты", Gender = "Женский", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6204629000", Description = "Шорты женские хлопчатобумажные" },

                // Носки (Socks) - SubjectID 197
                new() { SubjectId = 197, SubjectName = "Носки", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6115950000", Description = "Носки мужские из хлопчатобумажной пряжи" },
                new() { SubjectId = 197, SubjectName = "Носки", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6115950000", Description = "Носки женские из хлопчатоbuмажной пряжи" },
                new() { SubjectId = 197, SubjectName = "Носки", Gender = "Унисекс", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6115950000", Description = "Носки унисекс хлопковые" },

                // Нижнее белье / Трусы (Underwear) - SubjectID 189
                new() { SubjectId = 189, SubjectName = "Трусы", Gender = "Мужской", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6107110000", Description = "Трусы мужские трикотажные хлопчатобумажные" },
                new() { SubjectId = 189, SubjectName = "Трусы", Gender = "Женский", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6108210000", Description = "Трусы женские трикотажные хлопчатобумажные" },
                new() { SubjectId = 189, SubjectName = "Трусы", Gender = "Женский", Material = "Синтетика", KnitType = "Трикотаж", TnvedCode = "6108220000", Description = "Трусы женские из синтетических нитей" },

                // Обувь / Кроссовки (Shoes / Sneakers) - SubjectID 315
                new() { SubjectId = 315, SubjectName = "Кроссовки", Gender = "Мужской", Material = "Кожа", KnitType = "Ткань", TnvedCode = "6403999600", Description = "Обувь мужская с верхом из кожи" },
                new() { SubjectId = 315, SubjectName = "Кроссовки", Gender = "Женский", Material = "Кожа", KnitType = "Ткань", TnvedCode = "6403999800", Description = "Обувь женская с верхом из кожи" },
                new() { SubjectId = 315, SubjectName = "Кроссовки", Gender = "Мужской", Material = "Текстиль", KnitType = "Ткань", TnvedCode = "6404110000", Description = "Спортивная обувь мужская с текстильным верхом" },
                new() { SubjectId = 315, SubjectName = "Кроссовки", Gender = "Женский", Material = "Текстиль", KnitType = "Ткань", TnvedCode = "6404110000", Description = "Спортивная обувь женская с текстильным верхом" },

                // Головные уборы / Шапки (Caps / Hats) - SubjectID 211
                new() { SubjectId = 211, SubjectName = "Шапка", Gender = "Унисекс", Material = "Хлопок", KnitType = "Трикотаж", TnvedCode = "6505009000", Description = "Шапки трикотажные хлопковые" },
                new() { SubjectId = 211, SubjectName = "Шапка", Gender = "Унисекс", Material = "Шерсть", KnitType = "Трикотаж", TnvedCode = "6505009000", Description = "Шапки трикотажные шерстяные" },
                new() { SubjectId = 211, SubjectName = "Кепка", Gender = "Мужской", Material = "Хлопок", KnitType = "Ткань", TnvedCode = "6505003000", Description = "Кепки и бейсболки хлопчатобумажные" }
            };
        }
    }
}
