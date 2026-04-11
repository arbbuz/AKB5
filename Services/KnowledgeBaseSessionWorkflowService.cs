using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using AsutpKnowledgeBase.Models;

namespace AsutpKnowledgeBase.Services
{
    public enum KnowledgeBaseSessionTransitionFailure
    {
        None,
        InvalidSnapshot,
        InvalidWorkshopName,
        DuplicateWorkshopName,
        TransitionRejected
    }

    public class KnowledgeBaseSessionViewState
    {
        public string CurrentWorkshop { get; init; } = string.Empty;

        public IReadOnlyList<string> WorkshopNames { get; init; } = Array.Empty<string>();

        public IReadOnlyList<KbNode> CurrentRoots { get; init; } = Array.Empty<KbNode>();
    }

    public class KnowledgeBaseSessionTransitionResult
    {
        public bool IsSuccess { get; init; }

        public KnowledgeBaseSessionTransitionFailure Failure { get; init; }

        public string? ErrorMessage { get; init; }

        public KnowledgeBaseSessionViewState ViewState { get; init; } = new();
    }

    /// <summary>
    /// Координирует переходы session-state без UI-зависимостей:
    /// восстановление snapshot, переключение цеха и добавление нового цеха.
    /// </summary>
    public class KnowledgeBaseSessionWorkflowService
    {
        private readonly KnowledgeBaseSessionService _session;

        public KnowledgeBaseSessionWorkflowService(KnowledgeBaseSessionService session)
        {
            _session = session;
        }

        public KnowledgeBaseSessionViewState BuildViewState() =>
            new()
            {
                CurrentWorkshop = _session.CurrentWorkshop,
                WorkshopNames = _session.Workshops.Keys.ToList(),
                CurrentRoots = _session.GetCurrentWorkshopNodes()
            };

        public KnowledgeBaseSessionTransitionResult RestoreSnapshot(string json)
        {
            try
            {
                var data = JsonSerializer.Deserialize<SavedData>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (data == null)
                {
                    return new KnowledgeBaseSessionTransitionResult
                    {
                        Failure = KnowledgeBaseSessionTransitionFailure.InvalidSnapshot,
                        ErrorMessage = "Snapshot состояния пуст или имеет некорректный формат."
                    };
                }

                _session.ApplyLoadedData(data, recordAsSavedState: false);
                return Success();
            }
            catch (Exception ex)
            {
                return new KnowledgeBaseSessionTransitionResult
                {
                    Failure = KnowledgeBaseSessionTransitionFailure.InvalidSnapshot,
                    ErrorMessage = $"Ошибка восстановления состояния: {ex.Message}"
                };
            }
        }

        public KnowledgeBaseSessionTransitionResult SelectWorkshop(
            string selectedWorkshop,
            List<KbNode> currentWorkshopRoots)
        {
            if (!_session.TrySelectWorkshop(selectedWorkshop, currentWorkshopRoots))
            {
                return new KnowledgeBaseSessionTransitionResult
                {
                    Failure = KnowledgeBaseSessionTransitionFailure.TransitionRejected
                };
            }

            return Success();
        }

        public KnowledgeBaseSessionTransitionResult AddWorkshop(
            string workshopName,
            List<KbNode> currentWorkshopRoots)
        {
            if (string.IsNullOrWhiteSpace(workshopName))
            {
                return new KnowledgeBaseSessionTransitionResult
                {
                    Failure = KnowledgeBaseSessionTransitionFailure.InvalidWorkshopName,
                    ErrorMessage = "Название цеха не должно быть пустым."
                };
            }

            string normalizedWorkshop = workshopName.Trim();
            if (_session.HasWorkshop(normalizedWorkshop))
            {
                return new KnowledgeBaseSessionTransitionResult
                {
                    Failure = KnowledgeBaseSessionTransitionFailure.DuplicateWorkshopName,
                    ErrorMessage = "Цех с таким названием уже существует."
                };
            }

            if (!_session.TryAddWorkshop(normalizedWorkshop, currentWorkshopRoots))
            {
                return new KnowledgeBaseSessionTransitionResult
                {
                    Failure = KnowledgeBaseSessionTransitionFailure.TransitionRejected,
                    ErrorMessage = "Не удалось добавить новый цех."
                };
            }

            return Success();
        }

        private KnowledgeBaseSessionTransitionResult Success() =>
            new()
            {
                IsSuccess = true,
                Failure = KnowledgeBaseSessionTransitionFailure.None,
                ViewState = BuildViewState()
            };
    }
}
