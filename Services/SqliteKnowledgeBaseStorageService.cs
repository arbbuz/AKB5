using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AsutpKnowledgeBase.Models;
using Microsoft.Data.Sqlite;

namespace AsutpKnowledgeBase.Services
{
    public sealed class SqliteKnowledgeBaseStorageService : IKnowledgeBaseStorageService
    {
        public const int CurrentDatabaseSchemaVersion = 17;

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private static readonly JsonSerializerOptions SnapshotJsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false
        };

        private readonly KnowledgeBaseSqliteConnectionFactory _connectionFactory;
        private readonly KnowledgeBaseExternalBackupService _externalBackupService;
        private readonly IAppLogger _logger;
        private readonly Func<DateTimeOffset> _clock;

        public SqliteKnowledgeBaseStorageService(
            string savePath,
            IAppLogger? logger = null,
            KnowledgeBaseSqliteConnectionFactory? connectionFactory = null,
            KnowledgeBaseExternalBackupService? externalBackupService = null,
            Func<DateTimeOffset>? clock = null)
        {
            SavePath = savePath;
            _logger = logger ?? NullAppLogger.Instance;
            _connectionFactory = connectionFactory ?? new KnowledgeBaseSqliteConnectionFactory();
            _clock = clock ?? (() => DateTimeOffset.Now);
            _externalBackupService = externalBackupService ?? new KnowledgeBaseExternalBackupService(_clock);
        }

        public string SavePath { get; set; }

        public KnowledgeBaseStorageLoadResult Load()
        {
            if (!File.Exists(SavePath))
            {
                return new KnowledgeBaseStorageLoadResult
                {
                    FileMissing = true,
                    SourcePath = SavePath
                };
            }

            try
            {
                var totalStopwatch = Stopwatch.StartNew();
                using var connection = _connectionFactory.OpenConnection(SavePath);
                var schemaStopwatch = Stopwatch.StartNew();
                EnsureSchema(connection);
                LogStartupTiming("sqlite-ensure-schema", schemaStopwatch.ElapsedMilliseconds);

                var loadStopwatch = Stopwatch.StartNew();
                var data = LoadData(connection);
                LogStartupTiming("sqlite-load-data", loadStopwatch.ElapsedMilliseconds);

                var normalizeStopwatch = Stopwatch.StartNew();
                SavedData normalizedData = KnowledgeBaseDataService.NormalizeSavedData(data);
                LogStartupTiming("sqlite-normalize-data", normalizeStopwatch.ElapsedMilliseconds);
                LogStartupTiming("sqlite-load-total", totalStopwatch.ElapsedMilliseconds);
                return new KnowledgeBaseStorageLoadResult
                {
                    Data = normalizedData,
                    SourcePath = SavePath
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteLoadFailed",
                    AppLogLevel.Error,
                    "SQLite database load failed.",
                    ex,
                    CreateProperties(("path", SavePath)));

                return new KnowledgeBaseStorageLoadResult
                {
                    SourcePath = SavePath,
                    ErrorMessage = ex.Message,
                    PrimaryErrorMessage = ex.Message
                };
            }
        }

        public bool Save(SavedData data, out string? errorMessage)
        {
            errorMessage = null;
            var normalizedData = KnowledgeBaseDataService.NormalizeSavedData(data);

            try
            {
                string? directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                KnowledgeBaseExternalBackupResult externalBackupResult =
                    _externalBackupService.CreateSqliteBackup(SavePath);
                if (!externalBackupResult.IsSuccess)
                {
                    errorMessage = $"Не удалось создать резервную копию базы .akb: {externalBackupResult.ErrorMessage}";
                    return false;
                }

                using var connection = _connectionFactory.OpenConnection(SavePath);
                EnsureSchema(connection);
                SavedData? previousData = HasExistingSavedData(connection)
                    ? KnowledgeBaseDataService.NormalizeSavedData(LoadData(connection))
                    : null;

                using var transaction = connection.BeginTransaction();

                if (previousData != null)
                {
                    InsertSnapshot(
                        connection,
                        transaction,
                        previousData,
                        "before-save",
                        "Автоматический снимок перед сохранением.");
                }

                ClearData(connection, transaction);
                RemoveNetworkStorageArtifacts(connection, transaction);
                SaveData(connection, transaction, normalizedData);
                InsertChangeLog(
                    connection,
                    transaction,
                    "save",
                    "База сохранена.",
                    externalBackupResult.BackupCreated
                        ? $"Файл: {Path.GetFullPath(SavePath)}; резервная копия: {externalBackupResult.BackupPath}"
                        : $"Файл: {Path.GetFullPath(SavePath)}");

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                _logger.Log(
                    "SqliteSaveFailed",
                    AppLogLevel.Error,
                    "SQLite database save failed.",
                    ex,
                    CreateProperties(("path", SavePath)));
                return false;
            }
        }

        public KnowledgeBaseSnapshotCreateResult CreateManualSnapshot(SavedData data, string note)
        {
            if (string.IsNullOrWhiteSpace(SavePath))
            {
                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Не указан путь к SQLite-базе для создания снимка."
                };
            }

