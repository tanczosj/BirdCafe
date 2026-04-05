
using System.Collections.Generic;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;

namespace BirdCafe.Unity.Birds
{
    /// <summary>
    /// Thin Unity-side adapter over the real shared BirdCafeGame facade.
    /// This intentionally uses the actual shared API directly instead of reflection.
    /// </summary>
    public static class BirdVisualFacade
    {
        public static bool TryGetBirdAnimationState(string birdId, out BirdAnimationStateViewModel viewModel)
        {
            viewModel = null;

            if (string.IsNullOrWhiteSpace(birdId))
            {
                return false;
            }

            BirdCafeGame game = BirdCafeGame.Instance;
            if (game == null)
            {
                return false;
            }

            viewModel = game.GetBirdAnimationState(birdId);
            return viewModel != null;
        }

        public static List<BirdAnimationStateViewModel> GetAllBirdAnimationStates()
        {
            BirdCafeGame game = BirdCafeGame.Instance;
            if (game == null)
            {
                return new List<BirdAnimationStateViewModel>();
            }

            return game.GetAllBirdAnimationStates() ?? new List<BirdAnimationStateViewModel>();
        }

        public static bool TryGetBirdAnimationStateAtRosterIndex(int rosterIndex, out BirdAnimationStateViewModel viewModel)
        {
            viewModel = null;

            if (rosterIndex < 0)
            {
                return false;
            }

            List<BirdAnimationStateViewModel> states = GetAllBirdAnimationStates();
            if (states == null || rosterIndex >= states.Count)
            {
                return false;
            }

            viewModel = states[rosterIndex];
            return viewModel != null && !string.IsNullOrWhiteSpace(viewModel.BirdId);
        }

        public static bool TryAdvanceBirdAnimationState(string birdId)
        {
            if (string.IsNullOrWhiteSpace(birdId))
            {
                return false;
            }

            BirdCafeGame game = BirdCafeGame.Instance;
            if (game == null)
            {
                return false;
            }

            return game.AdvanceBirdAnimationState(birdId);
        }

        public static bool TryTriggerBirdAnimationEvent(string birdId, string eventId)
        {
            if (string.IsNullOrWhiteSpace(birdId) || string.IsNullOrWhiteSpace(eventId))
            {
                return false;
            }

            BirdCafeGame game = BirdCafeGame.Instance;
            if (game == null)
            {
                return false;
            }

            return game.TriggerBirdAnimationEvent(birdId, eventId);
        }
    }
}