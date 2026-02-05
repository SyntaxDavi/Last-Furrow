using System;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace LastFurrow.UI.GameOver
{
    /// <summary>
    /// Professional Presenter for the Game Over Screen.
    /// 
    /// SOLID (SRP): Apenas apresenta o Game Over e dispara intenção de retorno.
    /// SOLID (DIP): Depende de abstrações (IGameStateProvider) não implementações.
    /// 
    /// ROBUSTEZ:
    /// - Auto-retorno com fallback direto caso evento não seja escutado.
    /// - Múltiplas tentativas de retorno se necessário.
    /// - Logs detalhados para debug.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameOverView _view;

        [Header("Auto Return Settings")]
        [SerializeField] private float _autoReturnDelaySeconds = 7f;

        public event Action OnMainMenuRequested;

        private IGameStateProvider _stateProvider;
        private TimeEvents _timeEvents;
        private RunEndReason _lastReason = RunEndReason.Abandoned;
        private System.Threading.CancellationTokenSource _autoReturnCts;
        private bool _isReturning = false;

        public void Initialize(IGameStateProvider stateProvider, TimeEvents timeEvents)
        {
            _stateProvider = stateProvider;
            _timeEvents = timeEvents;

            if (_view == null)
            {
                Debug.LogError($"[GameOverController] View is missing on {gameObject.name}");
                enabled = false;
                return;
            }

            // Garante cleanup antes de subscrever
            if (_timeEvents != null)
            {
                _timeEvents.OnRunEnded -= HandleRunEnded;
                _timeEvents.OnRunEnded += HandleRunEnded;
            }

            if (_stateProvider != null)
            {
                _stateProvider.OnStateChanged -= HandleStateChanged;
                _stateProvider.OnStateChanged += HandleStateChanged;
            }

            Debug.Log("[GameOverController] ✓ Inicializado");
        }

        private void OnEnable()
        {
            if (_view != null)
            {
                _view.OnReturnToMenuRequested += HandleReturnRequested;
            }
        }

        private void OnDisable()
        {
            CancelAutoReturn();
            
            if (_view != null)
            {
                _view.OnReturnToMenuRequested -= HandleReturnRequested;
            }

            if (_stateProvider != null)
                _stateProvider.OnStateChanged -= HandleStateChanged;
        }

        private void OnDestroy()
        {
            CancelAutoReturn();
            
            if (_stateProvider != null)
                _stateProvider.OnStateChanged -= HandleStateChanged;

            if (_timeEvents != null)
                _timeEvents.OnRunEnded -= HandleRunEnded;
        }

        private void HandleRunEnded(RunEndReason reason)
        {
            Debug.Log($"[GameOverController] Run ended: {reason}");
            _lastReason = reason;
        }

        private void HandleStateChanged(GameState newState)
        {
            Debug.Log($"[GameOverController] State changed to: {newState}");
            
            if (newState == GameState.GameOver)
            {
                _isReturning = false;
                _view.Setup(_lastReason);
                _view.Show();
                
                // Inicia auto-retorno ao menu
                StartAutoReturnSequence().Forget();
            }
            else if (_view.gameObject.activeSelf)
            {
                CancelAutoReturn();
                _view.Hide();
            }
        }

        /// <summary>
        /// Sequência robusta de auto-retorno com fallback direto.
        /// </summary>
        private async UniTaskVoid StartAutoReturnSequence()
        {
            CancelAutoReturn();
            _autoReturnCts = new System.Threading.CancellationTokenSource();
            
            try
            {
                Debug.Log($"[GameOverController] Auto-retorno ao menu em {_autoReturnDelaySeconds}s...");
                
                // Usa realtime para funcionar mesmo com Time.timeScale = 0
                await UniTask.Delay(
                    TimeSpan.FromSeconds(_autoReturnDelaySeconds), 
                    DelayType.Realtime, 
                    cancellationToken: _autoReturnCts.Token
                );
                
                // Tempo esgotou, força retorno ao menu
                Debug.Log("[GameOverController] Tempo esgotado - Executando retorno ao menu");
                ExecuteReturnToMenu();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[GameOverController] Auto-retorno cancelado (usuário interagiu ou cena mudou)");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[GameOverController] Erro no auto-retorno: {ex.Message}");
                // Fallback: tenta retornar mesmo assim
                ExecuteReturnToMenu();
            }
        }

        private void CancelAutoReturn()
        {
            _autoReturnCts?.Cancel();
            _autoReturnCts?.Dispose();
            _autoReturnCts = null;
        }

        private void HandleReturnRequested()
        {
            Debug.Log("[GameOverController] Retorno ao menu solicitado pelo usuário");
            CancelAutoReturn();
            ExecuteReturnToMenu();
        }

        /// <summary>
        /// Executa o retorno ao menu de forma robusta.
        /// Primeiro tenta via evento, depois via fallback direto.
        /// </summary>
        private void ExecuteReturnToMenu()
        {
            if (_isReturning)
            {
                Debug.LogWarning("[GameOverController] Já está retornando ao menu, ignorando chamada duplicada");
                return;
            }
            
            _isReturning = true;
            CancelAutoReturn();

            // Tenta via evento primeiro (para manter desacoplamento)
            bool hasListeners = OnMainMenuRequested != null;
            
            if (hasListeners)
            {
                Debug.Log("[GameOverController] Disparando evento OnMainMenuRequested");
                OnMainMenuRequested.Invoke();
            }
            else
            {
                // FALLBACK DIRETO: Se ninguém está escutando, faz diretamente
                Debug.LogWarning("[GameOverController] Nenhum listener para OnMainMenuRequested - Usando fallback direto");
                ExecuteDirectReturn();
            }
        }

        /// <summary>
        /// Fallback: Retorna ao menu diretamente via AppCore.
        /// Usado quando nenhum FlowCoordinator está escutando o evento.
        /// </summary>
        private void ExecuteDirectReturn()
        {
            if (AppCore.Instance != null)
            {
                Debug.Log("[GameOverController] Executando ReturnToMainMenu via AppCore");
                AppCore.Instance.ReturnToMainMenu();
            }
            else
            {
                Debug.LogError("[GameOverController] AppCore.Instance é null! Não foi possível retornar ao menu.");
                // Último fallback: tenta carregar cena diretamente
                try
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[GameOverController] Fallback final falhou: {ex.Message}");
                }
            }
        }
    }
}
