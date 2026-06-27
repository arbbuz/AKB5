using AsutpKnowledgeBase.Models;
using AsutpKnowledgeBase.Services;

namespace AsutpKnowledgeBase.Core.Tests;

public class KnowledgeBaseActDataServiceTests
{
    [Fact]
    public void NormalizeActs_TrimsFieldsAndKeepsOrderNumberSeparateFromSerialNumber()
    {
        var normalized = KnowledgeBaseDataService.NormalizeActs(new[]
        {
            new KbAct
            {
                ActId = " act-1 ",
                ActYear = 2026,
                ActNumber = " 2026-0001 ",
                ActType = (KbActType)99,
                Status = (KbActStatus)99,
                ActDate = new DateTime(2026, 6, 24),
                WorkshopName = " Workshop 1 ",
                Lvl3NodeId = " cabinet-1 ",
                Lvl3NameSnapshot = " Cabinet 1 ",
                ObjectNameSnapshot = " System 1 ",
                ObjectPathSnapshot = " Area / System / Cabinet 1 ",
                RackId = " rack-1 ",
                RackNumberSnapshot = -1,
                RackNameSnapshot = " Rack A ",
                CompositionEntryId = " entry-1 ",
                EquipmentName = " Module DI ",
                EquipmentSnapshot = new KbActEquipmentSnapshot
                {
                    Lvl3Name = " Cabinet 1 ",
                    ObjectPath = " Area / System / Cabinet 1 ",
                    RackId = " rack-1 ",
                    RackNumber = -1,
                    RackName = " Rack A ",
                    CompositionEntryId = " entry-1 ",
                    ComponentType = " DI ",
                    Model = " Module DI ",
                    OrderNumber = " 6ES7 321-1BH02-0AA0 ",
                    SerialNumber = " ",
                    Notes = " Main input module "
                },
                FailureDate = new DateTime(2026, 6, 23),
                FaultDescription = " No input ",
                FailureReason = " Unknown ",
                InspectionResult = " Replace module ",
                FaultCriterion = " Failure ",
                RequestDocument = " Request-1 ",
                ActualLaborHours = " 2.5 ",
                CustomerName = " Customer ",
                CustomerPosition = " Engineer ",
                ApproverName = " Head ",
                ApproverPosition = " Automation head ",
                CreatedBy = " Operator ",
                CreatedAt = new DateTime(2026, 6, 24, 10, 0, 0),
                UpdatedAt = new DateTime(2026, 6, 24, 11, 0, 0)
            }
        });

        KbAct act = Assert.Single(normalized);
        Assert.Equal("act-1", act.ActId);
        Assert.Equal("2026-0001", act.ActNumber);
        Assert.Equal(KbActType.EquipmentFailure, act.ActType);
        Assert.Equal(KbActStatus.Draft, act.Status);
        Assert.Null(act.RackNumberSnapshot);
        Assert.Equal("Workshop 1", act.WorkshopName);
        Assert.Equal("cabinet-1", act.Lvl3NodeId);
        Assert.Equal("System 1", act.ObjectNameSnapshot);
        Assert.Equal("entry-1", act.CompositionEntryId);
        Assert.Equal("Module DI", act.EquipmentName);
        Assert.Equal("6ES7 321-1BH02-0AA0", act.EquipmentSnapshot.OrderNumber);
        Assert.Equal(string.Empty, act.EquipmentSnapshot.SerialNumber);
        Assert.Null(act.EquipmentSnapshot.RackNumber);
        Assert.Equal("2.5", act.ActualLaborHours);
        Assert.Equal("Head", act.ApproverName);
        Assert.Equal("Automation head", act.ApproverPosition);
        Assert.Null(typeof(KbAct).GetProperty("SlotNumber"));
        Assert.Null(typeof(KbActEquipmentSnapshot).GetProperty("SlotNumber"));
    }

    [Fact]
    public void NormalizeActs_GeneratesUniqueIdsButDoesNotAllocateActNumbers()
    {
        List<KbAct> normalized = KnowledgeBaseDataService.NormalizeActs(new[]
        {
            new KbAct
            {
                ActId = "same",
                ActYear = 2026
            },
            new KbAct
            {
                ActId = "same",
                ActYear = 2026
            }
        });

        Assert.Equal(2, normalized.Count);
        Assert.Equal(2, normalized.Select(static act => act.ActId).Distinct(StringComparer.Ordinal).Count());
        Assert.All(normalized, static act => Assert.Equal(string.Empty, act.ActNumber));
    }

