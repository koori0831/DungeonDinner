using Work.Input.Code;
using Work.Entities.Code;

namespace Work.Players.Code
{
    public class Player : Entity
    {
        private PlayerInputContainer _inputContainer;
        private EntityMovementModule _movementModule;

        public PlayerInputContainer InputContainer => _inputContainer;
        public EntityMovementModule MovementModule => _movementModule ??= GetModule<EntityMovementModule>(true);

        private void Awake()
        {
            _inputContainer = new PlayerInputContainer();
            _inputContainer.Initialize();
            Init();

            _movementModule = GetModule<EntityMovementModule>(true);
        }

        private void OnDestroy()
        {
            _inputContainer?.Uninitialize();
        }
    }
}
