using Cysharp.Threading.Tasks;
using System;
using System.Threading;
using UnityEngine;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline.Phases
{
    /// <summary>
    /// Fase 1: Ciclo Noturno. 
    /// Processa a lógica de crescimento/morte das plantas (puramente lógica).
    /// </summary>
    public class NightCyclePhase : IAnalysisPhase
    {
        public string Name => "Night Cycle";

        public async UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct)
        {
            int totalSlots = context.GridService.SlotCount;
            int processedCount = 0;

            for (int i = 0; i < totalSlots; i++)
            {
                if (ct.IsCancellationRequested) 
                    throw new OperationCanceledException();

                if (context.GridService.IsSlotUnlocked(i))
                {
                    context.GridService.ProcessNightCycleForSlot(i);
                    processedCount++;
                }

                progress?.Report(new PhaseProgress
                {
                    PhaseName = Name,
                    Percentage = (float)i / totalSlots,
                    CurrentAction = $"Processing night cycle: {i + 1}/{totalSlots}"
                });

                // Pequeno delay para não travar a main thread se houver muitos slots (opcional)
                if (i % 5 == 0) await UniTask.Yield();
            }

            return new PhaseResult
            {
                Success = true,
                Message = $"Processed night cycle for {processedCount} unlocked slots."
            };
        }
    }
}
