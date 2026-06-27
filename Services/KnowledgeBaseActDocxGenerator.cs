using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public interface IKnowledgeBaseActDocxGenerator
    {
        KnowledgeBaseActDocxGenerationResult Generate(KnowledgeBaseActDocxGenerationRequest request);
    }

    public sealed class KnowledgeBaseActDocxGenerationRequest
    {
        public KbAct? Act { get; init; }

        public IReadOnlyList<KbActExecutor>? Executors { get; init; }

        public string TemplatePath { get; init; } = string.Empty;

        public string OutputPath { get; init; } = string.Empty;
    }

    public sealed class KnowledgeBaseActDocxGenerationResult
    {
        public bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; } = string.Empty;

        public string OutputPath { get; init; } = string.Empty;

        public string ContentHash { get; init; } = string.Empty;
    }

    public static class KnowledgeBaseActDocxTemplateService
    {
        public const string TemplateRootDirectoryName = "Templates";
        public const string ActTemplateDirectoryName = "Acts";
        public const string TemplateVersion = "1";

        public static string ResolveTemplatePath(KbActType actType, string applicationBasePath)
        {
            string basePath = string.IsNullOrWhiteSpace(applicationBasePath)
                ? AppContext.BaseDirectory
                : applicationBasePath;

            return Path.Combine(
                basePath,
                TemplateRootDirectoryName,
                ActTemplateDirectoryName,
                GetTemplateFileName(actType));
        }

        public static string GetTemplateId(KbActType actType) =>
            actType switch
            {
                KbActType.InspectionWork => "inspection_act",
                KbActType.EquipmentFailure => "equipment_failure_act",
                _ => "equipment_failure_act"
            };

        public static string GetTemplateFileName(KbActType actType) =>
            $"{GetTemplateId(actType)}.docx";
    }
}
