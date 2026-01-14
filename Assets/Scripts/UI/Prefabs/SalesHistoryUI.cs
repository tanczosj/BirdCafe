using UnityEngine;
using System.Collections.Generic;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.UI.Components
{
    public class SalesHistoryUI : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The HistoryRow prefab to instantiate for each day.")]
        [SerializeField] private GameObject rowPrefab;

        [Tooltip("The object with the Vertical Layout Group where rows will be spawned.")]
        [SerializeField] private Transform container;

        /// <summary>
        /// Setting this property automatically clears the table and rebuilds 
        /// the rows based on the provided list.
        /// </summary>
        public List<DailySalesHistoryModel> History
        {
            set
            {
                RebuildTable(value);
            }
        }

        private void RebuildTable(List<DailySalesHistoryModel> data)
        {
            if (container == null || rowPrefab == null)
            {
                Debug.LogWarning("SalesHistoryUI: Missing references.");
                return;
            }

            // 1. Clear existing rows
            // We iterate backwards or use a loop to destroy immediate children
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            if (data == null) return;

            // 2. Instantiate new rows
            for (int i = 0; i < data.Count; i++)
            {
                var item = data[i];
                GameObject go = Instantiate(rowPrefab, container);

                var script = go.GetComponent<HistoryRowUI>();
                if (script)
                {
                    // Pass 'true' if i is even (0, 2, 4...) for striping visuals
                    script.Initialize(item, i % 2 == 0);
                }
            }
        }

        // Auto-assign container if script is attached to the same object as the Layout Group
        private void Reset()
        {
            if (container == null) container = transform;
        }
    }
}