using System.Text.Json;
using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;
using Microsoft.Data.Sqlite;

namespace AsutpKnowledgeBase.Core.Tests;

public class SqliteKnowledgeBaseStorageServiceTests
{
    [Fact]
    public void EnsureSchema_CreatesSchemaVersionOneAndCoreTables()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(path);

            service.EnsureSchema();

            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();

            Assert.Equal(SqliteKnowledgeBaseStorageService.CurrentDatabaseSchemaVersion, ReadUserVersion(connection));

            var tables = ReadTableNames(connection);
            Assert.Contains("app_metadata", tables);
            Assert.Contains("config", tables);
            Assert.Contains("workshops", tables);
            Assert.Contains("nodes", tables);
            Assert.Contains("composition_entries", tables);
            Assert.Contains("document_links", tables);
            Assert.Contains("software_records", tables);
            Assert.Contains("network_file_references", tables);
            Assert.Contains("network_devices", tables);
            Assert.Contains("network_interfaces", tables);
            Assert.Contains("network_connections", tables);
            Assert.Contains("maintenance_schedule_profiles", tables);
            Assert.Contains("maintenance_year_schedule_entries", tables);
            Assert.Contains("equipment_catalog_items", tables);
            Assert.Contains("equipment_catalog_properties", tables);
            Assert.Contains("snapshots", tables);
            Assert.Contains("change_log", tables);
            Assert.Contains("object_templates", tables);
            Assert.Contains("object_template_nodes", tables);

            var compositionColumns = ReadColumnNames(connection, "composition_entries");
            Assert.Contains("order_number", compositionColumns);
            Assert.Contains("firmware", compositionColumns);
            Assert.Contains("mpi_dp_pn_address", compositionColumns);
            Assert.Contains("input_address", compositionColumns);
            Assert.Contains("output_address", compositionColumns);
            Assert.Contains("comment_text", compositionColumns);
            Assert.Contains("interface_rows", compositionColumns);

            var connectionColumns = ReadColumnNames(connection, "network_connections");
            Assert.Contains("protocol", connectionColumns);
            Assert.Contains("medium", connectionColumns);
            Assert.Contains("route_text", connectionColumns);

            var fileReferenceColumns = ReadColumnNames(connection, "network_file_references");
            Assert.Contains("source_note", fileReferenceColumns);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void SaveAndLoad_RoundTripsNormalizedSavedData()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(path);
            SavedData expected = KnowledgeBaseDataService.NormalizeSavedData(CreateFullSampleData());

            Assert.True(service.Save(expected, out string? errorMessage), errorMessage);

