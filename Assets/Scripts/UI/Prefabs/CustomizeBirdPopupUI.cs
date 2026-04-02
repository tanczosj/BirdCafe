using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BirdCafe.UI.Components
{
    [DisallowMultipleComponent]
    public class CustomizeBirdPopupUI : MonoBehaviour
    {
        [Serializable]
        public class CostumeOption
        {
            public string id;
            public Sprite sprite;
        }

        [Header("References")]
        [SerializeField] private RectTransform birdCostumeSelector;
        [SerializeField] private RectTransform costumesRoot;
        [SerializeField] private GridLayoutGroup costumesGrid;
        [SerializeField] private Button arrowLeft;
        [SerializeField] private Button arrowRight;
        [SerializeField] private TMP_InputField birdNameInput;

        [Header("Costumes")]
        [SerializeField] private List<CostumeOption> costumes = new List<CostumeOption>();

        [Header("Animation")]
        [SerializeField] private float moveDuration = 0.25f;
        [SerializeField] private bool useUnscaledTime = true;

        private Coroutine moveCoroutine;
        private int currentColumnIndex;
        private bool listenersBound;

        public int SelectedCostumeIndex
        {
            get
            {
                if (costumes.Count == 0)
                    return -1;

                int index = currentColumnIndex * GetVisibleRows();
                return Mathf.Clamp(index, 0, costumes.Count - 1);
            }
        }

        public string SelectedCostumeId
        {
            get
            {
                int index = SelectedCostumeIndex;
                if (index < 0 || index >= costumes.Count)
                    return string.Empty;

                return costumes[index].id;
            }
        }

        public string BirdName => birdNameInput != null ? birdNameInput.text : string.Empty;

        private void Reset()
        {
            AutoAssignReferences();
        }

        private void Awake()
        {
            AutoAssignReferences();
            BindListeners();
            RebuildCostumeIcons();
            SnapToCurrentColumn(false);
            RefreshArrowState();
        }

        private void OnDestroy()
        {
            UnbindListeners();
        }

        public void SetInitialState(int costumeIndex, string birdName)
        {
            if (birdNameInput != null)
                birdNameInput.SetTextWithoutNotify(birdName ?? string.Empty);

            SetSelectedCostumeIndex(costumeIndex, true);
        }

        public void SetSelectedCostumeIndex(int costumeIndex, bool notify)
        {
            if (costumes.Count == 0)
            {
                currentColumnIndex = 0;
                RefreshArrowState();

                if (notify)
                    OnChanged();

                return;
            }

            costumeIndex = Mathf.Clamp(costumeIndex, 0, costumes.Count - 1);
            currentColumnIndex = GetColumnForCostumeIndex(costumeIndex);

            StopActiveTween();
            SnapToCurrentColumn(notify);
        }

        public void SetCostumes(List<CostumeOption> newCostumes, int startingIndex = 0, string birdName = "")
        {
            costumes = newCostumes ?? new List<CostumeOption>();
            RebuildCostumeIcons();
            SetInitialState(startingIndex, birdName);
        }

        public void MoveLeft()
        {
            if (currentColumnIndex <= 0)
                return;

            MoveToColumn(currentColumnIndex - 1);
        }

        public void MoveRight()
        {
            if (currentColumnIndex >= GetMaxColumnIndex())
                return;

            MoveToColumn(currentColumnIndex + 1);
        }

        [ContextMenu("Auto Assign References")]
        private void AutoAssignReferences()
        {
            if (birdCostumeSelector == null)
            {
                Transform selector = transform.Find("BirdCostumeSelector");
                if (selector != null)
                    birdCostumeSelector = selector as RectTransform;
            }

            if (costumesRoot == null && birdCostumeSelector != null)
            {
                Transform costumesTransform = birdCostumeSelector.Find("Costumes");
                if (costumesTransform != null)
                    costumesRoot = costumesTransform as RectTransform;
            }

            if (costumesGrid == null && costumesRoot != null)
                costumesGrid = costumesRoot.GetComponent<GridLayoutGroup>();

            if (arrowLeft == null)
            {
                Transform left = transform.Find("ArrowLeft");
                if (left != null)
                    arrowLeft = left.GetComponent<Button>();
            }

            if (arrowRight == null)
            {
                Transform right = transform.Find("ArrowRight");
                if (right != null)
                    arrowRight = right.GetComponent<Button>();
            }

            if (birdNameInput == null)
            {
                Transform input = transform.Find("BirdNameTMP");
                if (input != null)
                    birdNameInput = input.GetComponent<TMP_InputField>();
            }
        }

        [ContextMenu("Rebuild Costume Icons")]
        public void RebuildCostumeIcons()
        {
            if (costumesRoot == null || costumesGrid == null)
                return;

            ClearChildren(costumesRoot);

            for (int i = 0; i < costumes.Count; i++)
            {
                CostumeOption costume = costumes[i];

                GameObject iconObject = new GameObject(
                    string.IsNullOrWhiteSpace(costume.id) ? $"Costume_{i}" : $"Costume_{costume.id}",
                    typeof(RectTransform),
                    typeof(CanvasRenderer),
                    typeof(Image));

                iconObject.transform.SetParent(costumesRoot, false);

                RectTransform rect = iconObject.GetComponent<RectTransform>();
                rect.localScale = Vector3.one;

                Image image = iconObject.GetComponent<Image>();
                image.sprite = costume.sprite;
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            ResizeCostumesRoot();

            currentColumnIndex = Mathf.Clamp(currentColumnIndex, 0, GetMaxColumnIndex());
            SetCostumesRootX(GetTargetXForColumn(currentColumnIndex));
            RefreshArrowState();

            LayoutRebuilder.ForceRebuildLayoutImmediate(costumesRoot);
        }

        protected virtual void OnChanged()
        {
            // Intentionally empty for now.
        }

        private void BindListeners()
        {
            if (listenersBound)
                return;

            if (arrowLeft != null)
                arrowLeft.onClick.AddListener(MoveLeft);

            if (arrowRight != null)
                arrowRight.onClick.AddListener(MoveRight);

            if (birdNameInput != null)
                birdNameInput.onValueChanged.AddListener(HandleBirdNameChanged);

            listenersBound = true;
        }

        private void UnbindListeners()
        {
            if (!listenersBound)
                return;

            if (arrowLeft != null)
                arrowLeft.onClick.RemoveListener(MoveLeft);

            if (arrowRight != null)
                arrowRight.onClick.RemoveListener(MoveRight);

            if (birdNameInput != null)
                birdNameInput.onValueChanged.RemoveListener(HandleBirdNameChanged);

            listenersBound = false;
        }

        private void HandleBirdNameChanged(string _)
        {
            OnChanged();
        }

        private void MoveToColumn(int targetColumn)
        {
            targetColumn = Mathf.Clamp(targetColumn, 0, GetMaxColumnIndex());

            if (targetColumn == currentColumnIndex)
            {
                RefreshArrowState();
                return;
            }

            currentColumnIndex = targetColumn;

            StopActiveTween();
            moveCoroutine = StartCoroutine(TweenToX(
                costumesRoot.anchoredPosition.x,
                GetTargetXForColumn(currentColumnIndex),
                moveDuration));

            RefreshArrowState();
            OnChanged();
        }

        private void SnapToCurrentColumn(bool notify)
        {
            SetCostumesRootX(GetTargetXForColumn(currentColumnIndex));
            RefreshArrowState();

            if (notify)
                OnChanged();
        }

        private IEnumerator TweenToX(float fromX, float toX, float duration)
        {
            if (costumesRoot == null)
                yield break;

            if (duration <= 0f)
            {
                SetCostumesRootX(toX);
                moveCoroutine = null;
                yield break;
            }

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / duration);
                float eased = EaseInOutQuad(t);
                float x = Mathf.LerpUnclamped(fromX, toX, eased);

                SetCostumesRootX(x);
                yield return null;
            }

            SetCostumesRootX(toX);
            moveCoroutine = null;
        }

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f
                ? 2f * t * t
                : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        }

        private void StopActiveTween()
        {
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                moveCoroutine = null;
            }
        }

        private void SetCostumesRootX(float x)
        {
            if (costumesRoot == null)
                return;

            Vector2 pos = costumesRoot.anchoredPosition;
            pos.x = x;
            costumesRoot.anchoredPosition = pos;
        }

        private void RefreshArrowState()
        {
            int maxColumn = GetMaxColumnIndex();

            if (arrowLeft != null)
                arrowLeft.interactable = currentColumnIndex > 0;

            if (arrowRight != null)
                arrowRight.interactable = currentColumnIndex < maxColumn;
        }

        private int GetColumnForCostumeIndex(int costumeIndex)
        {
            int rows = GetVisibleRows();
            return Mathf.FloorToInt(costumeIndex / (float)rows);
        }

        private int GetVisibleRows()
        {
            if (birdCostumeSelector == null || costumesGrid == null)
                return 1;

            float usableHeight =
                birdCostumeSelector.rect.height
                - costumesGrid.padding.top
                - costumesGrid.padding.bottom;

            float rowSize = costumesGrid.cellSize.y + costumesGrid.spacing.y;

            if (rowSize <= 0f)
                return 1;

            int rows = Mathf.FloorToInt((usableHeight + costumesGrid.spacing.y) / rowSize);
            return Mathf.Max(1, rows);
        }

        private int GetVisibleColumns()
        {
            if (birdCostumeSelector == null || costumesGrid == null)
                return 1;

            float usableWidth =
                birdCostumeSelector.rect.width
                - costumesGrid.padding.left
                - costumesGrid.padding.right;

            float columnSize = costumesGrid.cellSize.x + costumesGrid.spacing.x;

            if (columnSize <= 0f)
                return 1;

            int columns = Mathf.FloorToInt((usableWidth + costumesGrid.spacing.x) / columnSize);
            return Mathf.Max(1, columns);
        }

        private int GetTotalColumns()
        {
            if (costumes.Count == 0)
                return 0;

            int rows = GetVisibleRows();
            return Mathf.CeilToInt(costumes.Count / (float)rows);
        }

        private int GetMaxColumnIndex()
        {
            int totalColumns = GetTotalColumns();
            int visibleColumns = GetVisibleColumns();

            return Mathf.Max(0, totalColumns - visibleColumns);
        }

        private float GetStepX()
        {
            if (costumesGrid == null)
                return 0f;

            return costumesGrid.cellSize.x + costumesGrid.spacing.x;
        }

        private float GetContentWidth()
        {
            if (costumesGrid == null)
                return 0f;

            int totalColumns = Mathf.Max(1, GetTotalColumns());

            return costumesGrid.padding.left
                   + costumesGrid.padding.right
                   + totalColumns * costumesGrid.cellSize.x
                   + Mathf.Max(0, totalColumns - 1) * costumesGrid.spacing.x;
        }

        private float GetTargetXForColumn(int columnIndex)
        {
            if (costumesRoot == null || costumesGrid == null)
                return 0f;

            float contentWidth = GetContentWidth();
            float stepX = GetStepX();
            float cellWidth = costumesGrid.cellSize.x;

            return (contentWidth * costumesRoot.pivot.x)
                   - costumesGrid.padding.left
                   - (columnIndex * stepX)
                   - (cellWidth * 0.5f);
        }

        private void ResizeCostumesRoot()
        {
            if (costumesRoot == null || costumesGrid == null)
                return;

            int totalColumns = Mathf.Max(1, GetTotalColumns());
            int rowsUsed = Mathf.Min(GetVisibleRows(), Mathf.Max(1, costumes.Count));

            float width =
                costumesGrid.padding.left
                + costumesGrid.padding.right
                + totalColumns * costumesGrid.cellSize.x
                + Mathf.Max(0, totalColumns - 1) * costumesGrid.spacing.x;

            float height =
                costumesGrid.padding.top
                + costumesGrid.padding.bottom
                + rowsUsed * costumesGrid.cellSize.y
                + Mathf.Max(0, rowsUsed - 1) * costumesGrid.spacing.y;

            Vector2 size = costumesRoot.sizeDelta;
            size.x = width;
            size.y = height;
            costumesRoot.sizeDelta = size;
        }

        private void ClearChildren(RectTransform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                GameObject child = parent.GetChild(i).gameObject;

#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(child);
                else
                    Destroy(child);
#else
                Destroy(child);
#endif
            }
        }
    }
}