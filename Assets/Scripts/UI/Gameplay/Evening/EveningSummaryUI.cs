using UnityEngine;
using TMPro;
using BirdCafe.Shared;
using BirdCafe.Shared.ViewModels;
using BirdCafe.UI.Components;
using System.Text;
using System.Collections;

namespace BirdCafe.UI.Gameplay.Evening
{
    public class EveningSummaryUI : MonoBehaviour
    {
        [Header("Header Stats")]
        [Tooltip("Shows current money balance")]
        public StatCounterUI moneyCounter;

        [Tooltip("Shows current popularity")]
        public StatCounterUI popularityCounter;

        public TMP_Text dayTextLabel;

        [Header("Narratives")]
        public TMP_Text popularityNarrativeText;
        public TMP_Text financialSummaryText;
        public TMP_Text trafficNarrativeText;

        [Header("Sales Breakdown")]
        public ReportStat coffeeStat;
        public ReportStat bakedStat;
        public ReportStat merchStat;

        [Header("Bird Performance (Simple)")]
        public TMP_Text birdNameText;
        public TMP_Text birdServedText;

        [Header("Effects")]
        public ParticleSystem[] summaryParticles;
        public float particleStopDelay = 2f;

        private Coroutine particleRoutine;

        private void OnEnable()
        {
            Refresh();
            PlaySummaryParticles();
        }

        private void OnDisable()
        {
            if (particleRoutine != null)
            {
                StopCoroutine(particleRoutine);
                particleRoutine = null;
            }

            StopSummaryParticles();
        }

        private void Refresh()
        {
            DailyReportViewModel vm = BirdCafeGame.Instance.GetDailyReport();
            if (vm == null) return;

            if (dayTextLabel)
                dayTextLabel.text = $"Day {vm.DayNumber} - {vm.DayName}";

            if (moneyCounter)
                moneyCounter.AnimateValue((float)vm.CurrentMoney, 0.5f);

            if (popularityCounter)
                popularityCounter.AnimateValue(vm.CurrentPopularity, 0.5f);

            if (popularityNarrativeText)
                popularityNarrativeText.text = vm.PopularityNarrative;

            if (financialSummaryText)
            {
                string colorHex = (vm.NetProfit >= 0) ? "#00FF00" : "#FF0000";
                financialSummaryText.text = $"Revenue: ${vm.TotalRevenue:F2}     Net Profit: <color={colorHex}>${vm.NetProfit:F2}</color>";
            }

            if (trafficNarrativeText)
                trafficNarrativeText.text = BuildTrafficString(vm);

            if (coffeeStat)
                coffeeStat.UpdateValue(vm.CoffeeSold, vm.CoffeeTotal, 1.0f);

            if (bakedStat)
                bakedStat.UpdateValue(vm.BakedSold, vm.BakedTotal, 1.0f);

            if (merchStat)
                merchStat.UpdateValue(vm.MerchSold, vm.MerchTotal, 1.0f);

            if (vm.Birds.Count > 0)
            {
                var b = vm.Birds[0];

                if (birdNameText)
                    birdNameText.text = b.Name;

                if (birdServedText)
                    birdServedText.text = $"{b.CustomersServed} Served";
            }
            else
            {
                if (birdNameText)
                    birdNameText.text = "No Birds";

                if (birdServedText)
                    birdServedText.text = "-";
            }
        }

        private string BuildTrafficString(DailyReportViewModel vm)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"You served {vm.CustomersServed} customers today");

            if (vm.CustomersLost > 0)
            {
                sb.Append($" but lost {vm.CustomersLost} customers.");

                bool hasWait = vm.LostWaitTooLong > 0;
                bool hasStock = vm.LostNoStock > 0;

                if (hasWait)
                {
                    sb.Append($" There were {vm.LostWaitTooLong} customers who got tired of waiting");
                }

                if (hasStock)
                {
                    if (hasWait)
                        sb.Append(" and ");
                    else
                        sb.Append(" There were ");

                    sb.Append($"{vm.LostNoStock} who wanted to order but you didn't have enough in stock.");
                }
            }
            else
            {
                sb.Append(" and didn't lose a single one! Great job!");
            }

            return sb.ToString();
        }

        private void PlaySummaryParticles()
        {
            if (particleRoutine != null)
            {
                StopCoroutine(particleRoutine);
            }

            particleRoutine = StartCoroutine(PlayParticlesRoutine());
        }

        private IEnumerator PlayParticlesRoutine()
        {
            if (summaryParticles != null)
            {
                foreach (ParticleSystem ps in summaryParticles)
                {
                    if (ps == null) continue;

                    ps.gameObject.SetActive(true);
                    ps.Clear();
                    ps.Play();
                }
            }

            yield return new WaitForSeconds(particleStopDelay);

            StopSummaryParticles();
            particleRoutine = null;
        }

        private void StopSummaryParticles()
        {
            if (summaryParticles != null)
            {
                foreach (ParticleSystem ps in summaryParticles)
                {
                    if (ps == null) continue;

                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }
            }
        }

        public void OnContinueClicked()
        {
            BirdCafeGame.Instance.AcknowledgeSummary();
        }
    }
}