    [Fact]
    public void NormalizeActExecutors_FiltersUnknownActsAndBlankRows()
    {
        List<KbActExecutor> normalized = KnowledgeBaseDataService.NormalizeActExecutors(
            new[]
            {
                new KbActExecutor
                {
                    ExecutorId = " executor-1 ",
                    ActId = " act-1 ",
                    SortOrder = -1,
                    LastName = " Ivanov ",
                    FirstName = " Ivan ",
                    MiddleName = " Ivanovich ",
                    Position = " Engineer "
                },
                new KbActExecutor
                {
                    ActId = "unknown",
                    LastName = " Unknown "
                },
                new KbActExecutor
                {
                    ActId = "act-1"
                }
            },
            new[] { " act-1 " });

        KbActExecutor executor = Assert.Single(normalized);
        Assert.Equal("executor-1", executor.ExecutorId);
        Assert.Equal("act-1", executor.ActId);
        Assert.Equal(0, executor.SortOrder);
        Assert.Equal("Ivanov", executor.LastName);
        Assert.Equal("Ivan", executor.FirstName);
        Assert.Equal("Ivanovich", executor.MiddleName);
        Assert.Equal("Engineer", executor.Position);
    }

    [Fact]
    public void NormalizeActDocuments_NormalizesVersionAndFiltersUnknownActs()
    {
        List<KbActDocument> normalized = KnowledgeBaseDataService.NormalizeActDocuments(
            new[]
            {
                new KbActDocument
                {
                    DocumentId = " doc-1 ",
                    ActId = " act-1 ",
                    VersionNumber = 0,
                    TemplateId = " template-1 ",
                    TemplateVersion = " v1 ",
                    Path = " Documents\\Acts\\2026-0001.docx ",
                    GeneratedAt = new DateTime(2026, 6, 24),
                    ContentHash = " hash "
                },
                new KbActDocument
                {
                    ActId = "unknown",
                    Path = " Documents\\Acts\\unknown.docx "
                },
                new KbActDocument
                {
                    ActId = "act-1"
                }
            },
            new[] { "act-1" });

        KbActDocument document = Assert.Single(normalized);
        Assert.Equal("doc-1", document.DocumentId);
        Assert.Equal("act-1", document.ActId);
        Assert.Equal(1, document.VersionNumber);
        Assert.Equal("template-1", document.TemplateId);
        Assert.Equal("v1", document.TemplateVersion);
        Assert.Equal("Documents\\Acts\\2026-0001.docx", document.Path);
        Assert.Equal("hash", document.ContentHash);
        Assert.True(document.IsLatest);
    }

    [Fact]
    public void NormalizeActNumberSequences_KeepsHighestNextNumberPerYear()
    {
        List<KbActNumberSequence> normalized = KnowledgeBaseDataService.NormalizeActNumberSequences(new[]
        {
            new KbActNumberSequence
            {
                Year = 2026,
                NextNumber = 3
            },
            new KbActNumberSequence
            {
                Year = 2026,
                NextNumber = 7
            },
            new KbActNumberSequence
            {
                Year = 2027,
                NextNumber = 0
            },
            new KbActNumberSequence
            {
                Year = 0,
                NextNumber = 100
            }
        });

        Assert.Collection(
            normalized,
            first =>
            {
                Assert.Equal(2026, first.Year);
                Assert.Equal(7, first.NextNumber);
            },
            second =>
            {
                Assert.Equal(2027, second.Year);
                Assert.Equal(1, second.NextNumber);
            });
    }

    [Fact]
    public void NormalizeSavedData_NormalizesActCollectionsAndFiltersOrphans()
    {
        SavedData normalized = KnowledgeBaseDataService.NormalizeSavedData(
            new SavedData
            {
                Workshops = new Dictionary<string, List<KbNode>>
                {
                    ["Цех"] = new()
                },
                Acts = new List<KbAct>
                {
                    new()
                    {
                        ActId = " act-1 ",
                        ActYear = 2026,
                        ActNumber = " 2026-0001 "
                    }
                },
                ActExecutors = new List<KbActExecutor>
                {
                    new()
                    {
                        ActId = " act-1 ",
                        LastName = " Ivanov "
                    },
                    new()
                    {
                        ActId = "unknown",
                        LastName = " Orphan "
                    }
                },
                ActDocuments = new List<KbActDocument>
                {
                    new()
                    {
                        ActId = " act-1 ",
                        Path = " Documents\\Acts\\2026-0001.docx "
                    },
                    new()
                    {
                        ActId = "unknown",
                        Path = " Documents\\Acts\\unknown.docx "
                    }
                },
                ActNumberSequences = new List<KbActNumberSequence>
                {
                    new()
                    {
                        Year = 2026,
                        NextNumber = 3
                    },
                    new()
                    {
                        Year = 2026,
                        NextNumber = 8
                    }
                },
                LastWorkshop = "Цех"
            });

        KbAct act = Assert.Single(normalized.Acts);
        Assert.Equal("act-1", act.ActId);
        Assert.Equal("2026-0001", act.ActNumber);
        Assert.Equal("act-1", Assert.Single(normalized.ActExecutors).ActId);
        Assert.Equal("Documents\\Acts\\2026-0001.docx", Assert.Single(normalized.ActDocuments).Path);
        Assert.Equal(8, Assert.Single(normalized.ActNumberSequences).NextNumber);
    }
}
