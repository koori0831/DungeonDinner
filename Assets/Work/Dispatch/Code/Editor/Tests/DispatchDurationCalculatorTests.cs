using System.Collections.Generic;
using NUnit.Framework;
using Work.Dispatch.Code.Runtime;

namespace Work.Dispatch.Code.Editor.Tests
{
    public sealed class DispatchDurationCalculatorTests
    {
        [Test]
        public void Calculate_UsesTravelBatchesAndNpcMultiplier()
        {
            DispatchDurationCalculator calculator = new DispatchDurationCalculator();
            List<DispatchWorkload> workloads = new List<DispatchWorkload>
            {
                new DispatchWorkload(amount: 5, amountPerBatch: 2, timePerBatch: 1)
            };

            DispatchDurationResult normal = calculator.Calculate(2, 100, workloads);
            DispatchDurationResult fast = calculator.Calculate(2, 80, workloads);
            DispatchDurationResult slow = calculator.Calculate(2, 120, workloads);

            Assert.That(normal.GatherTime, Is.EqualTo(3));
            Assert.That(normal.RequiredTime, Is.EqualTo(5));
            Assert.That(fast.RequiredTime, Is.EqualTo(4));
            Assert.That(slow.RequiredTime, Is.EqualTo(6));
        }

        [Test]
        public void Calculate_AddsWorkForMultipleMaterials()
        {
            DispatchDurationCalculator calculator = new DispatchDurationCalculator();
            List<DispatchWorkload> workloads = new List<DispatchWorkload>
            {
                new DispatchWorkload(5, 2, 1),
                new DispatchWorkload(4, 2, 1)
            };

            DispatchDurationResult result = calculator.Calculate(2, 100, workloads);

            Assert.That(result.GatherTime, Is.EqualTo(5));
            Assert.That(result.RequiredTime, Is.EqualTo(7));
        }
    }
}
