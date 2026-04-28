using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public sealed class KnowledgeBaseCompositionTransferResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public string AppliedTemplateName { get; init; } = string.Empty;

        public List<KbCompositionEntry> CompositionEntries { get; init; } = new();

        public int ImportedEntryCount { get; init; }
    }

    public sealed class KnowledgeBaseCompositionTemplateService
    {
        private static readonly IReadOnlyList<KbCompositionTemplate> Templates =
        [
            new KbCompositionTemplate
            {
                TemplateId = "cabinet-plc-standard",
                DisplayName = "Шкаф: типовой PLC",
                Description =
                    "Создает шкаф со стойкой контроллера, пятью слотами PLC-модулей и базовым вспомогательным оборудованием.",
                SuggestedNodeName = "Новый шкаф",
                TargetNodeType = KbNodeType.Cabinet,
                Entries =
                [
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "Контроллер",
                        Model = "PLC CPU",
                        Notes = "Основной контроллер шкафа."
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 2,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 3,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный вывод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 4,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Аналоговый ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 5,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Аналоговый вывод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = null,
                        PositionOrder = 0,
                        ComponentType = "Блок питания",
                        Model = "24V DC"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = null,
                        PositionOrder = 1,
                        ComponentType = "Сетевой коммутатор",
                        Model = "Industrial Ethernet"
                    }
                ]
            },
            new KbCompositionTemplate
            {
                TemplateId = "cabinet-drive-control",
                DisplayName = "Шкаф: управление приводом",
                Description =
                    "Создает шкаф с PLC, модулем связи и типовым вспомогательным оборудованием для управления приводом.",
                SuggestedNodeName = "Шкаф привода",
                TargetNodeType = KbNodeType.Cabinet,
                Entries =
                [
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "Контроллер",
                        Model = "PLC CPU"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 2,
                        PositionOrder = 0,
                        ComponentType = "Модуль связи",
                        Model = "Profinet/Profibus gateway"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 3,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 4,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный вывод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = null,
                        PositionOrder = 0,
                        ComponentType = "Блок питания",
                        Model = "24V DC"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = null,
                        PositionOrder = 1,
                        ComponentType = "Пускатель",
                        Model = "Motor starter assembly"
                    }
                ]
            },
            new KbCompositionTemplate
            {
                TemplateId = "controller-plc-compact",
                DisplayName = "Контроллер: компактный PLC",
                Description =
                    "Создает компактный контроллер с CPU и базовыми слотами дискретного ввода/вывода для типовых шкафов.",
                SuggestedNodeName = "Новый контроллер",
                TargetNodeType = KbNodeType.Controller,
                Entries =
                [
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "CPU",
                        Model = "Compact PLC CPU",
                        Notes = "Основной CPU контроллера."
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 2,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 3,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный вывод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = null,
                        PositionOrder = 0,
                        ComponentType = "Блок питания",
                        Model = "24V DC"
                    }
                ]
            },
            new KbCompositionTemplate
            {
                TemplateId = "controller-plc-extended",
                DisplayName = "Контроллер: расширенный PLC",
                Description =
                    "Создает контроллер с CPU, цифровыми и аналоговыми модулями ввода/вывода и модулем связи для более крупных систем.",
                SuggestedNodeName = "PLC-стойка",
                TargetNodeType = KbNodeType.Controller,
                Entries =
                [
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 1,
                        PositionOrder = 0,
                        ComponentType = "CPU",
                        Model = "Modular PLC CPU"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 2,
                        PositionOrder = 0,
                        ComponentType = "Модуль связи",
                        Model = "Ethernet/fieldbus"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 3,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 4,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Дискретный вывод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 5,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Аналоговый ввод"
                    },
                    new KbCompositionTemplateEntry
                    {
                        SlotNumber = 6,
                        PositionOrder = 0,
                        ComponentType = "Модуль ввода/вывода",
                        Model = "Аналоговый вывод"
                    }
                ]
            }
        ];

        public IReadOnlyList<KbCompositionTemplate> GetTemplates(KbNodeType nodeType) =>
            Templates
                .Where(template => template.TargetNodeType == nodeType)
                .ToList();

        public IReadOnlyList<KbCompositionTemplate> GetChildTemplates(KbNode? parentNode) =>
            TryResolveTemplateChildType(parentNode, out var targetNodeType)
                ? GetTemplates(targetNodeType)
                : Array.Empty<KbCompositionTemplate>();

        public KbCompositionTemplate? FindTemplate(string? templateId)
        {
            string normalizedTemplateId = templateId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedTemplateId))
                return null;

            return Templates.FirstOrDefault(template =>
                string.Equals(template.TemplateId, normalizedTemplateId, StringComparison.Ordinal));
        }

        public bool TryResolveTemplateChildType(KbNode? parentNode, out KbNodeType childNodeType)
        {
            childNodeType = parentNode?.NodeType switch
            {
                KbNodeType.System => KbNodeType.Cabinet,
                KbNodeType.Cabinet => KbNodeType.Controller,
                _ => KbNodeType.Unknown
            };

            return childNodeType != KbNodeType.Unknown;
        }

        public KnowledgeBaseCompositionTransferResult ApplyTemplate(
            KbNode? targetNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            string? templateId)
        {
            if (!TryValidateTargetNode(targetNode, out var targetNodeId, out var errorMessage))
                return Failure(errorMessage);

            var template = FindTemplate(templateId);
            if (template == null)
                return Failure("Шаблон состава не найден.");

            if (template.TargetNodeType != targetNode!.NodeType)
            {
                return Failure(
                    $"Шаблон \"{template.DisplayName}\" не подходит для узлов типа \"{targetNode.NodeType}\".");
            }

            var updatedEntries = CopyEntriesExceptTarget(compositionEntries, targetNodeId);
            updatedEntries.AddRange(CreateEntriesFromTemplate(targetNodeId, template));

            return Success(updatedEntries, template.DisplayName, template.Entries.Count);
        }

        public KnowledgeBaseCompositionTransferResult CopyComposition(
            KbNode? targetNode,
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            KbNode? sourceNode)
        {
            if (!TryValidateTargetNode(targetNode, out var targetNodeId, out var errorMessage))
                return Failure(errorMessage);

            if (sourceNode == null)
                return Failure("Не выбран объект-источник.");

            if (!KnowledgeBaseCompositionStateService.SupportsComposition(sourceNode.NodeType))
                return Failure("Выбранный объект-источник не поддерживает вкладку \"Состав\".");

            if (string.Equals(targetNodeId, sourceNode.NodeId?.Trim(), StringComparison.Ordinal))
                return Failure("Выберите другой объект-источник.");

            if (targetNode!.NodeType != sourceNode.NodeType)
            {
                return Failure(
                    $"Состав можно копировать только между узлами одного типа. Источник: {sourceNode.NodeType}, получатель: {targetNode.NodeType}.");
            }

            var sourceEntries = (compositionEntries ?? Array.Empty<KbCompositionEntry>())
                .Where(entry => string.Equals(entry.ParentNodeId, sourceNode.NodeId, StringComparison.Ordinal))
                .OrderBy(entry => entry.SlotNumber.HasValue ? 0 : 1)
                .ThenBy(entry => entry.SlotNumber ?? int.MaxValue)
                .ThenBy(entry => entry.PositionOrder)
                .ThenBy(entry => entry.EntryId, StringComparer.Ordinal)
                .ToList();

            if (sourceEntries.Count == 0)
                return Failure("У объекта-источника нет заполненных записей состава.");

            var updatedEntries = CopyEntriesExceptTarget(compositionEntries, targetNodeId);
            updatedEntries.AddRange(CloneEntriesForTarget(sourceEntries, targetNodeId));
            return Success(updatedEntries, appliedTemplateName: string.Empty, sourceEntries.Count);
        }

        public static string BuildInheritedLocation(KbNode? parentNode)
        {
            if (parentNode == null)
                return string.Empty;

            string parentLocation = parentNode.Details?.Location?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(parentLocation))
                return parentLocation;

            return parentNode.Name?.Trim() ?? string.Empty;
        }

        private static bool TryValidateTargetNode(
            KbNode? targetNode,
            out string targetNodeId,
            out string errorMessage)
        {
            if (targetNode == null)
            {
                targetNodeId = string.Empty;
                errorMessage = "Не выбран узел для заполнения состава.";
                return false;
            }

            if (!KnowledgeBaseCompositionStateService.SupportsComposition(targetNode.NodeType))
            {
                targetNodeId = string.Empty;
                errorMessage = "Выбранный узел не поддерживает вкладку \"Состав\".";
                return false;
            }

            targetNodeId = targetNode.NodeId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(targetNodeId))
            {
                errorMessage = "У выбранного узла отсутствует корректный NodeId.";
                return false;
            }

            errorMessage = string.Empty;
            return true;
        }

        private static List<KbCompositionEntry> CopyEntriesExceptTarget(
            IReadOnlyList<KbCompositionEntry>? compositionEntries,
            string targetNodeId)
        {
            var updatedEntries = new List<KbCompositionEntry>();
            if (compositionEntries == null)
                return updatedEntries;

            foreach (var entry in compositionEntries)
            {
                if (string.Equals(entry.ParentNodeId, targetNodeId, StringComparison.Ordinal))
                    continue;

                updatedEntries.Add(CloneEntry(entry));
            }

            return updatedEntries;
        }

        private static IEnumerable<KbCompositionEntry> CreateEntriesFromTemplate(
            string targetNodeId,
            KbCompositionTemplate template)
        {
            foreach (var entry in template.Entries)
            {
                yield return new KbCompositionEntry
                {
                    EntryId = string.Empty,
                    ParentNodeId = targetNodeId,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                };
            }
        }

        private static IEnumerable<KbCompositionEntry> CloneEntriesForTarget(
            IEnumerable<KbCompositionEntry> sourceEntries,
            string targetNodeId)
        {
            foreach (var entry in sourceEntries)
            {
                yield return new KbCompositionEntry
                {
                    EntryId = string.Empty,
                    ParentNodeId = targetNodeId,
                    SlotNumber = entry.SlotNumber,
                    PositionOrder = entry.PositionOrder,
                    ComponentType = entry.ComponentType,
                    Model = entry.Model,
                    IpAddress = entry.IpAddress,
                    LastCalibrationAt = entry.LastCalibrationAt,
                    NextCalibrationAt = entry.NextCalibrationAt,
                    Notes = entry.Notes
                };
            }
        }

        private static KbCompositionEntry CloneEntry(KbCompositionEntry entry) =>
            new()
            {
                EntryId = entry.EntryId,
                ParentNodeId = entry.ParentNodeId,
                SlotNumber = entry.SlotNumber,
                PositionOrder = entry.PositionOrder,
                ComponentType = entry.ComponentType,
                Model = entry.Model,
                IpAddress = entry.IpAddress,
                LastCalibrationAt = entry.LastCalibrationAt,
                NextCalibrationAt = entry.NextCalibrationAt,
                Notes = entry.Notes
            };

        private static KnowledgeBaseCompositionTransferResult Success(
            List<KbCompositionEntry> compositionEntries,
            string appliedTemplateName,
            int importedEntryCount) =>
            new()
            {
                IsSuccess = true,
                AppliedTemplateName = appliedTemplateName,
                CompositionEntries = compositionEntries,
                ImportedEntryCount = importedEntryCount
            };

        private static KnowledgeBaseCompositionTransferResult Failure(string errorMessage) =>
            new()
            {
                IsSuccess = false,
                ErrorMessage = errorMessage
            };
    }
}
