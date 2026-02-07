using UnityEngine;
using Cysharp.Threading.Tasks;
using System;

namespace LastFurrow.Domain.Patterns.Visual.Handlers
{
    /// <summary>
    /// Handler visual para scores passivos. 
    /// Desacopla a animação (levitação/popup) da lógica do pipeline.
    /// </summary>
    public class PassiveScoreVisualHandler : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GridVisualConfig _config;

        private void OnEnable()
        {
            // Aguarda AppCore estar pronto para se inscrever
            SubscribeAsync().Forget();
        }

        private async UniTaskVoid SubscribeAsync()
        {
            await UniTask.WaitUntil(() => AppCore.Instance?.Events?.Grid != null);
            AppCore.Instance.Events.Grid.OnCropPassiveScore += HandlePassiveScore;
        }

        private void OnDisable()
        {
            if (AppCore.Instance?.Events?.Grid != null)
            {
                AppCore.Instance.Events.Grid.OnCropPassiveScore -= HandlePassiveScore;
            }
        }

        private void HandlePassiveScore(int slotIndex, int points, int newTotal, int goal)
        {
            // Busca o GridSlotView na cena (poderíamos otimizar com cache, mas para fins visuais funciona)
            var slots = FindObjectsByType<GridSlotView>(FindObjectsSortMode.None);
            foreach (var slot in slots)
            {
                if (slot.SlotIndex == slotIndex)
                {
                    AnimatePassiveScore(slot, points).Forget();
                    break;
                }
            }
        }

        private async UniTaskVoid AnimatePassiveScore(GridSlotView slot, int points)
        {
            if (slot == null) return;

            // REFATORADO: ShowPassiveScore agora é a única fonte de verdade
            // para elevação, flash e timing. O handler apenas delega.
            slot.ShowPassiveScore(points);
        }
    }
}
