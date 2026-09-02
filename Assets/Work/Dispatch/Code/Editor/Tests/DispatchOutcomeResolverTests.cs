using System.Collections.Generic;
using NUnit.Framework;
using Work.Dispatch.Code.Runtime;

namespace Work.Dispatch.Code.Editor.Tests
{
    public sealed class DispatchOutcomeResolverTests
    {
        [Test]
        public void Resolve_IsDeterministicAndInsideVisibleRange()
        {
            DispatchOutcomeResolver resolver = new DispatchOutcomeResolver();
            List<DispatchResolvedRequest> requests = new List<DispatchResolvedRequest>
            {
                new DispatchResolvedRequest("mushroom", 5, 3, 5)
            };
            DispatchRareSettings noRare = new DispatchRareSettings(0f, new List<DispatchRareCandidate>());

            List<DispatchRewardData> first = resolver.Resolve(requests, noRare, 12345);
            List<DispatchRewardData> second = resolver.Resolve(requests, noRare, 12345);

            Assert.That(first[0].GrantedAmount, Is.InRange(3, 5));
            Assert.That(second[0].GrantedAmount, Is.EqualTo(first[0].GrantedAmount));
        }

        [Test]
        public void Resolve_AddsAtMostOneRareReward()
        {
            DispatchOutcomeResolver resolver = new DispatchOutcomeResolver();
            List<DispatchResolvedRequest> requests = new List<DispatchResolvedRequest>
            {
                new DispatchResolvedRequest("mushroom", 5, 3, 5)
            };
            DispatchRareSettings guaranteedRare = new DispatchRareSettings(
                100f,
                new List<DispatchRareCandidate>
                {
                    new DispatchRareCandidate("crystal", 1, 1, 1),
                    new DispatchRareCandidate("gold_herb", 1, 1, 2)
                });

            List<DispatchRewardData> rewards = resolver.Resolve(requests, guaranteedRare, 7);

            Assert.That(rewards.Count, Is.EqualTo(2));
            Assert.That(rewards[1].IsRare, Is.True);
            Assert.That(rewards[1].RemainingAmount, Is.GreaterThanOrEqualTo(1));
        }
    }
}
