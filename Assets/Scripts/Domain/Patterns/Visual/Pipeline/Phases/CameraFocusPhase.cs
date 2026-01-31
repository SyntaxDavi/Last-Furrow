using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase que aguarda a câmera se estabilizar após focar no grid.
    /// Garante que as animações visuais só comecem depois que a câmera parou de se mover.
    /// </summary>
    public class CameraFocusPhase : IAnalysisPhase
    {
        public string Name => "Camera Focus";
        
        private readonly float _stabilizationDelay;
        
        public CameraFocusPhase(float stabilizationDelayMs = 400f)
        {
            _stabilizationDelay = stabilizationDelayMs;
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
                CurrentAction = "Focusing camera on grid..."
            });

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            // Aguarda a câmera se estabilizar (SmoothDamp leva tempo)
            await UniTask.Delay((int)_stabilizationDelay, cancellationToken: ct);

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 1f,
                CurrentAction = "Camera focused"
            });

            return new PhaseResult
            {
                Success = true,
                Message = "Camera focused on grid."
            };
        }
    }
}
