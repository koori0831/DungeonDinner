using UnityEngine;
using Work.Cook.Code.Runtime.Core;
using Work.Cook.Code.Runtime.Events;
using Work.Cook.Code.Runtime.Integration;
using Work.Cook.Code.Runtime.Systems;
using Work.Cook.Code.Runtime.UI;
using Work.Core.EventBus;

namespace Work.Cook.Code.Runtime.Systems
{
    public sealed class CookingRewardWallet : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingBalance;
        [SerializeField] private bool loadFromPlayerPrefsOnAwake = true;
        [SerializeField] private bool saveToPlayerPrefs = true;
        [SerializeField] private string playerPrefsKey = "DungeonDinner.CookingRewardBalance";
        private bool _initialized;
        private int _balance;

        public int StartingBalance => Mathf.Max(0, startingBalance);
        public string PlayerPrefsKey => playerPrefsKey;
        public bool HasSavedPlayerPrefs => string.IsNullOrWhiteSpace(playerPrefsKey) == false
                                           && PlayerPrefs.HasKey(playerPrefsKey);
        public int Balance
        {
            get
            {
                EnsureInitialized();
                return _balance;
            }
        }

        private void Awake()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized == true)
                return;

            _balance = Mathf.Max(0, startingBalance);

            if (loadFromPlayerPrefsOnAwake == true && string.IsNullOrWhiteSpace(playerPrefsKey) == false)
                _balance = Mathf.Max(0, PlayerPrefs.GetInt(playerPrefsKey, _balance));

            _initialized = true;
            NotifyBalanceChanged();
        }

        public int Grant(int amount)
        {
            EnsureInitialized();

            amount = Mathf.Max(0, amount);
            if (amount <= 0)
                return _balance;

            _balance += amount;
            Save();
            NotifyBalanceChanged();
            return _balance;
        }

        public void SetBalanceForDebug(int value)
        {
            EnsureInitialized();

            _balance = Mathf.Max(0, value);
            Save();
            NotifyBalanceChanged();
        }

        public void ClearForDebug()
        {
            EnsureInitialized();

            _balance = Mathf.Max(0, startingBalance);

            if (saveToPlayerPrefs == true && string.IsNullOrWhiteSpace(playerPrefsKey) == false)
            {
                PlayerPrefs.DeleteKey(playerPrefsKey);
                PlayerPrefs.Save();
            }

            NotifyBalanceChanged();
        }

        public string BuildDebugSummary()
        {
            EnsureInitialized();

            return $"balance={_balance}, startingBalance={StartingBalance}, prefsKey={playerPrefsKey}, " +
                   $"hasSavedPrefs={HasSavedPlayerPrefs}";
        }

        private void EnsureInitialized()
        {
            if (_initialized == false)
                Initialize();
        }

        private void Save()
        {
            if (saveToPlayerPrefs == false || string.IsNullOrWhiteSpace(playerPrefsKey))
                return;

            PlayerPrefs.SetInt(playerPrefsKey, _balance);
            PlayerPrefs.Save();
        }

        private void NotifyBalanceChanged()
        {
            Bus<CookingRewardBalanceChangedEvent>.Raise(new CookingRewardBalanceChangedEvent(this, _balance));
        }
    }
}
