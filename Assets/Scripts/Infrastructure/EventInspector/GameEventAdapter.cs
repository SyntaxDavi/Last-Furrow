using System;
using System.Collections.Generic;
using UnityEngine;

namespace LastFurrow.EventInspector
{
    /// <summary>
    /// Adapter that connects to the GameEvents system.
    /// Must be initialized with a GameEvents instance from your AppCore.
    /// 
    /// USAGE: In your AppCore or Bootstrapper, after creating GameEvents:
    ///   FindObjectOfType<GameEventAdapter>()?.Initialize(gameEvents);
    /// </summary>
    public class GameEventAdapter : MonoBehaviour
    {
        private GameEvents _events;
        private bool _subscribed = false;

        /// <summary>
        /// Initialize with your GameEvents instance.
        /// </summary>
        public void Initialize(GameEvents events)
        {
            if (_subscribed) Unsubscribe();
            
            _events = events;
            Subscribe();
            Debug.Log("[GameEventAdapter] Initialized and subscribed to GameEvents");
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (_events == null) return;

            // GameState
            _events.GameState.OnStateChanged += HandleStateChanged;

            // Grid
            _events.Grid.OnSlotUpdated += HandleSlotUpdated;
            _events.Grid.OnCardDropped += HandleCardDropped;
            _events.Grid.OnCropPassiveScore += HandleCropPassiveScore;

            // Player
            _events.Player.OnCardAdded += HandleCardAdded;
            _events.Player.OnCardRemoved += HandleCardRemoved;
            _events.Player.OnCardConsumed += HandleCardConsumed;
            _events.Player.OnCardOverflow += HandleCardOverflow;

            // Time
            _events.Time.OnDayChanged += HandleDayChanged;
            _events.Time.OnWeekChanged += HandleWeekChanged;
            _events.Time.OnRunStarted += HandleRunStarted;
            _events.Time.OnRunEnded += HandleRunEnded;
            _events.Time.OnResolutionStarted += HandleResolutionStarted;
            _events.Time.OnResolutionEnded += HandleResolutionEnded;

            // Progression
            _events.Progression.OnScoreUpdated += HandleScoreUpdated;
            _events.Progression.OnLivesChanged += HandleLivesChanged;
            _events.Progression.OnWeeklyGoalEvaluated += HandleWeeklyGoalEvaluated;

            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_events == null || !_subscribed) return;

            _events.GameState.OnStateChanged -= HandleStateChanged;
            _events.Grid.OnSlotUpdated -= HandleSlotUpdated;
            _events.Grid.OnCardDropped -= HandleCardDropped;
            _events.Grid.OnCropPassiveScore -= HandleCropPassiveScore;
            _events.Player.OnCardAdded -= HandleCardAdded;
            _events.Player.OnCardRemoved -= HandleCardRemoved;
            _events.Player.OnCardConsumed -= HandleCardConsumed;
            _events.Player.OnCardOverflow -= HandleCardOverflow;
            _events.Time.OnDayChanged -= HandleDayChanged;
            _events.Time.OnWeekChanged -= HandleWeekChanged;
            _events.Time.OnRunStarted -= HandleRunStarted;
            _events.Time.OnRunEnded -= HandleRunEnded;
            _events.Time.OnResolutionStarted -= HandleResolutionStarted;
            _events.Time.OnResolutionEnded -= HandleResolutionEnded;
            _events.Progression.OnScoreUpdated -= HandleScoreUpdated;
            _events.Progression.OnLivesChanged -= HandleLivesChanged;
            _events.Progression.OnWeeklyGoalEvaluated -= HandleWeeklyGoalEvaluated;

            _subscribed = false;
        }

        // =====================================================================
        // HANDLERS
        // =====================================================================

        private void HandleStateChanged(GameState state)
        {
            EventLogger.Instance.LogCustom("GAME_STATE_CHANGED", new Dictionary<string, object>
            {
                { "state", state.ToString() }
            }, "GameState");
        }

