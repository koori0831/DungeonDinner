using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Work.Dispatch.Code.Runtime;
using Work.TimeSystem;

namespace Work.Dispatch.Code.Editor.Tests
{
    public sealed class DispatchRuntimeStateTests
    {
        [Test]
        public void Reconcile_ReturnsExactlyAtCompletionTime()
        {
            DispatchRuntimeState state = new DispatchRuntimeState();
            DispatchJob job = CreateJob(startedAt: 4, requiredTime: 12);

            Assert.That(state.TryStart(job), Is.True);
            Assert.That(state.Reconcile(15), Is.Null);
            Assert.That(state.HasActiveJob, Is.True);

            DispatchJob returned = state.Reconcile(16);

            Assert.That(returned, Is.SameAs(job));
            Assert.That(returned.State, Is.EqualTo(DispatchState.Returned));
            Assert.That(state.HasActiveJob, Is.False);
            Assert.That(state.ReturnedReports.Count, Is.EqualTo(1));
        }

        [Test]
        public void TryStart_AllowsOnlyOneActiveJob()
        {
            DispatchRuntimeState state = new DispatchRuntimeState();

            Assert.That(state.TryStart(CreateJob(0, 4)), Is.True);
            Assert.That(state.TryStart(CreateJob(0, 2)), Is.False);
            Assert.That(state.IsNpcDispatched("odin"), Is.True);
            Assert.That(state.IsNpcDispatched("OtherNpc"), Is.False);
        }

        [Test]
        public void TwelveTimeDispatch_ReturnsAfterFourthRestaurantOperation()
        {
            GameTimeState time = new GameTimeState();
            DispatchRuntimeState dispatch = new DispatchRuntimeState();
            dispatch.TryStart(CreateJob(0, 12));

            for (int operation = 1; operation <= 3; operation++)
            {
                time.Advance(3);
                Assert.That(dispatch.Reconcile(time.TotalElapsedTime), Is.Null);
            }

            time.Advance(3);
            DispatchJob returned = dispatch.Reconcile(time.TotalElapsedTime);

            Assert.That(time.TotalElapsedTime, Is.EqualTo(12));
            Assert.That(time.CurrentDay, Is.EqualTo(3));
            Assert.That(time.CurrentTimeOfDay, Is.EqualTo(0));
            Assert.That(returned, Is.Not.Null);
            Assert.That(returned.State, Is.EqualTo(DispatchState.Returned));
        }

        [Test]
        public void JsonSaveAndReload_PreservesHiddenRewards()
        {
            DispatchRuntimeState original = new DispatchRuntimeState();
            DispatchJob job = CreateJob(0, 2);
            job.Rewards.Add(new DispatchRewardData("mushroom", 4, false));
            original.TryStart(job);

            string json = JsonUtility.ToJson(original.CreateSaveData());
            DispatchSaveData restoredSave = JsonUtility.FromJson<DispatchSaveData>(json);
            DispatchRuntimeState loaded = new DispatchRuntimeState(restoredSave);

            Assert.That(loaded.ActiveJob.Rewards.Count, Is.EqualTo(1));
            Assert.That(loaded.ActiveJob.Rewards[0].GrantedAmount, Is.EqualTo(4));
            Assert.That(loaded.ActiveJob.Rewards[0].RemainingAmount, Is.EqualTo(4));
        }

        [Test]
        public void JsonSaveAndReload_PreservesPartiallyClaimedReport()
        {
            DispatchRuntimeState original = new DispatchRuntimeState();
            DispatchJob job = CreateJob(0, 2);
            job.Rewards.Add(new DispatchRewardData("mushroom", 5, false));
            original.TryStart(job);
            original.Reconcile(2);
            original.ReturnedReports[0].Rewards[0].RemainingAmount = 2;

            string json = JsonUtility.ToJson(original.CreateSaveData());
            DispatchSaveData restoredSave = JsonUtility.FromJson<DispatchSaveData>(json);
            DispatchRuntimeState loaded = new DispatchRuntimeState(restoredSave);

            Assert.That(loaded.HasActiveJob, Is.False);
            Assert.That(loaded.ReturnedReports.Count, Is.EqualTo(1));
            Assert.That(loaded.ReturnedReports[0].State, Is.EqualTo(DispatchState.Returned));
            Assert.That(loaded.ReturnedReports[0].Rewards[0].GrantedAmount, Is.EqualTo(5));
            Assert.That(loaded.ReturnedReports[0].Rewards[0].RemainingAmount, Is.EqualTo(2));
        }

        private static DispatchJob CreateJob(int startedAt, int requiredTime)
        {
            return new DispatchJob
            {
                JobId = System.Guid.NewGuid().ToString("N"),
                NpcId = "Odin",
                RegionId = "MossCave",
                StartedAtTotalTime = startedAt,
                RequiredTime = requiredTime,
                CompleteAtTotalTime = startedAt + requiredTime,
                State = DispatchState.Active,
                Requests = new List<DispatchResolvedRequest>(),
                Rewards = new List<DispatchRewardData>()
            };
        }
    }
}
