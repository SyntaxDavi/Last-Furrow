using Cysharp.Threading.Tasks;
using System;
using System.Threading;

namespace LastFurrow.Domain.Patterns.Visual.Pipeline
{
    /// <summary>
    /// Interface para uma fase individual do pipeline de análise.
    /// Segue o SRP: Cada fase cuida de uma parte específica da resolução noturna.
    /// </summary>
    public interface IAnalysisPhase
    {
        string Name { get; }

        /// <summary>
        /// Executa a fase de forma assíncrona.
        /// </summary>
        UniTask<PhaseResult> ExecuteAsync(
            AnalysisContext context, 
            IProgress<PhaseProgress> progress, 
            CancellationToken ct);
    }
}
