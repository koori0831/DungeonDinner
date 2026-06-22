using Work.Core.EventBus;
using Work.Input.Code;
using Work.Entities.Code;
using Work.Players.Code.Inventory;

namespace Work.Players.Code
{
    public class Player : Entity
    {
        private PlayerInputContainer _inputContainer;
        private EntityMovementModule _movementModule;
        private PlayerInventoryModule _inventoryModule;
        private bool _hasStarted;

        public PlayerInputContainer InputContainer => _inputContainer;
        public EntityMovementModule MovementModule => _movementModule ??= GetModule<EntityMovementModule>(true);
        public PlayerInventoryModule InventoryModule => _inventoryModule ??= GetModule<PlayerInventoryModule>(true);

        private void Awake()
        {
            _inputContainer = new PlayerInputContainer();
            _inputContainer.Initialize();
            Init();

            _movementModule = GetModule<EntityMovementModule>(true);
        }

        private void OnEnable()
        {
            if (_hasStarted == true)
            {
                RaisePlayerTargetRegistered();
            }
        }

        private void Start()
        {
            _hasStarted = true;
            RaisePlayerTargetRegistered();
        }

        private void OnDisable()
        {
            if (_hasStarted == false)
            {
                return;
            }

            Bus<PlayerTargetChangedEvent>.Raise(new PlayerTargetChangedEvent(transform, false));
        }

        private void OnDestroy()
        {
            _inputContainer?.Uninitialize();
        }

        private void RaisePlayerTargetRegistered()
        {
            Bus<PlayerTargetChangedEvent>.Raise(new PlayerTargetChangedEvent(transform, true));
        }
    }
}