            string normalizedNote = note?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedNote))
            {
                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Укажите примечание к снимку."
                };
            }

            try
            {
                SavedData normalizedData = KnowledgeBaseDataService.NormalizeSavedData(data);
                string? directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                using var connection = _connectionFactory.OpenConnection(SavePath);
                using var transaction = connection.BeginTransaction();
                EnsureSchema(connection, transaction);
                KnowledgeBaseSnapshotCreateResult result = InsertSnapshot(
                    connection,
                    transaction,
                    normalizedData,
                    "manual",
                    normalizedNote);
                InsertChangeLog(
                    connection,
                    transaction,
                    "manual-snapshot",
                    "Создан ручной снимок базы.",
                    normalizedNote);
                transaction.Commit();
                return result;
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteManualSnapshotFailed",
                    AppLogLevel.Error,
                    "SQLite manual snapshot creation failed.",
                    ex,
                    CreateProperties(("path", SavePath)));

                return new KnowledgeBaseSnapshotCreateResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseSnapshotListResult ListSnapshots()
        {
            if (!File.Exists(SavePath))
            {
                return new KnowledgeBaseSnapshotListResult
                {
                    IsSuccess = true,
                    SnapshotDirectoryPath = SavePath
                };
            }

            try
            {
                using var connection = _connectionFactory.OpenConnection(SavePath);
                EnsureSchema(connection);

                var snapshots = Query(
                    connection,
                    """
                    SELECT snapshot_id, created_at, kind, source_database_path, size_bytes, note
                    FROM snapshots
                    ORDER BY created_at DESC, snapshot_id DESC;
                    """,
                    reader =>
                    {
                        string snapshotId = GetString(reader, "snapshot_id");
                        DateTimeOffset createdAt = DateTimeOffset.Parse(
                            GetString(reader, "created_at"),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind);

                        return new KnowledgeBaseSnapshotEntry
                        {
                            SnapshotId = snapshotId,
                            SnapshotPath = BuildSnapshotReference(SavePath, snapshotId),
                            SnapshotFileName = snapshotId,
                            SourcePath = GetString(reader, "source_database_path"),
                            Kind = GetString(reader, "kind"),
                            Note = GetString(reader, "note"),
                            CreatedAt = createdAt,
                            SizeBytes = GetInt64(reader, "size_bytes"),
                            HasMetadata = true
                        };
                    });

                return new KnowledgeBaseSnapshotListResult
                {
                    IsSuccess = true,
                    SnapshotDirectoryPath = SavePath,
                    Snapshots = snapshots
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteSnapshotListFailed",
                    AppLogLevel.Error,
                    "SQLite snapshot list read failed.",
                    ex,
                    CreateProperties(("path", SavePath)));

                return new KnowledgeBaseSnapshotListResult
                {
                    IsSuccess = false,
                    SnapshotDirectoryPath = SavePath,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseSnapshotDataResult ReadSnapshotData(KnowledgeBaseSnapshotEntry snapshot)
        {
            string snapshotId = ResolveSnapshotId(snapshot);
            string snapshotPath = string.IsNullOrWhiteSpace(snapshot?.SnapshotPath)
                ? BuildSnapshotReference(SavePath, snapshotId)
                : snapshot.SnapshotPath;

            if (string.IsNullOrWhiteSpace(snapshotId))
            {
                return new KnowledgeBaseSnapshotDataResult
                {
                    IsSuccess = false,
                    SnapshotPath = snapshotPath,
                    ErrorMessage = "Не указан идентификатор снимка базы."
                };
            }

            if (!File.Exists(SavePath))
            {
                return new KnowledgeBaseSnapshotDataResult
                {
                    IsSuccess = false,
                    SnapshotPath = snapshotPath,
                    ErrorMessage = "Файл базы не найден."
                };
            }

            try
            {
                using var connection = _connectionFactory.OpenConnection(SavePath);
                EnsureSchema(connection);
                SavedData data = LoadSnapshotData(connection, snapshotId);

                return new KnowledgeBaseSnapshotDataResult
                {
                    IsSuccess = true,
                    SnapshotPath = BuildSnapshotReference(SavePath, snapshotId),
                    Data = data
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteSnapshotReadFailed",
                    AppLogLevel.Error,
                    "SQLite snapshot read failed.",
                    ex,
                    CreateProperties(("path", SavePath), ("snapshotId", snapshotId)));

                return new KnowledgeBaseSnapshotDataResult
                {
                    IsSuccess = false,
                    SnapshotPath = snapshotPath,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseSnapshotRestoreResult RestoreSnapshot(KnowledgeBaseSnapshotEntry snapshot)
        {
            string snapshotId = ResolveSnapshotId(snapshot);
            KnowledgeBaseSnapshotDataResult snapshotDataResult = ReadSnapshotData(snapshot);
            if (!snapshotDataResult.IsSuccess || snapshotDataResult.Data == null)
            {
                return new KnowledgeBaseSnapshotRestoreResult
                {
                    IsSuccess = false,
                    SnapshotPath = snapshotDataResult.SnapshotPath,
                    ErrorMessage = snapshotDataResult.ErrorMessage ?? "Не удалось прочитать снимок базы."
                };
            }

            try
            {
                string? directory = Path.GetDirectoryName(SavePath);
                if (!string.IsNullOrWhiteSpace(directory))
                    Directory.CreateDirectory(directory);

                KnowledgeBaseExternalBackupResult externalBackupResult =
                    _externalBackupService.CreateSqliteBackup(SavePath);
                if (!externalBackupResult.IsSuccess)
                {
                    return new KnowledgeBaseSnapshotRestoreResult
                    {
                        IsSuccess = false,
                        SnapshotPath = snapshotDataResult.SnapshotPath,
                        ErrorMessage = $"Не удалось создать резервную копию базы .akb: {externalBackupResult.ErrorMessage}"
                    };
                }

                SavedData restoredData = KnowledgeBaseDataService.NormalizeSavedData(snapshotDataResult.Data);
                using var connection = _connectionFactory.OpenConnection(SavePath);
                EnsureSchema(connection);
                SavedData? currentData = HasExistingSavedData(connection)
                    ? KnowledgeBaseDataService.NormalizeSavedData(LoadData(connection))
                    : null;

                using var transaction = connection.BeginTransaction();
                KnowledgeBaseSnapshotCreateResult protectiveSnapshot = currentData == null
                    ? new KnowledgeBaseSnapshotCreateResult { IsSkipped = true }
                    : InsertSnapshot(
                        connection,
                        transaction,
                        currentData,
                        "before-restore",
                        $"Защитный снимок перед восстановлением {snapshotId}.");

                ClearData(connection, transaction);
                RemoveNetworkStorageArtifacts(connection, transaction);
                SaveData(connection, transaction, restoredData);
                InsertChangeLog(
                    connection,
                    transaction,
                    "restore",
                    "База восстановлена из снимка.",
                    externalBackupResult.BackupCreated
                        ? $"Снимок: {snapshotId}; резервная копия: {externalBackupResult.BackupPath}"
                        : $"Снимок: {snapshotId}");

                transaction.Commit();
                return new KnowledgeBaseSnapshotRestoreResult
                {
                    IsSuccess = true,
                    SnapshotPath = snapshotDataResult.SnapshotPath,
                    ProtectiveSnapshotPath = protectiveSnapshot.SnapshotPath,
                    RestoredData = restoredData
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteSnapshotRestoreFailed",
                    AppLogLevel.Error,
                    "SQLite snapshot restore failed.",
                    ex,
                    CreateProperties(("path", SavePath), ("snapshotId", snapshotId)));

                return new KnowledgeBaseSnapshotRestoreResult
                {
                    IsSuccess = false,
                    SnapshotPath = snapshotDataResult.SnapshotPath,
                    ErrorMessage = ex.Message
                };
            }
        }

        public void EnsureSchema()
        {
            using var connection = _connectionFactory.OpenConnection(SavePath);
            EnsureSchema(connection);
        }

        public void WriteAppMetadata(IReadOnlyDictionary<string, string> metadata)
        {
            if (metadata.Count == 0)
                return;

            using var connection = _connectionFactory.OpenConnection(SavePath);
            using var transaction = connection.BeginTransaction();
            EnsureSchema(connection, transaction);

            foreach (var pair in metadata)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;

                UpsertMetadata(connection, transaction, pair.Key, pair.Value);
            }

            if (metadata.TryGetValue("last_migration_status", out string? migrationStatus) &&
                string.Equals(migrationStatus, "success", StringComparison.OrdinalIgnoreCase))
            {
                metadata.TryGetValue("last_migration_source_path", out string? sourcePath);
                metadata.TryGetValue("last_migration_safety_export_path", out string? safetyExportPath);
                InsertChangeLog(
                    connection,
                    transaction,
                    "migration",
                    "База перенесена из legacy JSON в SQLite.",
                    $"Источник: {sourcePath}; контрольный JSON: {safetyExportPath}");
            }

            transaction.Commit();
        }

        public KnowledgeBaseChangeLogAppendResult AppendChangeLog(
            string actionKind,
            string summary,
            string details = "")
        {
            if (!File.Exists(SavePath))
            {
                return new KnowledgeBaseChangeLogAppendResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Файл базы не найден."
                };
            }

            try
            {
                using var connection = _connectionFactory.OpenConnection(SavePath);
                using var transaction = connection.BeginTransaction();
                EnsureSchema(connection, transaction);
                InsertChangeLog(connection, transaction, actionKind, summary, details);
                transaction.Commit();
                return new KnowledgeBaseChangeLogAppendResult { IsSuccess = true };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteChangeLogAppendFailed",
                    AppLogLevel.Error,
                    "SQLite change log append failed.",
                    ex,
                    CreateProperties(("path", SavePath), ("actionKind", actionKind)));

                return new KnowledgeBaseChangeLogAppendResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        public KnowledgeBaseChangeLogListResult ListChangeLog()
        {
            if (!File.Exists(SavePath))
            {
                return new KnowledgeBaseChangeLogListResult
                {
                    IsSuccess = true
                };
            }

            try
            {
                using var connection = _connectionFactory.OpenConnection(SavePath);
                EnsureSchema(connection);

                var entries = Query(
                    connection,
                    """
                    SELECT change_id, created_at, action_kind, summary, details
                    FROM change_log
                    ORDER BY created_at DESC, change_id DESC;
                    """,
                    reader => new KnowledgeBaseChangeLogEntry
                    {
                        ChangeId = GetString(reader, "change_id"),
                        CreatedAt = DateTimeOffset.Parse(
                            GetString(reader, "created_at"),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind),
                        ActionKind = GetString(reader, "action_kind"),
                        Summary = GetString(reader, "summary"),
                        Details = GetString(reader, "details")
                    });

                return new KnowledgeBaseChangeLogListResult
                {
                    IsSuccess = true,
                    Entries = entries
                };
            }
            catch (Exception ex)
            {
                _logger.Log(
                    "SqliteChangeLogListFailed",
                    AppLogLevel.Error,
                    "SQLite change log list read failed.",
                    ex,
                    CreateProperties(("path", SavePath)));

                return new KnowledgeBaseChangeLogListResult
                {
                    IsSuccess = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private static void EnsureSchema(SqliteConnection connection, SqliteTransaction? transaction = null)
        {
            foreach (string statement in SchemaStatements)
                ExecuteNonQuery(connection, transaction, statement);

            EnsureCompositionEntriesRackNumberColumn(connection, transaction);
            EnsureCompositionEntriesHardwareColumns(connection, transaction);
            EnsureNodesNetworkTopologyColumn(connection, transaction);
            EnsureObjectTemplatesCompositionRacksColumn(connection, transaction);
            EnsureMaintenanceYearScheduleHoursColumn(connection, transaction);
            EnsureConfigActDocumentsDirectoryPathColumn(connection, transaction);
            EnsureActsMvpColumns(connection, transaction);
            ExecuteNonQuery(connection, transaction, $"PRAGMA user_version={CurrentDatabaseSchemaVersion};");
        }

        private static void EnsureConfigActDocumentsDirectoryPathColumn(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            if (ColumnExists(connection, transaction, "config", "act_documents_directory_path"))
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE config ADD COLUMN act_documents_directory_path TEXT NOT NULL DEFAULT 'Documents\\Acts';");
        }

        private static void EnsureCompositionEntriesRackNumberColumn(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            if (ColumnExists(connection, transaction, "composition_entries", "rack_number"))
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE composition_entries ADD COLUMN rack_number INTEGER NOT NULL DEFAULT 0;");
        }

        private static void EnsureCompositionEntriesHardwareColumns(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            (string ColumnName, string Definition)[] columns =
            [
                ("order_number", "order_number TEXT NOT NULL DEFAULT ''"),
                ("firmware", "firmware TEXT NOT NULL DEFAULT ''"),
                ("mpi_dp_pn_address", "mpi_dp_pn_address TEXT NOT NULL DEFAULT ''"),
                ("input_address", "input_address TEXT NOT NULL DEFAULT ''"),
                ("output_address", "output_address TEXT NOT NULL DEFAULT ''"),
                ("comment_text", "comment_text TEXT NOT NULL DEFAULT ''"),
                ("interface_rows", "interface_rows TEXT NOT NULL DEFAULT ''")
            ];

            foreach ((string columnName, string definition) in columns)
            {
                if (ColumnExists(connection, transaction, "composition_entries", columnName))
                    continue;

                ExecuteNonQuery(
                    connection,
                    transaction,
                    $"ALTER TABLE composition_entries ADD COLUMN {definition};");
            }
        }

        private static void EnsureObjectTemplatesCompositionRacksColumn(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            if (ColumnExists(connection, transaction, "object_templates", "composition_racks_json"))
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE object_templates ADD COLUMN composition_racks_json TEXT NOT NULL DEFAULT '[]';");
        }

        private static void EnsureNodesNetworkTopologyColumn(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            if (ColumnExists(connection, transaction, "nodes", "details_network_topology_json"))
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE nodes ADD COLUMN details_network_topology_json TEXT NOT NULL DEFAULT '{}';");
        }

        private static void EnsureMaintenanceYearScheduleHoursColumn(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            if (ColumnExists(connection, transaction, "maintenance_year_schedule_entries", "hours"))
                return;

            ExecuteNonQuery(
                connection,
                transaction,
                "ALTER TABLE maintenance_year_schedule_entries ADD COLUMN hours INTEGER NOT NULL DEFAULT 0;");
        }

        private static void EnsureActsMvpColumns(
            SqliteConnection connection,
            SqliteTransaction? transaction)
        {
            (string ColumnName, string Definition)[] columns =
            [
                ("workshop_name", "workshop_name TEXT NOT NULL DEFAULT ''"),
                ("object_name_snapshot", "object_name_snapshot TEXT NOT NULL DEFAULT ''"),
                ("equipment_name", "equipment_name TEXT NOT NULL DEFAULT ''"),
                ("approver_name", "approver_name TEXT NOT NULL DEFAULT ''"),
                ("approver_position", "approver_position TEXT NOT NULL DEFAULT ''")
            ];

            foreach ((string columnName, string definition) in columns)
            {
                if (ColumnExists(connection, transaction, "acts", columnName))
                    continue;

                ExecuteNonQuery(
                    connection,
                    transaction,
                    $"ALTER TABLE acts ADD COLUMN {definition};");
            }
        }

        private static bool ColumnExists(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            string tableName,
            string columnName)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = $"PRAGMA table_info({tableName});";
            using SqliteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (string.Equals(GetString(reader, "name"), columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static void ClearData(SqliteConnection connection, SqliteTransaction transaction)
        {
            foreach (string table in DataTablesInDeleteOrder)
                ExecuteNonQuery(connection, transaction, $"DELETE FROM {table};");
        }

        private static void RemoveNetworkStorageArtifacts(SqliteConnection connection, SqliteTransaction transaction)
        {
            DropColumnIfExists(connection, transaction, "composition_racks", "network_link");
            DropColumnIfExists(connection, transaction, "object_templates", "network_file_references_json");
            DropColumnIfExists(connection, transaction, "object_templates", "network_interface_stubs_json");

            ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS network_connections;");
            ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS network_interfaces;");
            ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS network_devices;");
            ExecuteNonQuery(connection, transaction, "DROP TABLE IF EXISTS network_file_references;");
        }

        private static void DropColumnIfExists(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string tableName,
            string columnName)
        {
            if (!ColumnExists(connection, transaction, tableName, columnName))
                return;

            ExecuteNonQuery(connection, transaction, $"ALTER TABLE {tableName} DROP COLUMN {columnName};");
        }

        private static bool HasExistingSavedData(SqliteConnection connection)
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT
                    (SELECT COUNT(*) FROM config) +
                    (SELECT COUNT(*) FROM workshops) +
                    (SELECT COUNT(*) FROM nodes);
                """;
            long count = Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
            return count > 0;
        }

        private static void SaveData(
            SqliteConnection connection,
            SqliteTransaction transaction,
            SavedData data)
        {
            UpsertMetadata(connection, transaction, "schema_version", CurrentDatabaseSchemaVersion.ToString(CultureInfo.InvariantCulture));
            UpsertMetadata(connection, transaction, "saved_data_schema_version", data.SchemaVersion.ToString(CultureInfo.InvariantCulture));
            UpsertMetadata(connection, transaction, "last_workshop", data.LastWorkshop);

            InsertConfig(connection, transaction, data.Config);
            InsertWorkshops(connection, transaction, data.Workshops);
            InsertCompositionRacks(connection, transaction, data.CompositionRacks);
            InsertCompositionEntries(connection, transaction, data.CompositionEntries);
            InsertDocumentLinks(connection, transaction, data.DocumentLinks);
            InsertSoftwareRecords(connection, transaction, data.SoftwareRecords);
            InsertMaintenanceScheduleProfiles(connection, transaction, data.MaintenanceScheduleProfiles);
            InsertEquipmentCatalogItems(connection, transaction, data.EquipmentCatalogItems);
            InsertObjectTemplates(connection, transaction, data.ObjectTemplates);
            InsertActs(connection, transaction, data.Acts);
            InsertActExecutors(connection, transaction, data.ActExecutors);
            InsertActDocuments(connection, transaction, data.ActDocuments);
            InsertActNumberSequences(connection, transaction, data.ActNumberSequences);
        }

        private static SavedData LoadData(SqliteConnection connection)
        {
            var metadata = LoadMetadata(connection);
            var config = LoadConfig(connection);
            var data = new SavedData
            {
                SchemaVersion = ParseInt(metadata.TryGetValue("saved_data_schema_version", out string? schemaVersion)
                    ? schemaVersion
                    : null,
                    SavedData.CurrentSchemaVersion),
                Config = config,
                Workshops = LoadWorkshops(connection),
                CompositionRacks = LoadCompositionRacks(connection),
                CompositionEntries = LoadCompositionEntries(connection),
                DocumentLinks = LoadDocumentLinks(connection),
                SoftwareRecords = LoadSoftwareRecords(connection),
                MaintenanceScheduleProfiles = LoadMaintenanceScheduleProfiles(connection),
                EquipmentCatalogItems = LoadEquipmentCatalogItems(connection),
                ObjectTemplates = LoadObjectTemplates(connection),
                Acts = LoadActs(connection),
                ActExecutors = LoadActExecutors(connection),
                ActDocuments = LoadActDocuments(connection),
                ActNumberSequences = LoadActNumberSequences(connection),
                LastWorkshop = metadata.TryGetValue("last_workshop", out string? lastWorkshop)
                    ? lastWorkshop
                    : string.Empty
            };

            return data;
        }

        private static void InsertConfig(
            SqliteConnection connection,
            SqliteTransaction transaction,
            KbConfig config)
        {
            Execute(
                connection,
                transaction,
                """
                INSERT INTO config (id, max_levels, act_documents_directory_path)
                VALUES (1, @max_levels, @act_documents_directory_path);
                """,
                ("@max_levels", config.MaxLevels),
                ("@act_documents_directory_path", config.ActDocumentsDirectoryPath));

            for (int i = 0; i < config.LevelNames.Count; i++)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO config_level_names (position_order, level_name)
                    VALUES (@position_order, @level_name);
                    """,
                    ("@position_order", i),
                    ("@level_name", config.LevelNames[i]));
            }

            foreach (KbProductionCalendarYear calendarYear in config.ProductionCalendarYears)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO production_calendar_years (year)
                    VALUES (@year);
                    """,
                    ("@year", calendarYear.Year));

                InsertProductionCalendarDates(
                    connection,
                    transaction,
                    calendarYear.Year,
                    "non_working",
                    calendarYear.AdditionalNonWorkingDays);
                InsertProductionCalendarDates(
                    connection,
                    transaction,
                    calendarYear.Year,
                    "working",
                    calendarYear.AdditionalWorkingDays);
            }
        }

        private static KbConfig LoadConfig(SqliteConnection connection)
        {
            var config = KnowledgeBaseDataService.CreateDefaultConfig();

            using (var command = connection.CreateCommand())
            {
                command.CommandText = "SELECT max_levels, act_documents_directory_path FROM config WHERE id = 1;";
                using SqliteDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    config.MaxLevels = GetInt(reader, "max_levels");
                    config.ActDocumentsDirectoryPath = GetString(reader, "act_documents_directory_path");
                }
            }

            config.LevelNames = Query(
                connection,
                "SELECT level_name FROM config_level_names ORDER BY position_order;",
                static reader => GetString(reader, "level_name"))
                .ToList();

            config.ProductionCalendarYears = LoadProductionCalendarYears(connection);
            return config;
        }

        private static void InsertProductionCalendarDates(
            SqliteConnection connection,
            SqliteTransaction transaction,
            int year,
            string kind,
            IReadOnlyList<DateOnly> dates)
        {
            for (int i = 0; i < dates.Count; i++)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO production_calendar_dates (year, date_kind, date_value, position_order)
                    VALUES (@year, @date_kind, @date_value, @position_order);
                    """,
                    ("@year", year),
                    ("@date_kind", kind),
                    ("@date_value", FormatDateOnly(dates[i])),
                    ("@position_order", i));
            }
        }

        private static List<KbProductionCalendarYear> LoadProductionCalendarYears(SqliteConnection connection)
        {
            var years = Query(
                connection,
                "SELECT year FROM production_calendar_years ORDER BY year;",
                static reader => new KbProductionCalendarYear
                {
                    Year = GetInt(reader, "year")
                }).ToList();

            foreach (KbProductionCalendarYear year in years)
            {
                year.AdditionalNonWorkingDays = LoadProductionCalendarDates(connection, year.Year, "non_working");
                year.AdditionalWorkingDays = LoadProductionCalendarDates(connection, year.Year, "working");
            }

            return years;
        }

        private static List<DateOnly> LoadProductionCalendarDates(
            SqliteConnection connection,
            int year,
            string kind) =>
            Query(
                connection,
                """
                SELECT date_value
                FROM production_calendar_dates
                WHERE year = @year AND date_kind = @date_kind
                ORDER BY position_order;
                """,
                static reader => ParseDateOnly(GetString(reader, "date_value")),
                ("@year", year),
                ("@date_kind", kind))
                .ToList();

        private static void InsertWorkshops(
            SqliteConnection connection,
            SqliteTransaction transaction,
            Dictionary<string, List<KbNode>> workshops)
        {
            int workshopOrder = 0;
            foreach (var pair in workshops)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO workshops (workshop_name, position_order)
                    VALUES (@workshop_name, @position_order);
                    """,
                    ("@workshop_name", pair.Key),
                    ("@position_order", workshopOrder));

                long workshopId = GetLastInsertRowId(connection, transaction);
                InsertNodes(connection, transaction, workshopId, parentNodeId: null, pair.Value);
                workshopOrder++;
            }
        }

        private static Dictionary<string, List<KbNode>> LoadWorkshops(SqliteConnection connection)
        {
            var workshops = new Dictionary<string, List<KbNode>>(KnowledgeBaseDataService.WorkshopNameComparer);
            var workshopRows = Query(
                connection,
                "SELECT workshop_id, workshop_name FROM workshops ORDER BY position_order;",
                static reader => new
                {
                    WorkshopId = GetInt64(reader, "workshop_id"),
                    Name = GetString(reader, "workshop_name")
                });

            foreach (var row in workshopRows)
                workshops[row.Name] = LoadNodes(connection, row.WorkshopId);

            return workshops;
        }

        private static void InsertNodes(
            SqliteConnection connection,
            SqliteTransaction transaction,
            long workshopId,
            string? parentNodeId,
            IReadOnlyList<KbNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                KbNode node = nodes[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO nodes (
                        node_id, workshop_id, parent_node_id, position_order, name, level_index, node_type,
                        details_description, details_location, details_inventory_number, details_photo_path,
                        details_ip_address, details_schema_link, details_network_topology_json)
                    VALUES (
                        @node_id, @workshop_id, @parent_node_id, @position_order, @name, @level_index, @node_type,
                        @details_description, @details_location, @details_inventory_number, @details_photo_path,
                        @details_ip_address, @details_schema_link, @details_network_topology_json);
                    """,
                    ("@node_id", node.NodeId),
                    ("@workshop_id", workshopId),
                    ("@parent_node_id", parentNodeId),
                    ("@position_order", i),
                    ("@name", node.Name),
                    ("@level_index", node.LevelIndex),
                    ("@node_type", (int)node.NodeType),
                    ("@details_description", node.Details.Description),
                    ("@details_location", node.Details.Location),
                    ("@details_inventory_number", node.Details.InventoryNumber),
                    ("@details_photo_path", node.Details.PhotoPath),
                    ("@details_ip_address", node.Details.IpAddress),
                    ("@details_schema_link", node.Details.SchemaLink),
                    ("@details_network_topology_json", SerializeJson(node.Details.NetworkTopology)));

                InsertNodes(connection, transaction, workshopId, node.NodeId, node.Children);
            }
        }

        private static List<KbNode> LoadNodes(SqliteConnection connection, long workshopId)
        {
            var rows = Query(
                connection,
                """
                SELECT *
                FROM nodes
                WHERE workshop_id = @workshop_id
                ORDER BY COALESCE(parent_node_id, ''), position_order;
                """,
                static reader => new NodeRow(
                    GetString(reader, "node_id"),
                    GetNullableString(reader, "parent_node_id"),
                    GetInt(reader, "position_order"),
                    new KbNode
                    {
                        NodeId = GetString(reader, "node_id"),
                        Name = GetString(reader, "name"),
                        LevelIndex = GetInt(reader, "level_index"),
                        NodeType = ToEnum(GetInt(reader, "node_type"), KbNodeType.Unknown),
                        Details = new KbNodeDetails
                        {
                            Description = GetString(reader, "details_description"),
                            Location = GetString(reader, "details_location"),
                            InventoryNumber = GetString(reader, "details_inventory_number"),
                            PhotoPath = GetString(reader, "details_photo_path"),
                            IpAddress = GetString(reader, "details_ip_address"),
                            SchemaLink = GetString(reader, "details_schema_link"),
                            NetworkTopology = DeserializeJson<KbNetworkTopology>(GetString(reader, "details_network_topology_json"))
                        }
                    }),
                ("@workshop_id", workshopId))
                .OrderBy(static row => row.PositionOrder)
                .ToList();

            var byId = rows.ToDictionary(static row => row.NodeId, static row => row.Node, StringComparer.Ordinal);
            var roots = new List<KbNode>();
            foreach (NodeRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ParentNodeId))
                {
                    roots.Add(row.Node);
                    continue;
                }

                if (byId.TryGetValue(row.ParentNodeId, out KbNode? parent))
                    parent.Children.Add(row.Node);
            }

            return roots;
        }

        private static void InsertCompositionRacks(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbCompositionRack> racks)
        {
            for (int i = 0; i < racks.Count; i++)
            {
                KbCompositionRack rack = racks[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO composition_racks (
                        rack_id, entry_order, parent_node_id, rack_number, sort_order, rack_type, label,
                        notes, properties_json)
                    VALUES (
                        @rack_id, @entry_order, @parent_node_id, @rack_number, @sort_order, @rack_type, @label,
                        @notes, @properties_json);
                    """,
                    ("@rack_id", rack.RackId),
                    ("@entry_order", i),
                    ("@parent_node_id", rack.ParentNodeId),
                    ("@rack_number", rack.RackNumber),
                    ("@sort_order", rack.SortOrder),
                    ("@rack_type", rack.RackType),
                    ("@label", rack.Label),
                    ("@notes", rack.Notes),
                    ("@properties_json", SerializeJson(rack.Properties)));
            }
        }

        private static List<KbCompositionRack> LoadCompositionRacks(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM composition_racks ORDER BY entry_order;",
                static reader => new KbCompositionRack
                {
                    RackId = GetString(reader, "rack_id"),
                    ParentNodeId = GetString(reader, "parent_node_id"),
                    RackNumber = GetInt(reader, "rack_number"),
                    SortOrder = GetInt(reader, "sort_order"),
                    RackType = GetString(reader, "rack_type"),
                    Label = GetString(reader, "label"),
                    Notes = GetString(reader, "notes"),
                    Properties = DeserializeJson<List<KbCompositionRackProperty>>(GetString(reader, "properties_json"))
                }).ToList();

        private static void InsertCompositionEntries(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbCompositionEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                KbCompositionEntry entry = entries[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO composition_entries (
                        entry_id, entry_order, parent_node_id, rack_number, slot_number, position_order, component_type, model,
                        order_number, firmware, mpi_dp_pn_address, input_address, output_address, comment_text, interface_rows,
                        ip_address, last_calibration_at, next_calibration_at, notes)
                    VALUES (
                        @entry_id, @entry_order, @parent_node_id, @rack_number, @slot_number, @position_order, @component_type, @model,
                        @order_number, @firmware, @mpi_dp_pn_address, @input_address, @output_address, @comment_text, @interface_rows,
                        @ip_address, @last_calibration_at, @next_calibration_at, @notes);
                    """,
                    ("@entry_id", entry.EntryId),
                    ("@entry_order", i),
                    ("@parent_node_id", entry.ParentNodeId),
                    ("@rack_number", entry.RackNumber),
                    ("@slot_number", entry.SlotNumber),
                    ("@position_order", entry.PositionOrder),
                    ("@component_type", entry.ComponentType),
                    ("@model", entry.Model),
                    ("@order_number", entry.OrderNumber),
                    ("@firmware", entry.Firmware),
                    ("@mpi_dp_pn_address", entry.MpiDpPnAddress),
                    ("@input_address", entry.InputAddress),
                    ("@output_address", entry.OutputAddress),
                    ("@comment_text", entry.Comment),
                    ("@interface_rows", entry.InterfaceRows),
                    ("@ip_address", entry.IpAddress),
                    ("@last_calibration_at", FormatDateTime(entry.LastCalibrationAt)),
                    ("@next_calibration_at", FormatDateTime(entry.NextCalibrationAt)),
                    ("@notes", entry.Notes));
            }
        }

        private static List<KbCompositionEntry> LoadCompositionEntries(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM composition_entries ORDER BY entry_order;",
                static reader => new KbCompositionEntry
                {
                    EntryId = GetString(reader, "entry_id"),
                    ParentNodeId = GetString(reader, "parent_node_id"),
                    RackNumber = GetInt(reader, "rack_number"),
                    SlotNumber = GetNullableInt(reader, "slot_number"),
                    PositionOrder = GetInt(reader, "position_order"),
                    ComponentType = GetString(reader, "component_type"),
                    Model = GetString(reader, "model"),
                    OrderNumber = GetString(reader, "order_number"),
                    Firmware = GetString(reader, "firmware"),
                    MpiDpPnAddress = GetString(reader, "mpi_dp_pn_address"),
                    InputAddress = GetString(reader, "input_address"),
                    OutputAddress = GetString(reader, "output_address"),
                    Comment = GetString(reader, "comment_text"),
                    InterfaceRows = GetString(reader, "interface_rows"),
                    IpAddress = GetString(reader, "ip_address"),
                    LastCalibrationAt = GetNullableDateTime(reader, "last_calibration_at"),
                    NextCalibrationAt = GetNullableDateTime(reader, "next_calibration_at"),
                    Notes = GetString(reader, "notes")
                }).ToList();

        private static void InsertDocumentLinks(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbDocumentLink> links)
        {
            for (int i = 0; i < links.Count; i++)
            {
                KbDocumentLink link = links[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO document_links (document_id, entry_order, owner_node_id, kind, title, path, updated_at)
                    VALUES (@document_id, @entry_order, @owner_node_id, @kind, @title, @path, @updated_at);
                    """,
                    ("@document_id", link.DocumentId),
                    ("@entry_order", i),
                    ("@owner_node_id", link.OwnerNodeId),
                    ("@kind", (int)link.Kind),
                    ("@title", link.Title),
                    ("@path", link.Path),
                    ("@updated_at", FormatDateTime(link.UpdatedAt)));
            }
        }

        private static List<KbDocumentLink> LoadDocumentLinks(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM document_links ORDER BY entry_order;",
                static reader => new KbDocumentLink
                {
                    DocumentId = GetString(reader, "document_id"),
                    OwnerNodeId = GetString(reader, "owner_node_id"),
                    Kind = ToEnum(GetInt(reader, "kind"), KbDocumentKind.Manual),
                    Title = GetString(reader, "title"),
                    Path = GetString(reader, "path"),
                    UpdatedAt = GetNullableDateTime(reader, "updated_at")
                }).ToList();

        private static void InsertSoftwareRecords(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbSoftwareRecord> records)
        {
            for (int i = 0; i < records.Count; i++)
            {
                KbSoftwareRecord record = records[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO software_records (
                        software_id, entry_order, owner_node_id, title, path, added_at, last_changed_at, last_backup_at, notes)
                    VALUES (
                        @software_id, @entry_order, @owner_node_id, @title, @path, @added_at, @last_changed_at, @last_backup_at, @notes);
                    """,
                    ("@software_id", record.SoftwareId),
                    ("@entry_order", i),
                    ("@owner_node_id", record.OwnerNodeId),
                    ("@title", record.Title),
                    ("@path", record.Path),
                    ("@added_at", FormatDateTime(record.AddedAt)),
                    ("@last_changed_at", FormatDateTime(record.LastChangedAt)),
                    ("@last_backup_at", FormatDateTime(record.LastBackupAt)),
                    ("@notes", record.Notes));
            }
        }

        private static List<KbSoftwareRecord> LoadSoftwareRecords(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM software_records ORDER BY entry_order;",
                static reader => new KbSoftwareRecord
                {
                    SoftwareId = GetString(reader, "software_id"),
                    OwnerNodeId = GetString(reader, "owner_node_id"),
                    Title = GetString(reader, "title"),
                    Path = GetString(reader, "path"),
                    AddedAt = GetNullableDateTime(reader, "added_at"),
                    LastChangedAt = GetNullableDateTime(reader, "last_changed_at"),
                    LastBackupAt = GetNullableDateTime(reader, "last_backup_at"),
                    Notes = GetString(reader, "notes")
                }).ToList();

        private static void InsertMaintenanceScheduleProfiles(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbMaintenanceScheduleProfile> profiles)
        {
            for (int i = 0; i < profiles.Count; i++)
            {
                KbMaintenanceScheduleProfile profile = profiles[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO maintenance_schedule_profiles (
                        maintenance_profile_id, entry_order, owner_node_id, is_included_in_schedule, to1_hours, to2_hours, to3_hours)
                    VALUES (
                        @maintenance_profile_id, @entry_order, @owner_node_id, @is_included_in_schedule, @to1_hours, @to2_hours, @to3_hours);
                    """,
                    ("@maintenance_profile_id", profile.MaintenanceProfileId),
                    ("@entry_order", i),
                    ("@owner_node_id", profile.OwnerNodeId),
                    ("@is_included_in_schedule", ToSqlBool(profile.IsIncludedInSchedule)),
                    ("@to1_hours", profile.To1Hours),
                    ("@to2_hours", profile.To2Hours),
                    ("@to3_hours", profile.To3Hours));

                InsertMaintenanceYearScheduleEntries(
                    connection,
                    transaction,
                    profile.MaintenanceProfileId,
                    profile.YearScheduleEntries);
            }
        }

        private static List<KbMaintenanceScheduleProfile> LoadMaintenanceScheduleProfiles(SqliteConnection connection)
        {
            var profiles = Query(
                connection,
                "SELECT * FROM maintenance_schedule_profiles ORDER BY entry_order;",
                static reader => new KbMaintenanceScheduleProfile
                {
                    MaintenanceProfileId = GetString(reader, "maintenance_profile_id"),
                    OwnerNodeId = GetString(reader, "owner_node_id"),
                    IsIncludedInSchedule = GetBool(reader, "is_included_in_schedule"),
                    To1Hours = GetInt(reader, "to1_hours"),
                    To2Hours = GetInt(reader, "to2_hours"),
                    To3Hours = GetInt(reader, "to3_hours")
                }).ToList();

            foreach (KbMaintenanceScheduleProfile profile in profiles)
                profile.YearScheduleEntries = LoadMaintenanceYearScheduleEntries(connection, profile.MaintenanceProfileId);

            return profiles;
        }

        private static void InsertMaintenanceYearScheduleEntries(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string profileId,
            IReadOnlyList<KbMaintenanceYearScheduleEntry> entries)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO maintenance_year_schedule_entries (maintenance_profile_id, entry_order, month, work_kind, hours)
                    VALUES (@maintenance_profile_id, @entry_order, @month, @work_kind, @hours);
                    """,
                    ("@maintenance_profile_id", profileId),
                    ("@entry_order", i),
                    ("@month", entries[i].Month),
                    ("@work_kind", (int)entries[i].WorkKind),
                    ("@hours", Math.Max(0, entries[i].Hours)));
            }
        }

        private static List<KbMaintenanceYearScheduleEntry> LoadMaintenanceYearScheduleEntries(
            SqliteConnection connection,
            string profileId) =>
            Query(
                connection,
                """
                SELECT month, work_kind, hours
                FROM maintenance_year_schedule_entries
                WHERE maintenance_profile_id = @maintenance_profile_id
                ORDER BY entry_order;
                """,
                static reader => new KbMaintenanceYearScheduleEntry
                {
                    Month = GetInt(reader, "month"),
                    WorkKind = ToEnum(GetInt(reader, "work_kind"), KbMaintenanceWorkKind.To1),
                    Hours = GetInt(reader, "hours")
                },
                ("@maintenance_profile_id", profileId))
                .ToList();

        private static void InsertEquipmentCatalogItems(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbEquipmentCatalogItem> items)
        {
            for (int i = 0; i < items.Count; i++)
            {
                KbEquipmentCatalogItem item = items[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO equipment_catalog_items (
                        catalog_item_id, entry_order, equipment_kind, manufacturer, series, model, default_node_type, description)
                    VALUES (
                        @catalog_item_id, @entry_order, @equipment_kind, @manufacturer, @series, @model, @default_node_type, @description);
                    """,
                    ("@catalog_item_id", item.CatalogItemId),
                    ("@entry_order", i),
                    ("@equipment_kind", item.EquipmentKind),
                    ("@manufacturer", item.Manufacturer),
                    ("@series", item.Series),
                    ("@model", item.Model),
                    ("@default_node_type", (int)item.DefaultNodeType),
                    ("@description", item.Description));

                for (int propertyIndex = 0; propertyIndex < item.Properties.Count; propertyIndex++)
                {
                    Execute(
                        connection,
                        transaction,
                        """
                        INSERT INTO equipment_catalog_properties (catalog_item_id, property_order, name, value)
                        VALUES (@catalog_item_id, @property_order, @name, @value);
                        """,
                        ("@catalog_item_id", item.CatalogItemId),
                        ("@property_order", propertyIndex),
                        ("@name", item.Properties[propertyIndex].Name),
                        ("@value", item.Properties[propertyIndex].Value));
                }
            }
        }

        private static List<KbEquipmentCatalogItem> LoadEquipmentCatalogItems(SqliteConnection connection)
        {
            var items = Query(
                connection,
                "SELECT * FROM equipment_catalog_items ORDER BY entry_order;",
                static reader => new KbEquipmentCatalogItem
                {
                    CatalogItemId = GetString(reader, "catalog_item_id"),
                    EquipmentKind = GetString(reader, "equipment_kind"),
                    Manufacturer = GetString(reader, "manufacturer"),
                    Series = GetString(reader, "series"),
                    Model = GetString(reader, "model"),
                    DefaultNodeType = ToEnum(GetInt(reader, "default_node_type"), KbNodeType.Device),
                    Description = GetString(reader, "description")
                }).ToList();

            foreach (KbEquipmentCatalogItem item in items)
            {
                item.Properties = Query(
                    connection,
                    """
                    SELECT name, value
                    FROM equipment_catalog_properties
                    WHERE catalog_item_id = @catalog_item_id
                    ORDER BY property_order;
                    """,
                    static reader => new KbEquipmentCatalogProperty
                    {
                        Name = GetString(reader, "name"),
                        Value = GetString(reader, "value")
                    },
                    ("@catalog_item_id", item.CatalogItemId))
                    .ToList();
            }

            return items;
        }

        private static void InsertObjectTemplates(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbObjectTemplate> templates)
        {
            for (int i = 0; i < templates.Count; i++)
            {
                KbObjectTemplate template = templates[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO object_templates (
                        template_id, entry_order, display_name, description, category, composition_racks_json, composition_entries_json,
                        document_links_json, software_records_json, maintenance_schedule_profiles_json)
                    VALUES (
                        @template_id, @entry_order, @display_name, @description, @category, @composition_racks_json, @composition_entries_json,
                        @document_links_json, @software_records_json, @maintenance_schedule_profiles_json);
                    """,
                    ("@template_id", template.TemplateId),
                    ("@entry_order", i),
                    ("@display_name", template.DisplayName),
                    ("@description", template.Description),
                    ("@category", template.Category),
                    ("@composition_racks_json", SerializeJson(template.CompositionRacks)),
                    ("@composition_entries_json", SerializeJson(template.CompositionEntries)),
                    ("@document_links_json", SerializeJson(template.DocumentLinks)),
                    ("@software_records_json", SerializeJson(template.SoftwareRecords)),
                    ("@maintenance_schedule_profiles_json", SerializeJson(template.MaintenanceScheduleProfiles)));

                InsertObjectTemplateNodes(
                    connection,
                    transaction,
                    template.TemplateId,
                    parentTemplateNodeId: null,
                    new[] { template.RootNode });
            }
        }

        private static List<KbObjectTemplate> LoadObjectTemplates(SqliteConnection connection)
        {
            var templates = Query(
                connection,
                "SELECT * FROM object_templates ORDER BY entry_order;",
                static reader => new KbObjectTemplate
                {
                    TemplateId = GetString(reader, "template_id"),
                    DisplayName = GetString(reader, "display_name"),
                    Description = GetString(reader, "description"),
                    Category = GetString(reader, "category"),
                    CompositionRacks = DeserializeJson<List<KbObjectTemplateCompositionRack>>(GetString(reader, "composition_racks_json")),
                    CompositionEntries = DeserializeJson<List<KbObjectTemplateCompositionEntry>>(GetString(reader, "composition_entries_json")),
                    DocumentLinks = DeserializeJson<List<KbObjectTemplateDocumentLink>>(GetString(reader, "document_links_json")),
                    SoftwareRecords = DeserializeJson<List<KbObjectTemplateSoftwareRecord>>(GetString(reader, "software_records_json")),
                    MaintenanceScheduleProfiles = DeserializeJson<List<KbObjectTemplateMaintenanceScheduleProfile>>(GetString(reader, "maintenance_schedule_profiles_json"))
                }).ToList();

            foreach (KbObjectTemplate template in templates)
                template.RootNode = LoadObjectTemplateRootNode(connection, template.TemplateId);

            return templates;
        }

        private static void InsertObjectTemplateNodes(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string templateId,
            string? parentTemplateNodeId,
            IReadOnlyList<KbObjectTemplateNode> nodes)
        {
            for (int i = 0; i < nodes.Count; i++)
            {
                KbObjectTemplateNode node = nodes[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO object_template_nodes (
                        template_id, template_node_id, parent_template_node_id, position_order, catalog_item_id,
                        name, node_type, details_description, details_location, details_inventory_number,
                        details_photo_path, details_ip_address, details_schema_link)
                    VALUES (
                        @template_id, @template_node_id, @parent_template_node_id, @position_order, @catalog_item_id,
                        @name, @node_type, @details_description, @details_location, @details_inventory_number,
                        @details_photo_path, @details_ip_address, @details_schema_link);
                    """,
                    ("@template_id", templateId),
                    ("@template_node_id", node.TemplateNodeId),
                    ("@parent_template_node_id", parentTemplateNodeId),
                    ("@position_order", i),
                    ("@catalog_item_id", node.CatalogItemId),
                    ("@name", node.Name),
                    ("@node_type", (int)node.NodeType),
                    ("@details_description", node.Details.Description),
                    ("@details_location", node.Details.Location),
                    ("@details_inventory_number", node.Details.InventoryNumber),
                    ("@details_photo_path", node.Details.PhotoPath),
                    ("@details_ip_address", node.Details.IpAddress),
                    ("@details_schema_link", node.Details.SchemaLink));

                InsertObjectTemplateNodes(connection, transaction, templateId, node.TemplateNodeId, node.Children);
            }
        }

        private static KbObjectTemplateNode LoadObjectTemplateRootNode(SqliteConnection connection, string templateId)
        {
            var rows = Query(
                connection,
                """
                SELECT *
                FROM object_template_nodes
                WHERE template_id = @template_id
                ORDER BY COALESCE(parent_template_node_id, ''), position_order;
                """,
                static reader => new TemplateNodeRow(
                    GetString(reader, "template_node_id"),
                    GetNullableString(reader, "parent_template_node_id"),
                    GetInt(reader, "position_order"),
                    new KbObjectTemplateNode
                    {
                        TemplateNodeId = GetString(reader, "template_node_id"),
                        CatalogItemId = GetString(reader, "catalog_item_id"),
                        Name = GetString(reader, "name"),
                        NodeType = ToEnum(GetInt(reader, "node_type"), KbNodeType.Device),
                        Details = new KbNodeDetails
                        {
                            Description = GetString(reader, "details_description"),
                            Location = GetString(reader, "details_location"),
                            InventoryNumber = GetString(reader, "details_inventory_number"),
                            PhotoPath = GetString(reader, "details_photo_path"),
                            IpAddress = GetString(reader, "details_ip_address"),
                            SchemaLink = GetString(reader, "details_schema_link")
                        }
                    }),
                ("@template_id", templateId))
                .OrderBy(static row => row.PositionOrder)
                .ToList();

            if (rows.Count == 0)
                return new KbObjectTemplateNode();

            var byId = rows.ToDictionary(static row => row.TemplateNodeId, static row => row.Node, StringComparer.Ordinal);
            KbObjectTemplateNode? root = null;
            foreach (TemplateNodeRow row in rows)
            {
                if (string.IsNullOrWhiteSpace(row.ParentTemplateNodeId))
                {
                    root ??= row.Node;
                    continue;
                }

                if (byId.TryGetValue(row.ParentTemplateNodeId, out KbObjectTemplateNode? parent))
                    parent.Children.Add(row.Node);
            }

            return root ?? rows[0].Node;
        }

        private static void InsertActs(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbAct> acts)
        {
            for (int i = 0; i < acts.Count; i++)
            {
                KbAct act = acts[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO acts (
                        act_id, entry_order, act_year, act_number, act_type, status, act_date,
                        workshop_name, lvl3_node_id, lvl3_name_snapshot, object_name_snapshot, object_path_snapshot, rack_id, rack_number_snapshot,
                        rack_name_snapshot, composition_entry_id, equipment_name, equipment_snapshot_json, failure_date,
                        fault_description, failure_reason, inspection_result, fault_criterion, request_document,
                        actual_labor_hours, customer_name, customer_position, approver_name, approver_position, created_by, created_at, updated_at)
                    VALUES (
                        @act_id, @entry_order, @act_year, @act_number, @act_type, @status, @act_date,
                        @workshop_name, @lvl3_node_id, @lvl3_name_snapshot, @object_name_snapshot, @object_path_snapshot, @rack_id, @rack_number_snapshot,
                        @rack_name_snapshot, @composition_entry_id, @equipment_name, @equipment_snapshot_json, @failure_date,
                        @fault_description, @failure_reason, @inspection_result, @fault_criterion, @request_document,
                        @actual_labor_hours, @customer_name, @customer_position, @approver_name, @approver_position, @created_by, @created_at, @updated_at);
                    """,
                    ("@act_id", act.ActId),
                    ("@entry_order", i),
                    ("@act_year", act.ActYear),
                    ("@act_number", act.ActNumber),
                    ("@act_type", (int)act.ActType),
                    ("@status", (int)act.Status),
                    ("@act_date", FormatDateTime(act.ActDate)),
                    ("@workshop_name", act.WorkshopName),
                    ("@lvl3_node_id", act.Lvl3NodeId),
                    ("@lvl3_name_snapshot", act.Lvl3NameSnapshot),
                    ("@object_name_snapshot", act.ObjectNameSnapshot),
                    ("@object_path_snapshot", act.ObjectPathSnapshot),
                    ("@rack_id", act.RackId),
                    ("@rack_number_snapshot", act.RackNumberSnapshot),
                    ("@rack_name_snapshot", act.RackNameSnapshot),
                    ("@composition_entry_id", act.CompositionEntryId),
                    ("@equipment_name", act.EquipmentName),
                    ("@equipment_snapshot_json", SerializeJson(act.EquipmentSnapshot)),
                    ("@failure_date", FormatDateTime(act.FailureDate)),
                    ("@fault_description", act.FaultDescription),
                    ("@failure_reason", act.FailureReason),
                    ("@inspection_result", act.InspectionResult),
                    ("@fault_criterion", act.FaultCriterion),
                    ("@request_document", act.RequestDocument),
                    ("@actual_labor_hours", act.ActualLaborHours),
                    ("@customer_name", act.CustomerName),
                    ("@customer_position", act.CustomerPosition),
                    ("@approver_name", act.ApproverName),
                    ("@approver_position", act.ApproverPosition),
                    ("@created_by", act.CreatedBy),
                    ("@created_at", FormatDateTime(act.CreatedAt)),
                    ("@updated_at", FormatDateTime(act.UpdatedAt)));
            }
        }

        private static List<KbAct> LoadActs(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM acts ORDER BY entry_order;",
                static reader => new KbAct
                {
                    ActId = GetString(reader, "act_id"),
                    ActYear = GetInt(reader, "act_year"),
                    ActNumber = GetString(reader, "act_number"),
                    ActType = ToEnum(GetInt(reader, "act_type"), KbActType.EquipmentFailure),
                    Status = ToEnum(GetInt(reader, "status"), KbActStatus.Draft),
                    ActDate = GetNullableDateTime(reader, "act_date"),
                    WorkshopName = GetString(reader, "workshop_name"),
                    Lvl3NodeId = GetString(reader, "lvl3_node_id"),
                    Lvl3NameSnapshot = GetString(reader, "lvl3_name_snapshot"),
                    ObjectNameSnapshot = GetString(reader, "object_name_snapshot"),
                    ObjectPathSnapshot = GetString(reader, "object_path_snapshot"),
                    RackId = GetString(reader, "rack_id"),
                    RackNumberSnapshot = GetNullableInt(reader, "rack_number_snapshot"),
                    RackNameSnapshot = GetString(reader, "rack_name_snapshot"),
                    CompositionEntryId = GetString(reader, "composition_entry_id"),
                    EquipmentName = GetString(reader, "equipment_name"),
                    EquipmentSnapshot = DeserializeJson<KbActEquipmentSnapshot>(GetString(reader, "equipment_snapshot_json")),
                    FailureDate = GetNullableDateTime(reader, "failure_date"),
                    FaultDescription = GetString(reader, "fault_description"),
                    FailureReason = GetString(reader, "failure_reason"),
                    InspectionResult = GetString(reader, "inspection_result"),
                    FaultCriterion = GetString(reader, "fault_criterion"),
                    RequestDocument = GetString(reader, "request_document"),
                    ActualLaborHours = GetString(reader, "actual_labor_hours"),
                    CustomerName = GetString(reader, "customer_name"),
                    CustomerPosition = GetString(reader, "customer_position"),
                    ApproverName = GetString(reader, "approver_name"),
                    ApproverPosition = GetString(reader, "approver_position"),
                    CreatedBy = GetString(reader, "created_by"),
                    CreatedAt = GetNullableDateTime(reader, "created_at"),
                    UpdatedAt = GetNullableDateTime(reader, "updated_at")
                }).ToList();

        private static void InsertActExecutors(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbActExecutor> executors)
        {
            for (int i = 0; i < executors.Count; i++)
            {
                KbActExecutor executor = executors[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO act_executors (
                        executor_id, entry_order, act_id, sort_order, last_name, first_name, middle_name, position)
                    VALUES (
                        @executor_id, @entry_order, @act_id, @sort_order, @last_name, @first_name, @middle_name, @position);
                    """,
                    ("@executor_id", executor.ExecutorId),
                    ("@entry_order", i),
                    ("@act_id", executor.ActId),
                    ("@sort_order", executor.SortOrder),
                    ("@last_name", executor.LastName),
                    ("@first_name", executor.FirstName),
                    ("@middle_name", executor.MiddleName),
                    ("@position", executor.Position));
            }
        }

        private static List<KbActExecutor> LoadActExecutors(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM act_executors ORDER BY entry_order;",
                static reader => new KbActExecutor
                {
                    ExecutorId = GetString(reader, "executor_id"),
                    ActId = GetString(reader, "act_id"),
                    SortOrder = GetInt(reader, "sort_order"),
                    LastName = GetString(reader, "last_name"),
                    FirstName = GetString(reader, "first_name"),
                    MiddleName = GetString(reader, "middle_name"),
                    Position = GetString(reader, "position")
                }).ToList();

        private static void InsertActDocuments(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbActDocument> documents)
        {
            for (int i = 0; i < documents.Count; i++)
            {
                KbActDocument document = documents[i];
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO act_documents (
                        document_id, entry_order, act_id, version_number, template_id, template_version,
                        path, generated_at, content_hash, is_latest)
                    VALUES (
                        @document_id, @entry_order, @act_id, @version_number, @template_id, @template_version,
                        @path, @generated_at, @content_hash, @is_latest);
                    """,
                    ("@document_id", document.DocumentId),
                    ("@entry_order", i),
                    ("@act_id", document.ActId),
                    ("@version_number", document.VersionNumber),
                    ("@template_id", document.TemplateId),
                    ("@template_version", document.TemplateVersion),
                    ("@path", document.Path),
                    ("@generated_at", FormatDateTime(document.GeneratedAt)),
                    ("@content_hash", document.ContentHash),
                    ("@is_latest", ToSqlBool(document.IsLatest)));
            }
        }

        private static List<KbActDocument> LoadActDocuments(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT * FROM act_documents ORDER BY entry_order;",
                static reader => new KbActDocument
                {
                    DocumentId = GetString(reader, "document_id"),
                    ActId = GetString(reader, "act_id"),
                    VersionNumber = GetInt(reader, "version_number"),
                    TemplateId = GetString(reader, "template_id"),
                    TemplateVersion = GetString(reader, "template_version"),
                    Path = GetString(reader, "path"),
                    GeneratedAt = GetNullableDateTime(reader, "generated_at"),
                    ContentHash = GetString(reader, "content_hash"),
                    IsLatest = GetBool(reader, "is_latest")
                }).ToList();

        private static void InsertActNumberSequences(
            SqliteConnection connection,
            SqliteTransaction transaction,
            IReadOnlyList<KbActNumberSequence> sequences)
        {
            foreach (KbActNumberSequence sequence in sequences)
            {
                Execute(
                    connection,
                    transaction,
                    """
                    INSERT INTO act_number_sequences (year, next_number)
                    VALUES (@year, @next_number);
                    """,
                    ("@year", sequence.Year),
                    ("@next_number", sequence.NextNumber));
            }
        }

        private static List<KbActNumberSequence> LoadActNumberSequences(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT year, next_number FROM act_number_sequences ORDER BY year;",
                static reader => new KbActNumberSequence
                {
                    Year = GetInt(reader, "year"),
                    NextNumber = GetInt(reader, "next_number")
                }).ToList();

        private static void UpsertMetadata(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string key,
            string value) =>
            Execute(
                connection,
                transaction,
                """
                INSERT INTO app_metadata (key, value)
                VALUES (@key, @value)
                ON CONFLICT(key) DO UPDATE SET value = excluded.value;
                """,
                ("@key", key),
                ("@value", value));

        private static Dictionary<string, string> LoadMetadata(SqliteConnection connection) =>
            Query(
                connection,
                "SELECT key, value FROM app_metadata;",
                static reader => new KeyValuePair<string, string>(
                    GetString(reader, "key"),
                    GetString(reader, "value")))
                .ToDictionary(static pair => pair.Key, static pair => pair.Value, StringComparer.Ordinal);

        private KnowledgeBaseSnapshotCreateResult InsertSnapshot(
            SqliteConnection connection,
            SqliteTransaction transaction,
            SavedData data,
            string kind,
            string note)
        {
            DateTimeOffset createdAt = _clock();
            string snapshotId = BuildSnapshotId(createdAt, kind);
            string payloadJson = JsonSerializer.Serialize(
                KnowledgeBaseDataService.NormalizeSavedData(data),
                SnapshotJsonOptions);
            long sizeBytes = Encoding.UTF8.GetByteCount(payloadJson);

            Execute(
                connection,
                transaction,
                """
                INSERT INTO snapshots (
                    snapshot_id, created_at, kind, source_database_path, size_bytes, note, payload_json)
                VALUES (
                    @snapshot_id, @created_at, @kind, @source_database_path, @size_bytes, @note, @payload_json);
                """,
                ("@snapshot_id", snapshotId),
                ("@created_at", createdAt.ToString("O", CultureInfo.InvariantCulture)),
                ("@kind", kind),
                ("@source_database_path", Path.GetFullPath(SavePath)),
                ("@size_bytes", sizeBytes),
                ("@note", note),
                ("@payload_json", payloadJson));

            return new KnowledgeBaseSnapshotCreateResult
            {
                IsSuccess = true,
                SnapshotPath = BuildSnapshotReference(SavePath, snapshotId),
                CreatedAt = createdAt,
                SizeBytes = sizeBytes
            };
        }

        private void InsertChangeLog(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string actionKind,
            string summary,
            string details)
        {
            DateTimeOffset createdAt = _clock();
            string normalizedActionKind = NormalizeIdentifierPart(actionKind, "action");
            string changeId = BuildChangeLogId(createdAt, normalizedActionKind);

            Execute(
                connection,
                transaction,
                """
                INSERT INTO change_log (change_id, created_at, action_kind, summary, details)
                VALUES (@change_id, @created_at, @action_kind, @summary, @details);
                """,
                ("@change_id", changeId),
                ("@created_at", createdAt.ToString("O", CultureInfo.InvariantCulture)),
                ("@action_kind", normalizedActionKind),
                ("@summary", summary?.Trim() ?? string.Empty),
                ("@details", details?.Trim() ?? string.Empty));
        }

        private static SavedData LoadSnapshotData(SqliteConnection connection, string snapshotId)
        {
            string payloadJson = Query(
                connection,
                """
                SELECT payload_json
                FROM snapshots
                WHERE snapshot_id = @snapshot_id;
                """,
                static reader => GetString(reader, "payload_json"),
                ("@snapshot_id", snapshotId))
                .SingleOrDefault() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(payloadJson))
                throw new InvalidOperationException("Снимок базы не найден.");

            SavedData data = JsonSerializer.Deserialize<SavedData>(payloadJson, SnapshotJsonOptions) ??
                throw new InvalidOperationException("Снимок базы не содержит корректные данные.");

            return KnowledgeBaseDataService.NormalizeSavedData(data);
        }

        private static string BuildSnapshotId(DateTimeOffset createdAt, string kind)
        {
            string timestamp = createdAt
                .ToUniversalTime()
                .ToString("yyyyMMdd-HHmmss-fff'Z'", CultureInfo.InvariantCulture);
            string normalizedKind = NormalizeIdentifierPart(kind, "snapshot");
            return $"{timestamp}.{normalizedKind}.{Guid.NewGuid():N}";
        }

        private static string BuildChangeLogId(DateTimeOffset createdAt, string actionKind)
        {
            string timestamp = createdAt
                .ToUniversalTime()
                .ToString("yyyyMMdd-HHmmss-fff'Z'", CultureInfo.InvariantCulture);
            return $"{timestamp}.{actionKind}.{Guid.NewGuid():N}";
        }

        private static string BuildSnapshotReference(string savePath, string snapshotId) =>
            $"{Path.GetFullPath(savePath)}#snapshot:{snapshotId}";

        private static string ResolveSnapshotId(KnowledgeBaseSnapshotEntry? snapshot)
        {
            string snapshotId = snapshot?.SnapshotId?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(snapshotId))
                return snapshotId;

            string snapshotPath = snapshot?.SnapshotPath?.Trim() ?? string.Empty;
            const string marker = "#snapshot:";
            int markerIndex = snapshotPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            return markerIndex < 0
                ? string.Empty
                : snapshotPath[(markerIndex + marker.Length)..].Trim();
        }

        private static string NormalizeIdentifierPart(string? value, string fallback)
        {
            string normalized = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalized))
                normalized = fallback;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                normalized = normalized.Replace(invalidChar, '-');

            normalized = normalized.Replace(' ', '-');
            return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
        }

        private static void Execute(
            SqliteConnection connection,
            SqliteTransaction transaction,
            string commandText,
            params (string Name, object? Value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            AddParameters(command, parameters);
            command.ExecuteNonQuery();
        }

        private static void ExecuteNonQuery(
            SqliteConnection connection,
            SqliteTransaction? transaction,
            string commandText)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = commandText;
            command.ExecuteNonQuery();
        }

        private static List<T> Query<T>(
            SqliteConnection connection,
            string commandText,
            Func<SqliteDataReader, T> map,
            params (string Name, object? Value)[] parameters)
        {
            using var command = connection.CreateCommand();
            command.CommandText = commandText;
            AddParameters(command, parameters);

            using var reader = command.ExecuteReader();
            var results = new List<T>();
            while (reader.Read())
                results.Add(map(reader));

            return results;
        }

        private static void AddParameters(
            SqliteCommand command,
            params (string Name, object? Value)[] parameters)
        {
            foreach (var (name, value) in parameters)
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        private static long GetLastInsertRowId(SqliteConnection connection, SqliteTransaction transaction)
        {
            using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = "SELECT last_insert_rowid();";
            return Convert.ToInt64(command.ExecuteScalar(), CultureInfo.InvariantCulture);
        }

        private static string GetString(SqliteDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
        }

        private static string? GetNullableString(SqliteDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        private static int GetInt(SqliteDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
        }

        private static long GetInt64(SqliteDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? 0 : reader.GetInt64(ordinal);
        }

        private static int? GetNullableInt(SqliteDataReader reader, string name)
        {
            int ordinal = reader.GetOrdinal(name);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }

        private static bool GetBool(SqliteDataReader reader, string name) =>
            GetInt(reader, name) != 0;

        private static DateTime? GetNullableDateTime(SqliteDataReader reader, string name)
        {
            string? value = GetNullableString(reader, name);
            return string.IsNullOrWhiteSpace(value)
                ? null
                : DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
        }

        private static TEnum ToEnum<TEnum>(int value, TEnum fallback)
            where TEnum : struct, Enum =>
            Enum.IsDefined(typeof(TEnum), value) ? (TEnum)Enum.ToObject(typeof(TEnum), value) : fallback;

        private static string? FormatDateTime(DateTime? value) =>
            value?.ToString("O", CultureInfo.InvariantCulture);

        private static string FormatDateOnly(DateOnly value) =>
            value.ToString("O", CultureInfo.InvariantCulture);

        private static DateOnly ParseDateOnly(string value) =>
            DateOnly.ParseExact(value, "O", CultureInfo.InvariantCulture);

        private static int ToSqlBool(bool value) => value ? 1 : 0;

        private static int ParseInt(string? value, int fallback) =>
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : fallback;

        private static string SerializeJson<T>(T value) =>
            JsonSerializer.Serialize(value, JsonOptions);

        private static T DeserializeJson<T>(string json)
            where T : new() =>
            string.IsNullOrWhiteSpace(json)
                ? new T()
                : JsonSerializer.Deserialize<T>(json, JsonOptions) ?? new T();

        private void LogStartupTiming(string stage, long elapsedMs) =>
            _logger.Log(
                "StartupTiming",
                AppLogLevel.Information,
                "SQLite storage startup timing checkpoint.",
                properties: CreateProperties(
                    ("stage", stage),
                    ("elapsedMs", elapsedMs),
                    ("path", SavePath)));

        private Dictionary<string, object?> CreateProperties(params (string Key, object? Value)[] values)
        {
            var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var (key, value) in values)
            {
                if (string.IsNullOrWhiteSpace(key) || value == null)
                    continue;

                properties[key] = value;
            }

            return properties;
        }

        private sealed record NodeRow(
            string NodeId,
            string? ParentNodeId,
            int PositionOrder,
            KbNode Node);

        private sealed record TemplateNodeRow(
            string TemplateNodeId,
            string? ParentTemplateNodeId,
            int PositionOrder,
            KbObjectTemplateNode Node);

        private static readonly string[] DataTablesInDeleteOrder =
        [
            "act_documents",
            "act_executors",
            "acts",
            "act_number_sequences",
            "object_template_nodes",
            "object_templates",
            "equipment_catalog_properties",
            "equipment_catalog_items",
            "maintenance_year_schedule_entries",
            "maintenance_schedule_profiles",
            "software_records",
            "document_links",
            "composition_entries",
            "composition_racks",
            "nodes",
            "workshops",
            "production_calendar_dates",
            "production_calendar_years",
            "config_level_names",
            "config"
        ];

        private static readonly string[] SchemaStatements =
        [
            """
            CREATE TABLE IF NOT EXISTS app_metadata (
                key TEXT PRIMARY KEY,
                value TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS config (
                id INTEGER PRIMARY KEY CHECK (id = 1),
                max_levels INTEGER NOT NULL,
                act_documents_directory_path TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS config_level_names (
                position_order INTEGER PRIMARY KEY,
                level_name TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS production_calendar_years (
                year INTEGER PRIMARY KEY
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS production_calendar_dates (
                year INTEGER NOT NULL,
                date_kind TEXT NOT NULL,
                date_value TEXT NOT NULL,
                position_order INTEGER NOT NULL,
                PRIMARY KEY (year, date_kind, date_value),
                FOREIGN KEY (year) REFERENCES production_calendar_years(year) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS workshops (
                workshop_id INTEGER PRIMARY KEY AUTOINCREMENT,
                workshop_name TEXT NOT NULL UNIQUE COLLATE NOCASE,
                position_order INTEGER NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS nodes (
                node_id TEXT PRIMARY KEY,
                workshop_id INTEGER NOT NULL,
                parent_node_id TEXT NULL,
                position_order INTEGER NOT NULL,
                name TEXT NOT NULL,
                level_index INTEGER NOT NULL,
                node_type INTEGER NOT NULL,
                details_description TEXT NOT NULL,
                details_location TEXT NOT NULL,
                details_inventory_number TEXT NOT NULL,
                details_photo_path TEXT NOT NULL,
                details_ip_address TEXT NOT NULL,
                details_schema_link TEXT NOT NULL,
                details_network_topology_json TEXT NOT NULL DEFAULT '{}',
                FOREIGN KEY (workshop_id) REFERENCES workshops(workshop_id) ON DELETE CASCADE,
                FOREIGN KEY (parent_node_id) REFERENCES nodes(node_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS composition_racks (
                rack_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                parent_node_id TEXT NOT NULL,
                rack_number INTEGER NOT NULL,
                sort_order INTEGER NOT NULL,
                rack_type TEXT NOT NULL,
                label TEXT NOT NULL,
                notes TEXT NOT NULL,
                properties_json TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS composition_entries (
                entry_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                parent_node_id TEXT NOT NULL,
                rack_number INTEGER NOT NULL DEFAULT 0,
                slot_number INTEGER NULL,
                position_order INTEGER NOT NULL,
                component_type TEXT NOT NULL,
                model TEXT NOT NULL,
                order_number TEXT NOT NULL,
                firmware TEXT NOT NULL,
                mpi_dp_pn_address TEXT NOT NULL,
                input_address TEXT NOT NULL,
                output_address TEXT NOT NULL,
                comment_text TEXT NOT NULL,
                interface_rows TEXT NOT NULL,
                ip_address TEXT NOT NULL,
                last_calibration_at TEXT NULL,
                next_calibration_at TEXT NULL,
                notes TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS document_links (
                document_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                owner_node_id TEXT NOT NULL,
                kind INTEGER NOT NULL,
                title TEXT NOT NULL,
                path TEXT NOT NULL,
                updated_at TEXT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS software_records (
                software_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                owner_node_id TEXT NOT NULL,
                title TEXT NOT NULL,
                path TEXT NOT NULL,
                added_at TEXT NULL,
                last_changed_at TEXT NULL,
                last_backup_at TEXT NULL,
                notes TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS maintenance_schedule_profiles (
                maintenance_profile_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                owner_node_id TEXT NOT NULL,
                is_included_in_schedule INTEGER NOT NULL,
                to1_hours INTEGER NOT NULL,
                to2_hours INTEGER NOT NULL,
                to3_hours INTEGER NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS maintenance_year_schedule_entries (
                maintenance_profile_id TEXT NOT NULL,
                entry_order INTEGER NOT NULL,
                month INTEGER NOT NULL,
                work_kind INTEGER NOT NULL,
                hours INTEGER NOT NULL DEFAULT 0,
                PRIMARY KEY (maintenance_profile_id, entry_order),
                FOREIGN KEY (maintenance_profile_id) REFERENCES maintenance_schedule_profiles(maintenance_profile_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS equipment_catalog_items (
                catalog_item_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                equipment_kind TEXT NOT NULL,
                manufacturer TEXT NOT NULL,
                series TEXT NOT NULL,
                model TEXT NOT NULL,
                default_node_type INTEGER NOT NULL,
                description TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS equipment_catalog_properties (
                catalog_item_id TEXT NOT NULL,
                property_order INTEGER NOT NULL,
                name TEXT NOT NULL,
                value TEXT NOT NULL,
                PRIMARY KEY (catalog_item_id, property_order),
                FOREIGN KEY (catalog_item_id) REFERENCES equipment_catalog_items(catalog_item_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS snapshots (
                snapshot_id TEXT PRIMARY KEY,
                created_at TEXT NOT NULL,
                kind TEXT NOT NULL,
                source_database_path TEXT NOT NULL,
                size_bytes INTEGER NOT NULL,
                note TEXT NOT NULL,
                payload_json TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS change_log (
                change_id TEXT PRIMARY KEY,
                created_at TEXT NOT NULL,
                action_kind TEXT NOT NULL,
                summary TEXT NOT NULL,
                details TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS object_templates (
                template_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                display_name TEXT NOT NULL,
                description TEXT NOT NULL,
                category TEXT NOT NULL,
                composition_racks_json TEXT NOT NULL DEFAULT '[]',
                composition_entries_json TEXT NOT NULL,
                document_links_json TEXT NOT NULL,
                software_records_json TEXT NOT NULL,
                maintenance_schedule_profiles_json TEXT NOT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS object_template_nodes (
                template_id TEXT NOT NULL,
                template_node_id TEXT NOT NULL,
                parent_template_node_id TEXT NULL,
                position_order INTEGER NOT NULL,
                catalog_item_id TEXT NOT NULL,
                name TEXT NOT NULL,
                node_type INTEGER NOT NULL,
                details_description TEXT NOT NULL,
                details_location TEXT NOT NULL,
                details_inventory_number TEXT NOT NULL,
                details_photo_path TEXT NOT NULL,
                details_ip_address TEXT NOT NULL,
                details_schema_link TEXT NOT NULL,
                PRIMARY KEY (template_id, template_node_id),
                FOREIGN KEY (template_id) REFERENCES object_templates(template_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS acts (
                act_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                act_year INTEGER NOT NULL,
                act_number TEXT NOT NULL,
                act_type INTEGER NOT NULL,
                status INTEGER NOT NULL,
                act_date TEXT NULL,
                workshop_name TEXT NOT NULL,
                lvl3_node_id TEXT NOT NULL,
                lvl3_name_snapshot TEXT NOT NULL,
                object_name_snapshot TEXT NOT NULL,
                object_path_snapshot TEXT NOT NULL,
                rack_id TEXT NOT NULL,
                rack_number_snapshot INTEGER NULL,
                rack_name_snapshot TEXT NOT NULL,
                composition_entry_id TEXT NOT NULL,
                equipment_name TEXT NOT NULL,
                equipment_snapshot_json TEXT NOT NULL,
                failure_date TEXT NULL,
                fault_description TEXT NOT NULL,
                failure_reason TEXT NOT NULL,
                inspection_result TEXT NOT NULL,
                fault_criterion TEXT NOT NULL,
                request_document TEXT NOT NULL,
                actual_labor_hours TEXT NOT NULL,
                customer_name TEXT NOT NULL,
                customer_position TEXT NOT NULL,
                approver_name TEXT NOT NULL,
                approver_position TEXT NOT NULL,
                created_by TEXT NOT NULL,
                created_at TEXT NULL,
                updated_at TEXT NULL
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS act_executors (
                executor_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                act_id TEXT NOT NULL,
                sort_order INTEGER NOT NULL,
                last_name TEXT NOT NULL,
                first_name TEXT NOT NULL,
                middle_name TEXT NOT NULL,
                position TEXT NOT NULL,
                FOREIGN KEY (act_id) REFERENCES acts(act_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS act_documents (
                document_id TEXT PRIMARY KEY,
                entry_order INTEGER NOT NULL,
                act_id TEXT NOT NULL,
                version_number INTEGER NOT NULL,
                template_id TEXT NOT NULL,
                template_version TEXT NOT NULL,
                path TEXT NOT NULL,
                generated_at TEXT NULL,
                content_hash TEXT NOT NULL,
                is_latest INTEGER NOT NULL,
                FOREIGN KEY (act_id) REFERENCES acts(act_id) ON DELETE CASCADE
            );
            """,
            """
            CREATE TABLE IF NOT EXISTS act_number_sequences (
                year INTEGER PRIMARY KEY,
                next_number INTEGER NOT NULL
            );
            """,
            """
            CREATE INDEX IF NOT EXISTS idx_acts_year_number
            ON acts(act_year, act_number);
            """,
            """
            CREATE INDEX IF NOT EXISTS idx_acts_lvl3_node_id
            ON acts(lvl3_node_id);
            """,
            """
            CREATE INDEX IF NOT EXISTS idx_acts_composition_entry_id
            ON acts(composition_entry_id);
            """,
            """
            CREATE INDEX IF NOT EXISTS idx_act_executors_act_id
            ON act_executors(act_id);
            """,
            """
            CREATE INDEX IF NOT EXISTS idx_act_documents_act_id
            ON act_documents(act_id);
            """
        ];
    }
}
