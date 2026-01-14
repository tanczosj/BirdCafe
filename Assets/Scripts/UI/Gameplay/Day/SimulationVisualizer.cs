using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using System.Linq;

namespace BirdCafe.UI.Gameplay.Day
{
    public class SimulationVisualizer : MonoBehaviour
    {
        [Header("References")]
        public DayProgressUI dayProgress;
        public StatCounterUI moneyCounter;
        public StatCounterUI popularityCounter;

        [Header("Actors")]
        [Tooltip("The transform where bubbles spawn (e.g. the bird's head)")]
        public RectTransform birdAnchor;
        public GameObject thoughtBubblePrefab;

        [Header("Settings")]
        public float dayDuration = 120f; // Must match engine config

        // Internal State
        private float _currentTimer;
        private List<UiTimelineEvent> _timeline;
        private int _eventIndex;

        // Tracking running totals for animation
        private float _currentMoney;
        private float _currentPopularity;

        private void OnEnable()
        {
            // 1. Initialize State
            _currentTimer = 0f;
            _eventIndex = 0;

            // Get initial values
            var intro = BirdCafeGame.Instance.GetDayIntro(); // Gets current snapshot
            _currentMoney = (float)BirdCafeGame.Instance.GetCareDashboard().CurrentMoney; // Hack: grab money from dashboard data
            _currentPopularity = intro.Popularity;

            // 2. Fetch Timeline
            _timeline = BirdCafeGame.Instance.GetDayTimeline();

            var deltaMoney = _timeline.Sum(x => x.MoneyDelta);
            _currentMoney -= (float)deltaMoney; // Rewind money to start of day

            // Initialize UI
            moneyCounter.Value = _currentMoney;
            popularityCounter.Value = _currentPopularity;
            dayProgress.UpdateVisuals(0);

            // 3. Start Loop
            StartCoroutine(RunSimulationRoutine());
        }

        private IEnumerator RunSimulationRoutine()
        {
            while (_currentTimer < dayDuration)
            {
                _currentTimer += Time.deltaTime * 1.5f;

                // A. Update Progress Bar
                dayProgress.UpdateVisuals(_currentTimer);

                // B. Process Events passed since last frame
                ProcessEvents();

                yield return null;
            }

            // Ensure we hit 100%
            dayProgress.UpdateVisuals(dayDuration);

            // Wait a moment for last animations
            yield return new WaitForSeconds(2.0f);

            // Finish
            BirdCafeGame.Instance.FinishSimulation();
        }

        private void ProcessEvents()
        {
            if (_timeline == null) return;

            // Keep consuming events until the next event is in the future
            while (_eventIndex < _timeline.Count &&
                   _timeline[_eventIndex].TimeSeconds <= _currentTimer)
            {
                var evt = _timeline[_eventIndex];
                PlayEvent(evt);
                _eventIndex++;
            }
        }

        private void PlayEvent(UiTimelineEvent evt)
        {
            Debug.Log($"Processing Event: {evt.EventType} | Money: {evt.MoneyDelta}");

            // 1. Update Stats (Money/Pop)
            if (evt.MoneyDelta != 0)
            {
                _currentMoney += (float)evt.MoneyDelta;
                moneyCounter.AnimateValue(_currentMoney, 0.5f);
            }

            if (evt.PopularityDelta != 0)
            {
                _currentPopularity += evt.PopularityDelta;
                popularityCounter.AnimateValue(_currentPopularity, 0.5f);
            }

            // 2. Visual Feedback (Bubbles)
            // We only show bubbles for specific interesting events
            if (evt.EventType == "ServiceCompleted")
            {
                SpawnBubble($"Sold {evt.IconId}!");
            }
            else if (evt.EventType == "ServiceFailed")
            {
                // Optional: Show angry text
                SpawnBubble("Angry Customer!");
            }
        }

        private void SpawnBubble(string text)
        {
            if (thoughtBubblePrefab && birdAnchor)
            {
                // 1. Instantiate as CHILD of the bird anchor
                var go = Instantiate(thoughtBubblePrefab, birdAnchor);

                // 2. Reset Position to 0,0 relative to the anchor
                var rect = go.GetComponent<RectTransform>();
                rect.anchoredPosition = Vector2.zero;
                rect.localScale = Vector3.one; // new Vector3(0.5f, 0.5f, 1f);

                // 3. Init
                var script = go.GetComponent<ThoughtBubble>();
                if (script) script.Initialize(text);
            }
        }
    }
}