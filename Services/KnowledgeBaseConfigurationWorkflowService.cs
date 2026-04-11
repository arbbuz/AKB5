using System;
using System.Collections.Generic;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public class KnowledgeBaseConfigurationUpdateResult
    {
        public bool IsSuccess { get; init; }

        public KbConfig Config { get; init; } = KnowledgeBaseDataService.CreateDefaultConfig();

        public int MaxUsedLevel { get; init; } = -1;

        public string? ErrorMessage { get; init; }
    }

    /// <summary>
    /// Координирует изменение конфигурации уровней без UI-зависимостей:
    /// нормализует вход, валидирует с учётом уже существующих данных и возвращает прикладной результат.
    /// </summary>
    public class KnowledgeBaseConfigurationWorkflowService
    {
        public KnowledgeBaseConfigurationUpdateResult ValidateAndNormalize(
            KbConfig proposedConfig,
            Dictionary<string, List<KbNode>> workshops)
        {
            var normalizedConfig = KnowledgeBaseDataService.NormalizeConfig(proposedConfig);
            var validationService = new KnowledgeBaseService(normalizedConfig, workshops);
            int maxUsedLevel = -1;

            foreach (var roots in workshops.Values)
                maxUsedLevel = Math.Max(maxUsedLevel, validationService.GetMaxLevelIndex(roots));

            if (maxUsedLevel >= normalizedConfig.MaxLevels)
            {
                return new KnowledgeBaseConfigurationUpdateResult
                {
                    IsSuccess = false,
                    Config = normalizedConfig,
                    MaxUsedLevel = maxUsedLevel,
                    ErrorMessage =
                        $"Нельзя уменьшить количество уровней до {normalizedConfig.MaxLevels}. " +
                        $"В базе уже используется уровень {maxUsedLevel + 1}."
                };
            }

            return new KnowledgeBaseConfigurationUpdateResult
            {
                IsSuccess = true,
                Config = normalizedConfig,
                MaxUsedLevel = maxUsedLevel
            };
        }
    }
}
