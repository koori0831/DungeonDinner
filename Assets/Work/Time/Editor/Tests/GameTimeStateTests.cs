using NUnit.Framework;

namespace Work.TimeSystem.Editor.Tests
{
    public sealed class GameTimeStateTests
    {
        [TestCase(0, 1, 0)]
        [TestCase(3, 1, 3)]
        [TestCase(6, 2, 0)]
        [TestCase(7, 2, 1)]
        public void Constructor_CalculatesDayAndTimeFromTotal(int total, int expectedDay, int expectedTime)
        {
            GameTimeState state = new GameTimeState(total);

            Assert.That(state.CurrentDay, Is.EqualTo(expectedDay));
            Assert.That(state.CurrentTimeOfDay, Is.EqualTo(expectedTime));
        }

        [Test]
        public void Advance_CrossesDayWithoutDiscardingOverflow()
        {
            GameTimeState state = new GameTimeState(4);

            GameTimeChange change = state.Advance(3);

            Assert.That(state.TotalElapsedTime, Is.EqualTo(7));
            Assert.That(state.CurrentDay, Is.EqualTo(2));
            Assert.That(state.CurrentTimeOfDay, Is.EqualTo(1));
            Assert.That(change.DidDayChange, Is.True);
        }

        [Test]
        public void Advance_RejectsNonPositiveAmounts()
        {
            GameTimeState state = new GameTimeState();

            Assert.That(() => state.Advance(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
            Assert.That(() => state.Advance(-1), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }
    }
}
