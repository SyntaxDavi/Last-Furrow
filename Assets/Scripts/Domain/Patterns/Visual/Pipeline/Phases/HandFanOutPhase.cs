using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase 0: Fan-Out da Mão.
    /// Move as cartas para fora da tela antes da análise começar.
    /// </summary>
    public class HandFanOutPhase : IAnalysisPhase
    {
        public string Name => "Hand Fan-Out";
        
        private readonly HandManager _handManager;
        
        public HandFanOutPhase(HandManager handManager)
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
                Debug.LogWarning("[HandFanOutPhase] HandManager not available, skipping fan-out.");
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
                CurrentAction = "Cards exiting screen..."
            });

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            await _handManager.FanOut();

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 1f,
                CurrentAction = "Cards hidden"
            });

            return new PhaseResult
            {
                Success = true,
                Message = "Hand cards fanned out successfully."
            };
        }
    }
}
