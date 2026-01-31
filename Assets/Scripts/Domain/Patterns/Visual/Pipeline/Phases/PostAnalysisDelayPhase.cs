using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase final do pipeline: Delay pós-análise.
    /// Dá um respiro visual antes do próximo ciclo (draw diário, próximo dia, etc).
    /// </summary>
    public class PostAnalysisDelayPhase : IAnalysisPhase
    {
        public string Name => "Post-Analysis Delay";
        
        private readonly float _delayMs;
        
        public PostAnalysisDelayPhase(float delayMs = 600f)
        {
            _delayMs = delayMs;
        }

        public async UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct)
        {
            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 0f,
                CurrentAction = "Settling..."
            });

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            // Delay configurável para respiração visual
            await UniTask.Delay((int)_delayMs, cancellationToken: ct);

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 1f,
                CurrentAction = "Ready"
            });

            return new PhaseResult
            {
                Success = true,
                Message = "Post-analysis delay complete."
            };
        }
    }
}