            KnowledgeBaseStorageLoadResult loadResult = service.Load();

            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.NotNull(loadResult.Data);
            Assert.Equal(Serialize(expected), Serialize(loadResult.Data));

            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            Assert.Equal(2, CountRows(connection, "workshops"));
            Assert.Equal(3, CountRows(connection, "nodes"));
            Assert.Equal(1, CountRows(connection, "composition_entries"));
            Assert.Equal(1, CountRows(connection, "document_links"));
            Assert.Equal(1, CountRows(connection, "software_records"));
            Assert.Equal(1, CountRows(connection, "network_file_references"));
            Assert.Equal(2, CountRows(connection, "network_devices"));
            Assert.Equal(2, CountRows(connection, "network_interfaces"));
            Assert.Equal(1, CountRows(connection, "network_connections"));
            Assert.Equal(1, CountRows(connection, "maintenance_schedule_profiles"));
            Assert.Equal(2, CountRows(connection, "maintenance_year_schedule_entries"));
            Assert.Equal(1, CountRows(connection, "equipment_catalog_items"));
            Assert.Equal(2, CountRows(connection, "equipment_catalog_properties"));
            Assert.Equal(1, CountRows(connection, "object_templates"));
            Assert.Equal(2, CountRows(connection, "object_template_nodes"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenCompositionEntriesHaveLegacySchema_AddsRackNumberColumnAsRack0()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "legacy.akb");
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE composition_entries (
                        entry_id TEXT PRIMARY KEY,
                        entry_order INTEGER NOT NULL,
                        parent_node_id TEXT NOT NULL,
                        slot_number INTEGER NULL,
                        position_order INTEGER NOT NULL,
                        component_type TEXT NOT NULL,
                        model TEXT NOT NULL,
                        ip_address TEXT NOT NULL,
                        last_calibration_at TEXT NULL,
                        next_calibration_at TEXT NULL,
                        notes TEXT NOT NULL
                    );
                    INSERT INTO composition_entries (
                        entry_id, entry_order, parent_node_id, slot_number, position_order, component_type, model,
                        ip_address, last_calibration_at, next_calibration_at, notes)
                    VALUES ('legacy-composition-1', 0, 'cabinet-1', 2, 0, 'CPU', 'S7-300', '', NULL, NULL, '');
                    """;
                command.ExecuteNonQuery();
            }

            var service = new SqliteKnowledgeBaseStorageService(path);
            KnowledgeBaseStorageLoadResult loadResult = service.Load();

            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            var entry = Assert.Single(loadResult.Data!.CompositionEntries);
            Assert.Equal(0, entry.RackNumber);
            Assert.Equal(2, entry.SlotNumber);
            Assert.Equal(string.Empty, entry.OrderNumber);
            Assert.Equal(string.Empty, entry.Firmware);
            Assert.Equal(string.Empty, entry.MpiDpPnAddress);
            Assert.Equal(string.Empty, entry.InputAddress);
            Assert.Equal(string.Empty, entry.OutputAddress);
            Assert.Equal(string.Empty, entry.Comment);
            Assert.Equal(string.Empty, entry.InterfaceRows);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenNetworkFileReferencesHaveLegacySchema_AddsSourceNoteColumn()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "legacy-network.akb");
            using (var connection = new SqliteConnection($"Data Source={path};Pooling=False"))
            {
                connection.Open();
                using var command = connection.CreateCommand();
                command.CommandText =
                    """
                    CREATE TABLE network_file_references (
                        network_asset_id TEXT PRIMARY KEY,
                        entry_order INTEGER NOT NULL,
                        owner_node_id TEXT NOT NULL,
                        title TEXT NOT NULL,
                        path TEXT NOT NULL,
                        preview_kind INTEGER NOT NULL
                    );
                    INSERT INTO network_file_references (
                        network_asset_id, entry_order, owner_node_id, title, path, preview_kind)
                    VALUES ('network-file-1', 0, 'system-1', 'Topology', 'C:\network\topology.pdf', 2);
                    """;
                command.ExecuteNonQuery();
            }

            var service = new SqliteKnowledgeBaseStorageService(path);
            KnowledgeBaseStorageLoadResult loadResult = service.Load();

            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            var reference = Assert.Single(loadResult.Data!.NetworkFileReferences);
            Assert.Equal("Topology", reference.Title);
            Assert.Equal(string.Empty, reference.SourceNote);

            using var verifyConnection = new SqliteConnection($"Data Source={path};Pooling=False");
            verifyConnection.Open();
            Assert.Contains("source_note", ReadColumnNames(verifyConnection, "network_file_references"));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_WhenDatabaseMissing_ReturnsFileMissing()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "missing.akb");
            var service = new SqliteKnowledgeBaseStorageService(path);

            KnowledgeBaseStorageLoadResult result = service.Load();

            Assert.True(result.FileMissing);
            Assert.False(result.IsSuccess);
            Assert.Equal(path, result.SourcePath);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void CreateManualSnapshot_WritesSnapshotMetadataAndPayloadIntoSqlite()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(
                path,
                clock: () => new DateTimeOffset(2026, 5, 7, 10, 0, 0, TimeSpan.Zero));
            SavedData data = CreateFullSampleData();
            Assert.True(service.Save(data, out string? saveError), saveError);

            KnowledgeBaseSnapshotCreateResult result =
                service.CreateManualSnapshot(data, "Перед импортом JSON");

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Contains("#snapshot:", result.SnapshotPath);
            Assert.True(result.SizeBytes > 0);

            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            Assert.Equal(1, CountRows(connection, "snapshots"));
            SnapshotRow row = ReadSnapshotRows(connection).Single();
            Assert.Equal("manual", row.Kind);
            Assert.Equal("Перед импортом JSON", row.Note);
            Assert.Contains("\"LastWorkshop\"", row.PayloadJson);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenDatabaseAlreadyContainsData_CreatesProtectiveSnapshot()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(
                path,
                clock: () => new DateTimeOffset(2026, 5, 7, 10, 30, 0, TimeSpan.Zero));
            SavedData first = CreateFullSampleData();
            Assert.True(service.Save(first, out string? firstError), firstError);

            SavedData second = CreateFullSampleData();
            second.LastWorkshop = "Цех Б";
            Assert.True(service.Save(second, out string? secondError), secondError);

            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            SnapshotRow row = ReadSnapshotRows(connection).Single();
            Assert.Equal("before-save", row.Kind);
            Assert.Equal("Автоматический снимок перед сохранением.", row.Note);
            Assert.Contains("\"LastWorkshop\":\"Цех А\"", row.PayloadJson);

            KnowledgeBaseSnapshotListResult listResult = service.ListSnapshots();
            Assert.True(listResult.IsSuccess, listResult.ErrorMessage);
            KnowledgeBaseSnapshotEntry entry = listResult.Snapshots.Single();
            Assert.Equal("before-save", entry.Kind);
            Assert.Equal(row.SnapshotId, entry.SnapshotId);
            Assert.Equal(path, entry.SourcePath);
            Assert.True(entry.HasMetadata);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Save_WhenDatabaseAlreadyExists_CreatesExternalAkbBackup()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(
                path,
                clock: () => new DateTimeOffset(2026, 5, 12, 14, 30, 15, TimeSpan.Zero));
            SavedData first = CreateFullSampleData();
            Assert.True(service.Save(first, out string? firstError), firstError);

            SavedData second = CreateFullSampleData();
            second.LastWorkshop = "Цех Б";
            Assert.True(service.Save(second, out string? secondError), secondError);

            string backupPath = Path.Combine(
                tempDirectory,
                KnowledgeBaseExternalBackupService.BackupDirectoryName,
                "2026-05-12",
                "knowledge-base-20260512-143015.akb");
            Assert.True(File.Exists(backupPath));

            var backupStorage = new SqliteKnowledgeBaseStorageService(backupPath);
            KnowledgeBaseStorageLoadResult backupLoadResult = backupStorage.Load();
            Assert.True(backupLoadResult.IsSuccess, backupLoadResult.ErrorMessage);
            Assert.Equal("Цех А", backupLoadResult.Data!.LastWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ListSnapshots_WhenDatabaseMissing_ReturnsEmptyListWithoutCreatingDatabase()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "missing.akb");
            var service = new SqliteKnowledgeBaseStorageService(path);

            KnowledgeBaseSnapshotListResult result = service.ListSnapshots();

            Assert.True(result.IsSuccess, result.ErrorMessage);
            Assert.Empty(result.Snapshots);
            Assert.False(File.Exists(path));
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RestoreSnapshot_RestoresPayloadAndCreatesProtectiveSnapshot()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(
                path,
                clock: () => new DateTimeOffset(2026, 5, 7, 11, 0, 0, TimeSpan.Zero));
            SavedData original = CreateFullSampleData();
            Assert.True(service.Save(original, out string? firstError), firstError);
            KnowledgeBaseSnapshotCreateResult manualSnapshot =
                service.CreateManualSnapshot(original, "Перед ошибочной правкой");
            Assert.True(manualSnapshot.IsSuccess, manualSnapshot.ErrorMessage);

            SavedData changed = CreateFullSampleData();
            changed.LastWorkshop = "Цех Б";
            Assert.True(service.Save(changed, out string? secondError), secondError);

            KnowledgeBaseSnapshotEntry snapshot = service.ListSnapshots()
                .Snapshots
                .Single(entry => entry.Kind == "manual");

            KnowledgeBaseSnapshotRestoreResult restoreResult = service.RestoreSnapshot(snapshot);

            Assert.True(restoreResult.IsSuccess, restoreResult.ErrorMessage);
            Assert.Contains("#snapshot:", restoreResult.ProtectiveSnapshotPath);

            KnowledgeBaseStorageLoadResult loadResult = service.Load();
            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.Equal("Цех А", loadResult.Data!.LastWorkshop);

            using var connection = new SqliteConnection($"Data Source={path};Pooling=False");
            connection.Open();
            List<SnapshotRow> snapshots = ReadSnapshotRows(connection);
            SnapshotRow beforeRestore = snapshots.Single(row => row.Kind == "before-restore");
            Assert.Contains("\"LastWorkshop\":\"Цех Б\"", beforeRestore.PayloadJson);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void RestoreSnapshot_WhenSnapshotIsMissing_LeavesCurrentDatabaseIntact()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(path);
            SavedData current = CreateFullSampleData();
            current.LastWorkshop = "Цех Б";
            Assert.True(service.Save(current, out string? saveError), saveError);

            var missingSnapshot = new KnowledgeBaseSnapshotEntry
            {
                SnapshotId = "missing-snapshot",
                SnapshotPath = $"{path}#snapshot:missing-snapshot"
            };

            KnowledgeBaseSnapshotRestoreResult restoreResult = service.RestoreSnapshot(missingSnapshot);

            Assert.False(restoreResult.IsSuccess);
            KnowledgeBaseStorageLoadResult loadResult = service.Load();
            Assert.True(loadResult.IsSuccess, loadResult.ErrorMessage);
            Assert.Equal("Цех Б", loadResult.Data!.LastWorkshop);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void ChangeLog_RecordsSaveManualSnapshotRestoreAndCustomActions()
    {
        string tempDirectory = CreateTempDirectory();

        try
        {
            string path = Path.Combine(tempDirectory, "knowledge-base.akb");
            var service = new SqliteKnowledgeBaseStorageService(
                path,
                clock: () => new DateTimeOffset(2026, 5, 7, 12, 0, 0, TimeSpan.Zero));
            SavedData original = CreateFullSampleData();
            Assert.True(service.Save(original, out string? firstError), firstError);
            KnowledgeBaseSnapshotCreateResult manualSnapshot =
                service.CreateManualSnapshot(original, "Перед импортом");
            Assert.True(manualSnapshot.IsSuccess, manualSnapshot.ErrorMessage);
            KnowledgeBaseChangeLogAppendResult customLog = service.AppendChangeLog(
                "catalog-template-import",
                "Импорт каталога",
                "+1 запись");
            Assert.True(customLog.IsSuccess, customLog.ErrorMessage);

            SavedData changed = CreateFullSampleData();
            changed.LastWorkshop = "Цех Б";
            Assert.True(service.Save(changed, out string? secondError), secondError);
            KnowledgeBaseSnapshotEntry snapshot = service.ListSnapshots()
                .Snapshots
                .Single(entry => entry.Kind == "manual");
            KnowledgeBaseSnapshotRestoreResult restoreResult = service.RestoreSnapshot(snapshot);
            Assert.True(restoreResult.IsSuccess, restoreResult.ErrorMessage);

            KnowledgeBaseChangeLogListResult history = service.ListChangeLog();

            Assert.True(history.IsSuccess, history.ErrorMessage);
            Assert.Contains(history.Entries, entry => entry.ActionKind == "save");
            Assert.Contains(history.Entries, entry => entry.ActionKind == "manual-snapshot");
            Assert.Contains(history.Entries, entry => entry.ActionKind == "catalog-template-import");
            Assert.Contains(history.Entries, entry => entry.ActionKind == "restore");
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    private static SavedData CreateFullSampleData()
    {
        const string workshopA = "Цех А";
        const string workshopB = "Цех Б";
        const string rootId = "node-root";
        const string childId = "node-child";

        return new SavedData
        {
            SchemaVersion = SavedData.CurrentSchemaVersion,
            Config = new KbConfig
            {
                MaxLevels = 3,
                LevelNames = new List<string> { "Цех", "Линия", "Шкаф" },
                ProductionCalendarYears = new List<KbProductionCalendarYear>
                {
                    new()
                    {
                        Year = 2027,
                        AdditionalNonWorkingDays = new List<DateOnly>
                        {
                            new(2027, 1, 2)
                        },
                        AdditionalWorkingDays = new List<DateOnly>
                        {
                            new(2027, 2, 20)
                        }
                    }
                }
            },
            Workshops = new Dictionary<string, List<KbNode>>
            {
                [workshopA] = new List<KbNode>
                {
                    new()
                    {
                        NodeId = rootId,
                        Name = "Линия 1",
                        LevelIndex = 0,
                        NodeType = KbNodeType.System,
                        Details = new KbNodeDetails
                        {
                            Description = "Описание",
                            Location = "Корпус 1",
                            InventoryNumber = "INV-001",
                            PhotoPath = @"C:\photos\line.png",
                            IpAddress = "10.0.0.1",
                            SchemaLink = @"C:\schemes\line.pdf"
                        },
                        Children = new List<KbNode>
                        {
                            new()
                            {
                                NodeId = childId,
                                Name = "Шкаф 1",
                                LevelIndex = 1,
                                NodeType = KbNodeType.Cabinet,
                                Details = new KbNodeDetails
                                {
                                    Description = "Шкаф управления"
                                }
                            }
                        }
                    }
                },
                [workshopB] = new List<KbNode>
                {
                    new()
                    {
                        NodeId = "node-b",
                        Name = "Линия 2",
                        LevelIndex = 0,
                        NodeType = KbNodeType.System
                    }
                }
            },
            CompositionEntries = new List<KbCompositionEntry>
            {
                new()
                {
                    EntryId = "composition-1",
                    ParentNodeId = childId,
                    RackNumber = 1,
                    SlotNumber = 1,
                    PositionOrder = 2,
                    ComponentType = "ПЛК",
                    Model = "CPU 1214C",
                    OrderNumber = "6ES7 214-1AG40-0XB0",
                    Firmware = "V4.5",
                    MpiDpPnAddress = "PN/IE 10.0.0.10",
                    InputAddress = "I 0.0",
                    OutputAddress = "Q 4.0",
                    Comment = "Central rack",
                    InterfaceRows = "X1, Port 1",
                    IpAddress = "10.0.0.10",
                    LastCalibrationAt = new DateTime(2026, 5, 1, 12, 30, 0, DateTimeKind.Utc),
                    NextCalibrationAt = new DateTime(2027, 5, 1, 12, 30, 0, DateTimeKind.Utc),
                    Notes = "Основной контроллер"
                }
            },
            DocumentLinks = new List<KbDocumentLink>
            {
                new()
                {
                    DocumentId = "doc-1",
                    OwnerNodeId = childId,
                    Kind = KbDocumentKind.SchemeLink,
                    Title = "Схема",
                    Path = @"C:\docs\scheme.pdf",
                    UpdatedAt = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            },
            SoftwareRecords = new List<KbSoftwareRecord>
            {
                new()
                {
                    SoftwareId = "soft-1",
                    OwnerNodeId = childId,
                    Title = "Проект ПЛК",
                    Path = @"C:\software\plc",
                    AddedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastChangedAt = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                    LastBackupAt = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                    Notes = "Версия 1"
                }
            },
            NetworkFileReferences = new List<KbNetworkFileReference>
            {
                new()
                {
                    NetworkAssetId = "net-1",
                    OwnerNodeId = childId,
                    Title = "Топология",
                    Path = @"C:\network\topology.png",
                    SourceNote = "Лист 1, шкаф PLC-1",
                    PreviewKind = KbNetworkPreviewKind.Image
                }
            },
            NetworkDevices = new List<KbNetworkDevice>
            {
                new()
                {
                    NetworkDeviceId = "network-device-1",
                    OwnerNodeId = childId,
                    LinkedNodeId = childId,
                    Name = "PLC-1",
                    Role = "Controller",
                    Vendor = "Siemens",
                    Model = "CPU 1214C",
                    OrderNumber = "6ES7 214-1AG40-0XB0",
                    SerialNumber = "S123",
                    Firmware = "V4.5",
                    ProfinetName = "plc-1",
                    MacAddress = "00-11-22-33-44-55",
                    LocationText = "Line 1",
                    CabinetText = "Cabinet 1",
                    Notes = "Main controller"
                },
                new()
                {
                    NetworkDeviceId = "network-device-2",
                    OwnerNodeId = childId,
                    Name = "HMI-1",
                    Role = "Operator panel",
                    Vendor = "Siemens",
                    Model = "KTP700",
                    ProfinetName = "hmi-1",
                    MacAddress = "00-11-22-33-44-66",
                    CabinetText = "Cabinet 1"
                }
            },
            NetworkInterfaces = new List<KbNetworkInterface>
            {
                new()
                {
                    NetworkInterfaceId = "network-interface-1",
                    NetworkDeviceId = "network-device-1",
                    InterfaceName = "X1",
                    PortNumber = "1",
                    MacAddress = "00-11-22-33-44-55",
                    IpAddress = "10.0.0.10",
                    SubnetMask = "255.255.255.0",
                    Gateway = "10.0.0.1",
                    Vlan = "10",
                    Protocol = "Profinet",
                    MpiDpPnAddress = "PN/IE",
                    Speed = "100 Mbit/s",
                    Medium = "Copper",
                    Notes = "Controller PN port"
                },
                new()
                {
                    NetworkInterfaceId = "network-interface-2",
                    NetworkDeviceId = "network-device-2",
                    InterfaceName = "X1",
                    PortNumber = "1",
                    MacAddress = "00-11-22-33-44-66",
                    IpAddress = "10.0.0.20",
                    SubnetMask = "255.255.255.0",
                    Gateway = "10.0.0.1",
                    Protocol = "Profinet",
                    Speed = "100 Mbit/s",
                    Medium = "Copper",
                    Notes = "Panel PN port"
                }
            },
            NetworkConnections = new List<KbNetworkConnection>
            {
                new()
                {
                    NetworkConnectionId = "network-connection-1",
                    EndpointAInterfaceId = "network-interface-1",
                    EndpointBInterfaceId = "network-interface-2",
                    CableLabel = "W1",
                    CableType = "Profinet",
                    Protocol = "PROFINET",
                    Medium = "Copper",
                    Length = "12 m",
                    RouteText = "Operator room +7.0",
                    Status = "Active",
                    Notes = "PLC to HMI"
                }
            },
            MaintenanceScheduleProfiles = new List<KbMaintenanceScheduleProfile>
            {
                new()
                {
                    MaintenanceProfileId = "maintenance-1",
                    OwnerNodeId = childId,
                    IsIncludedInSchedule = true,
                    To1Hours = 1,
                    To2Hours = 4,
                    To3Hours = 8,
                    YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                    {
                        new() { Month = 1, WorkKind = KbMaintenanceWorkKind.To1, Hours = 2 },
                        new() { Month = 6, WorkKind = KbMaintenanceWorkKind.To2, Hours = 5 }
                    }
                }
            },
            EquipmentCatalogItems = new List<KbEquipmentCatalogItem>
            {
                new()
                {
                    CatalogItemId = "catalog-1",
                    EquipmentKind = "ПЛК",
                    Manufacturer = "Siemens",
                    Series = "S7-1200",
                    Model = "CPU 1214C",
                    DefaultNodeType = KbNodeType.Controller,
                    Description = "Контроллер",
                    Properties = new List<KbEquipmentCatalogProperty>
                    {
                        new() { Name = "DI", Value = "14" },
                        new() { Name = "DO", Value = "10" }
                    }
                }
            },
            ObjectTemplates = new List<KbObjectTemplate>
            {
                new()
                {
                    TemplateId = "template-1",
                    DisplayName = "Шкаф ПЛК",
                    Description = "Типовой шкаф",
                    Category = "Шкаф",
                    RootNode = new KbObjectTemplateNode
                    {
                        TemplateNodeId = "template-root",
                        CatalogItemId = "catalog-1",
                        Name = "Шкаф",
                        NodeType = KbNodeType.Cabinet,
                        Details = new KbNodeDetails
                        {
                            Description = "Описание шаблона"
                        },
                        Children = new List<KbObjectTemplateNode>
                        {
                            new()
                            {
                                TemplateNodeId = "template-child",
                                Name = "ПЛК",
                                NodeType = KbNodeType.Controller
                            }
                        }
                    },
                    CompositionEntries = new List<KbObjectTemplateCompositionEntry>
                    {
                        new()
                        {
                            ParentTemplateNodeId = "template-child",
                            RackNumber = 1,
                            SlotNumber = 1,
                            PositionOrder = 1,
                            ComponentType = "ПЛК",
                            Model = "CPU 1214C",
                            OrderNumber = "6ES7 214-1AG40-0XB0",
                            Firmware = "V4.5",
                            MpiDpPnAddress = "PN/IE 10.0.0.20",
                            InputAddress = "I 8.0",
                            OutputAddress = "Q 12.0",
                            Comment = "Template central rack",
                            InterfaceRows = "X2, Port 2",
                            IpAddress = "10.0.0.20",
                            LastCalibrationAt = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                            NextCalibrationAt = new DateTime(2027, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                            Notes = "Из шаблона"
                        }
                    },
                    DocumentLinks = new List<KbObjectTemplateDocumentLink>
                    {
                        new()
                        {
                            OwnerTemplateNodeId = "template-child",
                            Kind = KbDocumentKind.Manual,
                            Title = "Руководство",
                            Path = @"C:\docs\manual.pdf",
                            UpdatedAt = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc)
                        }
                    },
                    SoftwareRecords = new List<KbObjectTemplateSoftwareRecord>
                    {
                        new()
                        {
                            OwnerTemplateNodeId = "template-child",
                            Title = "TIA Portal",
                            Path = @"C:\software\tia",
                            AddedAt = new DateTime(2026, 5, 3, 0, 0, 0, DateTimeKind.Utc),
                            Notes = "ПО"
                        }
                    },
                    NetworkFileReferences = new List<KbObjectTemplateNetworkFileReference>
                    {
                        new()
                        {
                            OwnerTemplateNodeId = "template-child",
                            Title = "Сеть",
                            Path = @"C:\network\template.png"
                        }
                    },
                    MaintenanceScheduleProfiles = new List<KbObjectTemplateMaintenanceScheduleProfile>
                    {
                        new()
                        {
                            OwnerTemplateNodeId = "template-child",
                            IsIncludedInSchedule = true,
                            To1Hours = 1,
                            To2Hours = 2,
                            To3Hours = 3,
                            YearScheduleEntries = new List<KbMaintenanceYearScheduleEntry>
                            {
                                new() { Month = 12, WorkKind = KbMaintenanceWorkKind.To3 }
                            }
                        }
                    },
                    NetworkInterfaceStubs = new List<KbObjectTemplateNetworkInterfaceStub>
                    {
                        new()
                        {
                            OwnerTemplateNodeId = "template-child",
                            InterfaceId = "eth0",
                            Name = "Ethernet",
                            IpAddress = "10.0.0.20",
                            SubnetMask = "255.255.255.0",
                            Gateway = "10.0.0.1",
                            Protocol = "Profinet",
                            Notes = "Основной интерфейс"
                        }
                    }
                }
            },
            LastWorkshop = workshopA
        };
    }

    private static int ReadUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static HashSet<string> ReadTableNames(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";

        using var reader = command.ExecuteReader();
        var tables = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
            tables.Add(reader.GetString(0));

        return tables;
    }

    private static HashSet<string> ReadColumnNames(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"PRAGMA table_info({tableName});";

        using var reader = command.ExecuteReader();
        var columns = new HashSet<string>(StringComparer.Ordinal);
        while (reader.Read())
            columns.Add(reader.GetString(1));

        return columns;
    }

    private static int CountRows(SqliteConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static List<SnapshotRow> ReadSnapshotRows(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT snapshot_id, kind, note, payload_json
            FROM snapshots
            ORDER BY created_at;
            """;

        using var reader = command.ExecuteReader();
        var rows = new List<SnapshotRow>();
        while (reader.Read())
        {
            rows.Add(new SnapshotRow(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3)));
        }

        return rows;
    }

    private static string Serialize(SavedData data) =>
        JsonSerializer.Serialize(
            data,
            new JsonSerializerOptions
            {
                WriteIndented = false
            });

    private static string CreateTempDirectory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"asutp-sqlite-storage-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed record SnapshotRow(
        string SnapshotId,
        string Kind,
        string Note,
        string PayloadJson);
}
