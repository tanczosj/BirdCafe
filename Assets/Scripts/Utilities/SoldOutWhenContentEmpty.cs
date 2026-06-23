using System.Collections;
using UnityEngine;

namespace BirdCafe.Unity.UI
{
    [DisallowMultipleComponent]
    public sealed class SoldOutWhenContentEmpty : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform content;
        [SerializeField] private GameObject soldOutPrefab;

        [Header("Sold Out Messages")]
        [SerializeField] private string soldOutTitleMessage = "[SOLD OUT]";
        [SerializeField] private string soldOutDescriptionMessage = "Every pet has already been purchased.";

        [Header("Counting")]
        [SerializeField] private bool countOnlyActiveChildren = true;

        [Tooltip("Turn this on if your bird items are hidden with SetActive(false) instead of destroyed.")]
        [SerializeField] private bool checkEveryFrame = false;

        private GameObject _soldOutInstance;
        private SoldOutMessageUI _soldOutMessageUI;
        private bool _isRefreshing;

        private void Reset()
        {
            content = transform;
        }

        private void Awake()
        {
            if (content == null)
                content = transform;

            Refresh();
        }

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (checkEveryFrame)
                Refresh();
        }

        private void OnTransformChildrenChanged()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (_isRefreshing)
                return;

            if (content == null || soldOutPrefab == null)
                return;

            _isRefreshing = true;

            int realChildCount = GetRealChildCount();

            if (realChildCount <= 0)
                ShowSoldOutMessage();
            else
                HideSoldOutMessage();

            _isRefreshing = false;
        }

        public void RefreshNextFrame()
        {
            if (!gameObject.activeInHierarchy)
                return;

            StartCoroutine(RefreshNextFrameRoutine());
        }

        private IEnumerator RefreshNextFrameRoutine()
        {
            yield return null;
            Refresh();
        }

        private int GetRealChildCount()
        {
            int count = 0;

            for (int i = 0; i < content.childCount; i++)
            {
                Transform child = content.GetChild(i);

                if (_soldOutInstance != null && child.gameObject == _soldOutInstance)
                    continue;

                if (child.GetComponent<SoldOutMessageUI>() != null)
                    continue;

                if (countOnlyActiveChildren && !child.gameObject.activeInHierarchy)
                    continue;

                count++;
            }

            return count;
        }

        private void ShowSoldOutMessage()
        {
            if (_soldOutInstance == null)
            {
                _soldOutInstance = Instantiate(soldOutPrefab, content);
                _soldOutInstance.name = "Sold Out Message";

                _soldOutMessageUI = _soldOutInstance.GetComponent<SoldOutMessageUI>();

                if (_soldOutMessageUI == null)
                    _soldOutMessageUI = _soldOutInstance.GetComponentInChildren<SoldOutMessageUI>(true);
            }

            if (_soldOutMessageUI != null)
            {
                _soldOutMessageUI.SetMessages(
                    soldOutTitleMessage,
                    soldOutDescriptionMessage
                );
            }

            _soldOutInstance.SetActive(true);
            _soldOutInstance.transform.SetAsLastSibling();
        }

        private void HideSoldOutMessage()
        {
            if (_soldOutInstance != null)
                _soldOutInstance.SetActive(false);
        }
    }
}