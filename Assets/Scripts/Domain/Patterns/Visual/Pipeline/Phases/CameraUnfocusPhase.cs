using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase que dispara o retorno da câmera para posição normal e aguarda estabilização.
    /// Executada no final do pipeline, antes do Fan-In das cartas.
    /// </summary>
    public class CameraUnfocusPhase : IAnalysisPhase
    {
        public string Name => "Camera Unfocus";
        
        private readonly float _stabilizationDelay;
        
        public CameraUnfocusPhase(float stabilizationDelayMs = 500f)
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
                CurrentAction = "Camera returning to normal..."
            });

            if (ct.IsCancellationRequested)
                throw new OperationCanceledException();

            // Dispara evento para câmera voltar à posição normal
            // Isso desativa o SetForced() no CameraEdgeScroll
            context.Events?.Time.TriggerResolutionEnded();
            
            Debug.Log("[CameraUnfocusPhase] ResolutionEnded disparado - câmera voltando.");

            // Aguarda a câmera se estabilizar na posição normal
            await UniTask.Delay((int)_stabilizationDelay, cancellationToken: ct);

            progress?.Report(new PhaseProgress
            {
                PhaseName = Name,
                Percentage = 1f,
                CurrentAction = "Camera returned"
            });

            return new PhaseResult
            {
                Success = true,
                Message = "Camera returned to normal position."
            };
        }
    }
}
