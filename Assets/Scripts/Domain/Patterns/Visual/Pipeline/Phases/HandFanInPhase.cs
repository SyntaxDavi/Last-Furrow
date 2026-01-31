using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase final do pipeline: Fan-In da Mão.
    /// Retorna as cartas para a tela de forma sequencial após a câmera se estabilizar.
    /// </summary>
    public class HandFanInPhase : IAnalysisPhase
    {
        public string Name => "Hand Fan-In";
        
        private readonly HandManager _handManager;
        
        public HandFanInPhase(HandManager handManager)
        {
            _handManager = handManager;
        }

        public async UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct)
        {
            if (_handManager == null)
            {
                Debug.LogWarning("[HandFanInPhase] HandManager not available, skipping fan-in.");
                return new PhaseResult
                {
                    Success = true,
                    Message = "Skipped - no HandManager"
                };
            }

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 0f,
                CurrentAction = "Cards returning to screen..."
            });

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            var fanController = _handManager.GetFanController();
            if (fanController != null)
            {
                await fanController.FanIn();
            }

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 1f,
                CurrentAction = "Cards returned"
            });

            return new PhaseResult
            {
                Success = true,
                Message = "Hand cards returned to screen."
            };
        }
    }
}
