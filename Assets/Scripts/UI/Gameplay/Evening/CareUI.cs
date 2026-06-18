using BirdCafe.Shared;
using BirdCafe.Shared.Enums;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using Ricimi;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
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

        [Header("Inventory")]
        [SerializeField] private GameObject inventoryStats;
        [SerializeField] private TMP_Text seedmixLabel;
        [SerializeField] private TMP_Text fruitMedleyLabel;
        [SerializeField] private TMP_Text nutriPelletsLabel;

        [Header("Actions")]
        [SerializeField] private Button feedButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button vetButton;
        [SerializeField] private Toggle restToggle;

        [Header("Navigation")]
        [SerializeField] private Button continueButton;
        [SerializeField] private Button switchBirdButton;

        [Header("Customization")]
        [SerializeField] private CustomizeBirdPopupUI customizeBirdPopup;

        [Header("Animation")]
        [SerializeField] private float cardMoveDuration = 0.22f;
        [SerializeField] private float drawerDuration = 0.24f;
        [SerializeField] private float careOptionsDuration = 0.14f;
        [SerializeField] private float switchBirdDuration = 0.14f;
        [SerializeField] private float careOptionsHiddenPadding = 120f;
        [SerializeField] private float switchBirdHiddenPadding = 120f;
        [SerializeField] private float movementOvershoot = 1.0f;
        [SerializeField] private float cardFlipDuration = 0.12f;
        [SerializeField] private AnimationCurve moveEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve flipEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        public string SelectedBirdId => _currentBirdId;

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
        private RectTransform _careOptionsRect;
        private RectTransform _switchBirdButtonRect;
        private RectTransform _rootCanvasRect;
        private Vector2 _careOptionsVisiblePosition;
        private Vector2 _switchBirdButtonVisiblePosition;
        private bool _isDrawerVisible;
        private bool _isFlippingCard;

        private void Awake()
        {
            _birdCardRect = birdCard != null ? birdCard.GetComponent<RectTransform>() : null;
            _birdSelectGridRect = birdSelectGrid != null ? birdSelectGrid.GetComponent<RectTransform>() : null;
            _careOptionsRect = careOptionsPanel != null ? careOptionsPanel.GetComponent<RectTransform>() : null;
            _switchBirdButtonRect = switchBirdButton != null ? switchBirdButton.GetComponent<RectTransform>() : null;

            Canvas rootCanvas = GetComponentInParent<Canvas>();
            if (rootCanvas != null && rootCanvas.rootCanvas != null)
                _rootCanvasRect = rootCanvas.rootCanvas.GetComponent<RectTransform>();

            CacheCareOptionsVisiblePosition();
            CacheSwitchBirdButtonVisiblePosition();

            if (customizeBirdPopup != null)
                customizeBirdPopup.SetCareUI(this);
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

            if (customizeBirdPopup != null)
                customizeBirdPopup.SetCareUI(this);
        }

        public BirdCareViewModel GetSelectedBird()
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null || dashboard.Birds == null || dashboard.Birds.Count == 0)
                return null;

            var selectedBird = FindBirdById(dashboard.Birds, _currentBirdId);
            return selectedBird ?? dashboard.Birds[0];
        }

        public void RefreshAfterBirdCustomization()
        {
            Refresh();
        }

        public void OnCustomizeBirdClicked()
        {
            if (customizeBirdPopup == null)
                return;

            customizeBirdPopup.OpenForSelectedBird(this);
        }

        private void Update()
        {
            if (Keyboard.current == null)
                return;

            bool ctrlPressed =
                Keyboard.current.leftCtrlKey.isPressed ||
                Keyboard.current.rightCtrlKey.isPressed;

            bool shiftPressed =
                Keyboard.current.leftShiftKey.isPressed ||
                Keyboard.current.rightShiftKey.isPressed;

            if (ctrlPressed && shiftPressed && Keyboard.current.sKey.wasPressedThisFrame)
            {
                MakeBirdSick();
            }
        }

        private void Refresh()
        {
            var dashboard = BirdCafeGame.Instance.GetCareDashboard();
            if (dashboard == null)
                return;

            if (moneyCounter) moneyCounter.AnimateValue((float)dashboard.CurrentMoney, 0.5f);
            if (popularityCounter) popularityCounter.AnimateValue((float)dashboard.CurrentPopularity, 0.5f);

            inventoryStats.SetActive(!_isDrawerVisible);

            var seedMixUnits = BirdCafeGame.Instance.Controller.CurrentState.PetStore.GetFoodUnits(BirdFoodType.SeedMix).ToString();
            var fruitMedleyUnits = BirdCafeGame.Instance.Controller.CurrentState.PetStore.GetFoodUnits(BirdFoodType.FruitMedley).ToString();
            var nutriPelletsUnits = BirdCafeGame.Instance.Controller.CurrentState.PetStore.GetFoodUnits(BirdFoodType.NutriPellets).ToString();

            var seedMixText = $"Seed Mix (x{seedMixUnits})";
            var fruitMedleyText = $"Fruit Medley (x{fruitMedleyUnits})";
            var nutriPelletsText = $"Nutri Pellets (x{nutriPelletsUnits})";

            if (seedmixLabel != null) 
                seedmixLabel.text = seedMixText;

            if (fruitMedleyLabel != null) 
                fruitMedleyLabel.text = fruitMedleyText;

            if (nutriPelletsLabel != null)
                nutriPelletsLabel.text = nutriPelletsText;

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

        private void MakeBirdSick()
        {
            Debug.Log("MakeBirdSick() called.");

            if (SelectedBirdId == null)
            {
                Debug.LogWarning("No bird is currently selected to make sick.");
                return;
            }

            var bird = BirdCafeGame.Instance.Controller.CurrentState.Birds.FirstOrDefault(b => b.Id == SelectedBirdId);
            bird.IsSick = true;
            bird.Hunger = 45;
            bird.Energy = 10;
            bird.Health = 10;

            BirdCafeGame.Instance.AdvanceBirdAnimationState(SelectedBirdId);
            Refresh();
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

            if (_birdSelectGridRect == null && birdSelectGrid != null)
                _birdSelectGridRect = birdSelectGrid.GetComponent<RectTransform>();

            if (_careOptionsRect == null && careOptionsPanel != null)
                _careOptionsRect = careOptionsPanel.GetComponent<RectTransform>();

            if (_switchBirdButtonRect == null && switchBirdButton != null)
                _switchBirdButtonRect = switchBirdButton.GetComponent<RectTransform>();

            Vector2 gridOpenPosition = Vector2.zero;
            Vector2 gridClosedPosition = GetBirdSelectGridHiddenPosition();

            Vector2 careVisiblePosition = _careOptionsVisiblePosition;
            Vector2 careHiddenPosition = GetCareOptionsHiddenPosition();

            Vector2 switchVisiblePosition = _switchBirdButtonVisiblePosition;
            Vector2 switchHiddenPosition = GetSwitchBirdButtonHiddenPosition();

            if (showDrawer)
            {
                if (birdSelectGrid != null && !birdSelectGrid.activeSelf)
                    birdSelectGrid.SetActive(true);

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = gridClosedPosition;

                if (careOptionsPanel != null && careOptionsPanel.activeSelf)
                {
                    RefreshRectLayout(_careOptionsRect);

                    if (_careOptionsRect != null)
                        _careOptionsRect.anchoredPosition = careVisiblePosition;
                }

                if (switchBirdButton != null && switchBirdButton.gameObject.activeSelf)
                {
                    RefreshRectLayout(_switchBirdButtonRect);

                    if (_switchBirdButtonRect != null)
                        _switchBirdButtonRect.anchoredPosition = switchVisiblePosition;
                }
            }
            else
            {
                if (careOptionsPanel != null && !careOptionsPanel.activeSelf)
                    careOptionsPanel.SetActive(true);

                if (switchBirdButton != null && !switchBirdButton.gameObject.activeSelf)
                    switchBirdButton.gameObject.SetActive(true);

                RefreshRectLayout(_careOptionsRect);
                RefreshRectLayout(_switchBirdButtonRect);

                if (_careOptionsRect != null)
                    _careOptionsRect.anchoredPosition = careHiddenPosition;

                if (_switchBirdButtonRect != null)
                    _switchBirdButtonRect.anchoredPosition = switchHiddenPosition;
            }

            Vector2 birdStartPosition = _birdCardRect.anchoredPosition;
            Vector3 birdStartScale = _birdCardRect.localScale;
            Quaternion birdStartRotation = _birdCardRect.localRotation;

            Vector2 birdTargetPosition = showDrawer ? BirdCardDrawerOpenPosition : BirdCardDrawerClosedPosition;
            Vector3 birdTargetScale = showDrawer ? BirdCardDrawerOpenScale : BirdCardDrawerClosedScale;

            Vector2 gridStartPosition = _birdSelectGridRect != null ? _birdSelectGridRect.anchoredPosition : gridClosedPosition;
            Vector2 gridTargetPosition = showDrawer ? gridOpenPosition : gridClosedPosition;

            Vector2 careStartPosition = _careOptionsRect != null ? _careOptionsRect.anchoredPosition : careHiddenPosition;
            Vector2 careTargetPosition = showDrawer ? careHiddenPosition : careVisiblePosition;

            Vector2 switchStartPosition = _switchBirdButtonRect != null ? _switchBirdButtonRect.anchoredPosition : switchHiddenPosition;
            Vector2 switchTargetPosition = showDrawer ? switchHiddenPosition : switchVisiblePosition;

            float duration = Mathf.Max(
                0.0001f,
                Mathf.Max(
                    cardMoveDuration,
                    Mathf.Max(drawerDuration, Mathf.Max(careOptionsDuration, switchBirdDuration))
                )
            );

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;

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

                if (_careOptionsRect != null)
                {
                    float careT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, careOptionsDuration));
                    float careEased = EvaluateOvershoot(careT);
                    _careOptionsRect.anchoredPosition = Vector2.LerpUnclamped(careStartPosition, careTargetPosition, careEased);
                }

                if (_switchBirdButtonRect != null)
                {
                    float switchT = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, switchBirdDuration));
                    float switchEased = EvaluateOvershoot(switchT);
                    _switchBirdButtonRect.anchoredPosition = Vector2.LerpUnclamped(switchStartPosition, switchTargetPosition, switchEased);
                }

                yield return null;
            }

            if (showDrawer)
            {
                ForceBirdCardOpenState();

                if (_birdSelectGridRect != null)
                    _birdSelectGridRect.anchoredPosition = gridOpenPosition;

                if (_careOptionsRect != null)
                    _careOptionsRect.anchoredPosition = careHiddenPosition;

                if (_switchBirdButtonRect != null)
                    _switchBirdButtonRect.anchoredPosition = switchHiddenPosition;

                if (careOptionsPanel != null)
                    careOptionsPanel.SetActive(false);

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

                if (_careOptionsRect != null)
                    _careOptionsRect.anchoredPosition = careVisiblePosition;

                if (_switchBirdButtonRect != null)
                    _switchBirdButtonRect.anchoredPosition = switchVisiblePosition;

                if (careOptionsPanel != null)
                    careOptionsPanel.SetActive(true);

                if (switchBirdButton != null)
                    switchBirdButton.gameObject.SetActive(true);
            }

            inventoryStats.SetActive(!_isDrawerVisible);

            _layoutCoroutine = null;
        }

        private Vector2 GetBirdSelectGridHiddenPosition()
        {
            if (_birdSelectGridRect == null)
                return new Vector2(0f, 700f);

            float hiddenY = _birdSelectGridRect.rect.height + DrawerHiddenPadding;
            return new Vector2(0f, hiddenY);
        }

        private Vector2 GetCareOptionsHiddenPosition()
        {
            if (_careOptionsRect == null)
                return _careOptionsVisiblePosition + new Vector2(0f, -1600f);

            float panelHeight = Mathf.Max(_careOptionsRect.rect.height, Mathf.Abs(_careOptionsRect.sizeDelta.y));
            float parentHeight = 0f;
            float canvasHeight = 0f;

            RectTransform parentRect = _careOptionsRect.parent as RectTransform;
            if (parentRect != null)
                parentHeight = Mathf.Abs(parentRect.rect.height);

            if (_rootCanvasRect != null)
                canvasHeight = Mathf.Abs(_rootCanvasRect.rect.height);

            float travelDistance = Mathf.Max(parentHeight, canvasHeight, 1200f) + panelHeight + careOptionsHiddenPadding;

            return _careOptionsVisiblePosition + new Vector2(0f, -travelDistance);
        }

        private Vector2 GetSwitchBirdButtonHiddenPosition()
        {
            if (_switchBirdButtonRect == null)
                return _switchBirdButtonVisiblePosition + new Vector2(1600f, 0f);

            float buttonWidth = Mathf.Max(_switchBirdButtonRect.rect.width, Mathf.Abs(_switchBirdButtonRect.sizeDelta.x));
            float parentWidth = 0f;
            float canvasWidth = 0f;

            RectTransform parentRect = _switchBirdButtonRect.parent as RectTransform;
            if (parentRect != null)
                parentWidth = Mathf.Abs(parentRect.rect.width);

            if (_rootCanvasRect != null)
                canvasWidth = Mathf.Abs(_rootCanvasRect.rect.width);

            float travelDistance = Mathf.Max(parentWidth, canvasWidth, 1200f) + buttonWidth + switchBirdHiddenPadding;

            return _switchBirdButtonVisiblePosition + new Vector2(travelDistance, 0f);
        }

        private void CacheCareOptionsVisiblePosition()
        {
            if (_careOptionsRect == null && careOptionsPanel != null)
                _careOptionsRect = careOptionsPanel.GetComponent<RectTransform>();

            if (_careOptionsRect == null)
                return;

            RefreshRectLayout(_careOptionsRect);
            _careOptionsVisiblePosition = _careOptionsRect.anchoredPosition;
        }

        private void CacheSwitchBirdButtonVisiblePosition()
        {
            if (_switchBirdButtonRect == null && switchBirdButton != null)
                _switchBirdButtonRect = switchBirdButton.GetComponent<RectTransform>();

            if (_switchBirdButtonRect == null)
                return;

            RefreshRectLayout(_switchBirdButtonRect);
            _switchBirdButtonVisiblePosition = _switchBirdButtonRect.anchoredPosition;
        }

        private void RefreshRectLayout(RectTransform rectTransform)
        {
            if (rectTransform == null)
                return;

            RectTransform parentRect = rectTransform.parent as RectTransform;

            Canvas.ForceUpdateCanvases();

            if (parentRect != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            Canvas.ForceUpdateCanvases();
        }

        private float EvaluateOvershoot(float t)
        {
            t = Mathf.Clamp01(t);

            if (movementOvershoot <= 0f)
                return t;

            float s = movementOvershoot;
            float inv = t - 1f;
            return 1f + (s + 1f) * inv * inv * inv + s * inv * inv;
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

            if (_careOptionsRect == null && careOptionsPanel != null)
                _careOptionsRect = careOptionsPanel.GetComponent<RectTransform>();

            if (_switchBirdButtonRect == null && switchBirdButton != null)
                _switchBirdButtonRect = switchBirdButton.GetComponent<RectTransform>();

            if (careOptionsPanel != null)
            {
                if (showDrawer)
                {
                    RefreshRectLayout(_careOptionsRect);

                    if (_careOptionsRect != null)
                        _careOptionsRect.anchoredPosition = GetCareOptionsHiddenPosition();

                    careOptionsPanel.SetActive(false);
                }
                else
                {
                    careOptionsPanel.SetActive(true);
                    RefreshRectLayout(_careOptionsRect);

                    if (_careOptionsRect != null)
                        _careOptionsRect.anchoredPosition = _careOptionsVisiblePosition;
                }
            }

            if (switchBirdButton != null)
            {
                if (showDrawer)
                {
                    RefreshRectLayout(_switchBirdButtonRect);

                    if (_switchBirdButtonRect != null)
                        _switchBirdButtonRect.anchoredPosition = GetSwitchBirdButtonHiddenPosition();

                    switchBirdButton.gameObject.SetActive(false);
                }
                else
                {
                    switchBirdButton.gameObject.SetActive(true);
                    RefreshRectLayout(_switchBirdButtonRect);

                    if (_switchBirdButtonRect != null)
                        _switchBirdButtonRect.anchoredPosition = _switchBirdButtonVisiblePosition;
                }
            }
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
            if (string.IsNullOrEmpty(_currentBirdId))
                return;

            BirdCafeGame.Instance.TryStartCareMinigame(_currentBirdId, CareActionIds.Play);
        }

        public IEnumerator OpenFlappy()
        {
            yield return SceneManager.LoadSceneAsync("Flappy", LoadSceneMode.Single);

            Scene flappyScene = SceneManager.GetSceneByName("Flappy");
            if (flappyScene.IsValid() && flappyScene.isLoaded)
            {
                SceneManager.SetActiveScene(flappyScene);
            }
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

            // This is a hack to bring back bird energy
            if (actionId == "Vet" && success)
            {
                var bird = BirdCafeGame.Instance.Controller.CurrentState.Birds.FirstOrDefault(b => b.Id == SelectedBirdId);
                bird.Energy = 100;
            }

            if (success)
            {
                var bird = BirdCafeGame.Instance.Controller.CurrentState.Birds.FirstOrDefault(b => b.Id == SelectedBirdId);

                BirdCafeGame.Instance.AdvanceBirdAnimationState(SelectedBirdId);

                Refresh();
            }
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