        private void HandleSlotUpdated(int slotIndex)
        {
            EventLogger.Instance.LogCustom("SLOT_UPDATED", new Dictionary<string, object>
            {
                { "slotIndex", slotIndex }
            }, "Grid");
        }

        private void HandleCardDropped(int slotIndex, CardData cardData)
        {
            var payload = new Dictionary<string, object>
            {
                { "slotIndex", slotIndex }
            };

            if (cardData != null)
            {
                payload["cardId"] = cardData.ID.ToString();
                payload["cardName"] = cardData.Name;
            }

            EventLogger.Instance.LogCustom("CARD_DROPPED", payload, "Grid");
        }

        private void HandleCropPassiveScore(int slotIndex, int cropPoints, int newTotal, int goal)
        {
            EventLogger.Instance.LogCustom("CROP_PASSIVE_SCORE", new Dictionary<string, object>
            {
                { "slotIndex", slotIndex },
                { "cropPoints", cropPoints },
                { "newTotal", newTotal },
                { "goal", goal }
            }, "Grid");
        }

        // CardInstance is a struct, so we don't use null checks with ?
        private void HandleCardAdded(CardInstance card)
        {
            EventLogger.Instance.LogCustom("CARD_ADDED", new Dictionary<string, object>
            {
                { "uniqueId", card.UniqueID },
                { "templateId", card.TemplateID.ToString() }
            }, "Player");
        }

        private void HandleCardRemoved(CardInstance card)
        {
            EventLogger.Instance.LogCustom("CARD_REMOVED", new Dictionary<string, object>
            {
                { "uniqueId", card.UniqueID },
                { "templateId", card.TemplateID.ToString() }
            }, "Player");
        }

        private void HandleCardConsumed(CardID cardId)
        {
            EventLogger.Instance.LogCustom("CARD_CONSUMED", new Dictionary<string, object>
            {
                { "cardId", cardId.ToString() }
            }, "Player");
        }

        private void HandleCardOverflow(CardID cardId, int amount)
        {
            EventLogger.Instance.LogCustom("CARD_OVERFLOW", new Dictionary<string, object>
            {
                { "cardId", cardId.ToString() },
                { "amount", amount }
            }, "Player");
        }

        private void HandleDayChanged(int day)
        {
            EventLogger.Instance.LogPropertyChange("time.day", day - 1, day, "Day changed", "Time");
        }

        private void HandleWeekChanged(int week)
        {
            EventLogger.Instance.LogPropertyChange("time.week", week - 1, week, "Week changed", "Time");
        }

        private void HandleRunStarted()
        {
            EventLogger.Instance.LogCustom("RUN_STARTED", new Dictionary<string, object>(), "Time");
        }

        private void HandleRunEnded(RunEndReason reason)
        {
            EventLogger.Instance.LogCustom("RUN_ENDED", new Dictionary<string, object>
            {
                { "reason", reason.ToString() }
            }, "Time");
        }

        private void HandleResolutionStarted()
        {
            EventLogger.Instance.LogCustom("RESOLUTION_STARTED", new Dictionary<string, object>(), "Time");
        }

        private void HandleResolutionEnded()
        {
            EventLogger.Instance.LogCustom("RESOLUTION_ENDED", new Dictionary<string, object>(), "Time");
        }

        private void HandleScoreUpdated(int current, int target)
        {
            EventLogger.Instance.LogPropertyChange("progression.score", 0, current, "Score: " + current + "/" + target, "Progression");
        }

        private void HandleLivesChanged(int lives)
        {
            EventLogger.Instance.LogPropertyChange("progression.lives", 0, lives, "Lives changed", "Progression");
        }

        private void HandleWeeklyGoalEvaluated(bool success, int lives)
        {
            EventLogger.Instance.LogCustom("WEEKLY_GOAL_EVALUATED", new Dictionary<string, object>
            {
                { "success", success },
                { "livesRemaining", lives }
            }, "Progression");
        }
    }
}
