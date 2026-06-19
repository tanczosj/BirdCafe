
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using System.Linq;
using UnityEngine;

namespace BirdCafe.Unity.Birds
{
    /// <summary>
    /// Binds a local BirdVisualController to a bird at a specific shared roster index.
    /// If the requested slot does not exist, the visual is unbound and hidden.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class BirdRosterSlotBinder : MonoBehaviour
    {
        [SerializeField] private BirdVisualController birdVisualController;
        [Min(0)]
        [SerializeField] private int rosterIndex;
        [SerializeField] private bool bindOnEnable = true;
        [SerializeField] private bool hideIfMissing = true;

        private void Reset()
        {
            if (birdVisualController == null)
            {
                birdVisualController = GetComponent<BirdVisualController>();
            }
        }

        private void OnEnable()
        {
            if (bindOnEnable)
            {
                RefreshBinding();
            }
        }

        [ContextMenu("Refresh Binding")]
        public void RefreshBinding()
        {
            if (birdVisualController == null)
            {
                birdVisualController = GetComponent<BirdVisualController>();
            }

            if (birdVisualController == null)
            {
                Debug.LogWarning("[BirdRosterSlotBinder] Missing BirdVisualController reference.", this);
                return;
            }

            if (BirdVisualFacade.TryGetBirdAnimationStateAtRosterIndex(rosterIndex, out BirdAnimationStateViewModel viewModel))
            {
                birdVisualController.BindToViewModel(viewModel, immediateApply: true);
                return;
            }

            //var bird = BirdCafeGame.Instance.Controller.CurrentState.Birds?.FirstOrDefault(b => b.Id == viewModel.BirdId);

            birdVisualController.Unbind(hideVisual: hideIfMissing); // || bird?.AssignedDayOffNextDay == true);
        }

        public void SetRosterIndex(int newRosterIndex, bool refreshNow = true)
        {
            rosterIndex = Mathf.Max(0, newRosterIndex);

            if (refreshNow)
            {
                RefreshBinding();
            }
        }
    }
}