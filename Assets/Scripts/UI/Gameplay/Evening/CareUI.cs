using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BirdCafe.UI.Gameplay.Evening
{
    public class CareUI : MonoBehaviour
    {
        [Header("Global Stats")]
        [SerializeField] private StatCounterUI moneyCounter;
        [SerializeField] private StatCounterUI popularityCounter;

        [Header("Bird Display")]
        [Tooltip("The container for the bird's stats and info.")]
        [SerializeField] private BirdCareCard birdCard;
        [SerializeField] private BirdProfileCardUI birdProfileCardPrefab;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private GameObject birdSelectGrid;
        [SerializeField] private GameObject careOptionsPanel;

        [Header("Actions")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button vetButton;
        [SerializeField] private Toggle restToggle;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button switchBirdButton;

        [Header("Animation")]
        [SerializeField] private float cardMoveDuration = 0.22f;
        [SerializeField] private float drawerDuration = 0.24f;
        [SerializeField] private float cardFlipDuration = 0.12f;
        [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve flipEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static readonly Vector2 BirdCardDrawerClosedPosition = new Vector2(-592f, -587f);
        private static readonly Vector2 BirdCardDrawerOpenPosition = new Vector2(0f, -768f);
        private static readonly Vector3 BirdCardDrawerClosedScale = new Vector3(1f, 1f, 1f);
        private static readonly Vector3 BirdCardDrawerOpenScale = new Vector3(0.7f, 0.7f, 1f);
        private static readonly Quaternion BirdCardFrontRotation = Quaternion.Euler(0f, 0f, 0f);
        private static readonly Quaternion BirdCardSideRotation = Quaternion.Euler(0f, 90f, 0f);

        private const float DrawerHiddenPadding = 100f;

        private string _currentBirdId;
        private Coroutine _layoutCoroutine;
        private Coroutine _flipCoroutine;
        private RectTransform _birdCardRect;
        private RectTransform _birdSelectGridRect;
        private bool _isDrawerVisible;
        private bool _isFlippingCard;

        private void Awake()
        {
            _birdCardRect = birdCard != null ? birdCard.GetComponent<RectTransform>() : null;
            _birdSelectGridRect = birdSelectGrid != null ? birdSelectGrid.GetComponent<RectTransform>() : null;
        }

        private void OnEnable()
        {
            if (restToggle != null)
            {
                restToggle.onValueChanged.RemoveAllListeners();
                restToggle.onValueChanged.AddListener(OnRestToggled);
            }

            _isDrawerVisible = true;

            Refresh();
            ApplyVisualStateImmediate(true);
        }

        private void Refresh()
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null)
                return;

            if (moneyCounter) moneyCounter.AnimateValue((float)dashboard.CurrentMoney, 0.5f);
            if (popularityCounter) popularityCounter.AnimateValue((float)dashboard.CurrentPopularity, 0.5f);

            if (dashboard.Birds == null || dashboard.Birds.Count == 0)
                return;

            ShowBirds(dashboard.Birds);

            var selectedBird = FindBirdById(dashboard.Birds, _currentBirdId);

            if (selectedBird == null)
            {
                selectedBird = dashboard.Birds[0];
                _currentBirdId = selectedBird.Id;
            }

            LoadBirdIntoCard(selectedBird, dashboard.CurrentMoney);
        }

        private BirdCareViewModel FindBirdById(List<BirdCareViewModel> birds, string birdId)
        {
            if (birds == null || string.IsNullOrEmpty(birdId))
                return null;

            foreach (var bird in birds)
            {
                if (bird.Id == birdId)
                    return bird;
            }

            return null;
        }

        private void LoadBirdIntoCard(BirdCareViewModel bird, decimal currentMoney)
        {
            if (bird == null)
                return;

            _currentBirdId = bird.Id;

            if (birdCard)
                birdCard.ViewModel = bird;

            if (restToggle)
                restToggle.SetIsOnWithoutNotify(bird.WillRestTomorrow);

            UpdateButtons(currentMoney, bird);
        }

        public void ShowBirds(List<BirdCareViewModel> birds)
        {
            foreach (Transform child in cardContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var bird in birds)
            {
                var card = Instantiate(birdProfileCardPrefab, cardContainer);
                card.Initialize(bird);

                RegisterCardInteractions(card, bird);
            }
        }

        private void RegisterCardInteractions(BirdProfileCardUI card, BirdCareViewModel bird)
        {
            if (card == null || bird == null)
                return;

            var button = card.GetComponent<Button>();
            if (button == null)
                button = card.GetComponentInChildren<Button>(true);

            if (button != null)
            {
                string clickedBirdId = bird.Id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnBirdSelected(clickedBirdId));
            }

            var triggerHost = button != null ? button.gameObject : card.gameObject;
            var trigger = triggerHost.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = triggerHost.AddComponent<EventTrigger>();

            if (trigger.triggers == null)
                trigger.triggers = new List<EventTrigger.Entry>();
            else
                trigger.triggers.Clear();

            AddEventTrigger(trigger, EventTriggerType.PointerEnter, () => OnBirdHovered(bird.Id));
        }

        private void AddEventTrigger(EventTrigger trigger, EventTriggerType eventType, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = eventType };
            entry.callback.AddListener(_ => action.Invoke());
            trigger.triggers.Add(entry);
        }

        public void OnBirdHovered(string birdId)
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null || dashboard.Birds == null || dashboard.Birds.Count == 0)
                return;

            var hoveredBird = FindBirdById(dashboard.Birds, birdId);
            if (hoveredBird == null)
                return;

            if (_isFlippingCard)
                return;

            if (_currentBirdId == hoveredBird.Id)
            {
                LoadBirdIntoCard(hoveredBird, dashboard.CurrentMoney);
                return;
            }

            if (_flipCoroutine != null)
                StopCoroutine(_flipCoroutine);

            _flipCoroutine = StartCoroutine(FlipToBirdRoutine(hoveredBird, dashboard.CurrentMoney));
        }

        public void OnBirdSelected(string birdId)
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null || dashboard.Birds == null || dashboard.Birds.Count == 0)
                return;

            var selectedBird = FindBirdById(dashboard.Birds, birdId);
            if (selectedBird == null)
                return;

            _currentBirdId = selectedBird.Id;
            LoadBirdIntoCard(selectedBird, dashboard.CurrentMoney);
            SetBirdSelectGridVisible(false);
        }

        public void ToggleBirdSelectGrid()
        {
            SetBirdSelectGridVisible(!_isDrawerVisible);
        }

        public void ShowBirdSelectGrid()
        {
            SetBirdSelectGridVisible(true);
        }

        public void OnSwitchBirdClicked()
        {
            SetBirdSelectGridVisible(true);
        }

        public void SetBirdSelectGridVisible(bool isVisible)
        {
            _isDrawerVisible = isVisible;

            if (_layoutCoroutine != null)
                StopCoroutine(_layoutCoroutine);

            _layoutCoroutine = StartCoroutine(AnimateDrawerAndBirdCardRoutine(isVisible));
        }

        private IEnumerator FlipToBirdRoutine(BirdCareViewModel targetBird, decimal currentMoney)
        {
            if (_birdCardRect == null)
            {
                LoadBirdIntoCard(targetBird, currentMoney);
                yield break;
            }

            _isFlippingCard = true;

            yield return TweenRotation(_birdCardRect, _birdCardRect.localRotation, BirdCardSideRotation, cardFlipDuration, flipEase);

            LoadBirdIntoCard(targetBird, currentMoney);

            yield return TweenRotation(_birdCardRect, _birdCardRect.localRotation, BirdCardFrontRotation, cardFlipDuration, flipEase);

            _isFlippingCard = false;
            _flipCoroutine = null;
        }

        private IEnumerator AnimateDrawerAndBirdCardRoutine(bool showDrawer)
        {
            if (_birdCardRect == null)
            {
                ApplyVisualStateImmediate(showDrawer);
                yield break;
            }

            if (_flipCoroutine != null)
            {
                StopCoroutine(_flipCoroutine);
                _flipCoroutine = null;
                _isFlippingCard = false;
            }

            if (careOptionsPanel != null)
                careOptionsPanel.SetActive(false);

            if (_birdSelectGridRect == null && birdSelectGrid != null)
                _birdSelectGridRect = birdSelectGrid.GetComponent<RectTransform>();

            Vector2 gridOpenPosition = Vector2.zero;
            Vector2 gridClosedPosition = GetBirdSelectGridHiddenPosition();

            if (showDrawer)
            {
                if (birdSelectGrid != null && !birdSelectGrid.activeSelf)
                    birdSelectGrid.SetActive(true);

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = gridClosedPosition;
            }

            Vector2 birdStartPosition = _birdCardRect.anchoredPosition;
            Vector3 birdStartScale = _birdCardRect.localScale;
            Quaternion birdStartRotation = _birdCardRect.localRotation;

            Vector2 birdTargetPosition = showDrawer ? BirdCardDrawerOpenPosition : BirdCardDrawerClosedPosition;
            Vector3 birdTargetScale = showDrawer ? BirdCardDrawerOpenScale : BirdCardDrawerClosedScale;

            Vector2 gridStartPosition = _birdSelectGridRect != null ? _birdSelectGridRect.anchoredPosition : gridClosedPosition;
            Vector2 gridTargetPosition = showDrawer ? gridOpenPosition : gridClosedPosition;

            float duration = Mathf.Max(0.0001f, Mathf.Max(cardMoveDuration, drawerDuration));
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);

                float cardT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, cardMoveDuration));
                float cardEased = moveEase != null ? moveEase.Evaluate(cardT) : cardT;

                _birdCardRect.anchoredPosition = Vector2.LerpUnclamped(birdStartPosition, birdTargetPosition, cardEased);
                _birdCardRect.localScale = Vector3.LerpUnclamped(birdStartScale, birdTargetScale, cardEased);
                _birdCardRect.localRotation = Quaternion.SlerpUnclamped(birdStartRotation, BirdCardFrontRotation, cardEased);

                if (_birdSelectGridRect != null)
                {
                    float drawerT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, drawerDuration));
                    float drawerEased = moveEase != null ? moveEase.Evaluate(drawerT) : drawerT;
                    _birdSelectGridRect.anchoredPosition = Vector2.LerpUnclamped(gridStartPosition, gridTargetPosition, drawerEased);
                }

                yield return null;
            }

            if (showDrawer)
            {
                ForceBirdCardOpenState();

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = gridOpenPosition;

                if (switchBirdButton != null)
                    switchBirdButton.gameObject.SetActive(false);
            }
            else
            {
                ForceBirdCardClosedState();

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = gridClosedPosition;

                if (birdSelectGrid != null)
                    birdSelectGrid.SetActive(false);

                if (careOptionsPanel != null)
                    careOptionsPanel.SetActive(true);

                if (switchBirdButton != null)
                    switchBirdButton.gameObject.SetActive(true);
            }

            _layoutCoroutine = null;
        }

        private Vector2 GetBirdSelectGridHiddenPosition()
        {
            if (_birdSelectGridRect == null)
                return new Vector2(0f, 700f);

            float hiddenY = _birdSelectGridRect.rect.height + DrawerHiddenPadding;
            return new Vector2(0f, hiddenY);
        }

        private void ApplyVisualStateImmediate(bool showDrawer)
        {
            if (_birdCardRect != null)
            {
                _birdCardRect.anchoredPosition = showDrawer ? BirdCardDrawerOpenPosition : BirdCardDrawerClosedPosition;
                _birdCardRect.localScale = showDrawer ? BirdCardDrawerOpenScale : BirdCardDrawerClosedScale;
                _birdCardRect.localRotation = BirdCardFrontRotation;
            }

            if (_birdSelectGridRect == null && birdSelectGrid != null)
                _birdSelectGridRect = birdSelectGrid.GetComponent<RectTransform>();

            if (birdSelectGrid != null)
            {
                birdSelectGrid.SetActive(showDrawer);

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = showDrawer ? Vector2.zero : GetBirdSelectGridHiddenPosition();
            }

            if (careOptionsPanel != null)
                careOptionsPanel.SetActive(!showDrawer);

            if (switchBirdButton != null)
                switchBirdButton.gameObject.SetActive(!showDrawer);
        }

        private void ForceBirdCardClosedState()
        {
            if (_birdCardRect == null)
                return;

            _birdCardRect.anchoredPosition = BirdCardDrawerClosedPosition;
            _birdCardRect.localScale = BirdCardDrawerClosedScale;
            _birdCardRect.localRotation = BirdCardFrontRotation;
        }

        private void ForceBirdCardOpenState()
        {
            if (_birdCardRect == null)
                return;

            _birdCardRect.anchoredPosition = BirdCardDrawerOpenPosition;
            _birdCardRect.localScale = BirdCardDrawerOpenScale;
            _birdCardRect.localRotation = BirdCardFrontRotation;
        }

        private IEnumerator TweenRectTransform(
            RectTransform rectTransform,
            Vector2 fromPosition,
            Vector2 toPosition,
            Vector3 fromScale,
            Vector3 toScale,
            Quaternion fromRotation,
            Quaternion toRotation,
            float duration,
            AnimationCurve ease)
        {
            float elapsed = 0f;
            duration = Mathf.Max(0.0001f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = ease != null ? ease.Evaluate(t) : t;

                rectTransform.anchoredPosition = Vector2.LerpUnclamped(fromPosition, toPosition, eased);
                rectTransform.localScale = Vector3.LerpUnclamped(fromScale, toScale, eased);
                rectTransform.localRotation = Quaternion.SlerpUnclamped(fromRotation, toRotation, eased);

                yield return null;
            }

            rectTransform.anchoredPosition = toPosition;
            rectTransform.localScale = toScale;
            rectTransform.localRotation = toRotation;
        }

        private IEnumerator TweenRotation(RectTransform rectTransform, Quaternion from, Quaternion to, float duration, AnimationCurve ease)
        {
            float elapsed = 0f;
            duration = Mathf.Max(0.0001f, duration);

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = ease != null ? ease.Evaluate(t) : t;

                rectTransform.localRotation = Quaternion.SlerpUnclamped(from, to, eased);
                yield return null;
            }

            rectTransform.localRotation = to;
        }

        private void UpdateButtons(decimal currentMoney, BirdCareViewModel bird)
        {
            if (feedButton) feedButton.interactable = currentMoney >= 5.0m;
            if (playButton) playButton.interactable = true;
            if (vetButton) vetButton.interactable = currentMoney >= 50.0m;
        }

        public void OnFeedClicked()
        {
            AttemptAction("Feed");
        }

        public void OnPlayClicked()
        {
            AttemptAction("Play");
        }

        public void OnVetClicked()
        {
            AttemptAction("Vet");
        }

        private void AttemptAction(string actionId)
        {
            if (string.IsNullOrEmpty(_currentBirdId))
                return;

            bool success = BirdCafeGame.Instance.PerformCare(_currentBirdId, actionId);

            if (success)
                Refresh();
        }

        public void OnRestToggled(bool isOn)
        {
            if (string.IsNullOrEmpty(_currentBirdId))
                return;

            bool success = BirdCafeGame.Instance.ToggleRest(_currentBirdId);

            if (!success)
            {
                if (restToggle)
                    restToggle.SetIsOnWithoutNotify(!isOn);
            }
            else
            {
                Refresh();
            }
        }

        public void OnContinueClicked()
        {
            BirdCafeGame.Instance.GoToHub();
        }
    }
}
