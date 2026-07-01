using UnityEngine;
using Shootopia.FSM;
using Unity.Netcode;

namespace Shootopia
{
    [RequireComponent(typeof(CharacterController))]
    public class CharacterMotor : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInputRouter input;
        [SerializeField] private Animator animator;
        [SerializeField] private Transform modelRoot;
        [SerializeField] private MonoBehaviour aimProviderBehaviour;

        [Header("Movement")]
        public float moveSpeed = 4f;
        public float rotationSpeed = 900f;
        public float gravity = -20f;
        public float jumpForce = 8f;
        public float dashSpeed = 10f;
        public float rollSpeed = 8f;
        public float animDamp = 0.1f;

        public CharacterController Controller { get; private set; }
        public Animator Animator => animator;
        public PlayerInputRouter InputRouter => input;
        public IAimProvider Aim { get; private set; }
        public Transform ModelRoot => modelRoot != null ? modelRoot : transform;
        
        public float VerticalVelocity { get; set; }
        public bool IsGrounded => Controller.isGrounded;
        public PlayerInputIntent InputIntent => input != null ? input.Intent : default;
        public bool IsMoving => InputIntent.Move.sqrMagnitude > 0.01f;
        public float Gravity => gravity;
        public float JumpForce => jumpForce;
        public float DashSpeed => dashSpeed;
        public float RollSpeed => rollSpeed;

        private readonly NetworkVariable<Quaternion> _modelRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        // FSM Context
        public BaseState CurrentState { get; set; }
        private StateFactory _factory;

        public static readonly int HashMoveX = Animator.StringToHash("MoveX");
        public static readonly int HashMoveY = Animator.StringToHash("MoveY");
        public static readonly int HashSpeed = Animator.StringToHash("Speed");
        public static readonly int HashGrounded = Animator.StringToHash("IsGrounded");
        public static readonly int HashDash = Animator.StringToHash("Dash");
        public static readonly int HashRoll = Animator.StringToHash("Roll");
        public static readonly int HashJump = Animator.StringToHash("Jump");
        public static readonly int HashIsFiring = Animator.StringToHash("IsFiring");
        public static readonly int HashReload = Animator.StringToHash("Reload");
        public static readonly int HashSwapWeapon = Animator.StringToHash("SwapWeapon");

        public void SetAimProvider(IAimProvider provider) => Aim = provider;

        private void Awake()
        {
            Controller = GetComponent<CharacterController>();
            if (animator == null) animator = GetComponentInChildren<Animator>(true);
            if (aimProviderBehaviour is IAimProvider ap) Aim = ap;

            _factory = new StateFactory(this);
            CurrentState = _factory.Grounded();
            CurrentState.EnterState();
        }

        private void Update()
        {
            // Only the owning client drives FSM, input, and animator triggers.
            if (!IsOwner)
            {
                ModelRoot.rotation = _modelRotation.Value;
                return;
            }
            if (input == null) return;
            float dt = Time.deltaTime;

            // Handle independent upper body logic directly
            if (animator != null)
            {
                if (InputIntent.Reload) animator.SetTrigger(HashReload);
                if (InputIntent.SwapWeapon) animator.SetTrigger(HashSwapWeapon);
            }

            CurrentState?.UpdateStates(dt);
        }

        public void ApplyMovementAndRotation(float dt, float speedMultiplier = 1f)
        {
            Vector3 fwd = Aim != null ? Aim.CameraPlanarForward : Vector3.forward;
            Vector3 right = Aim != null ? Aim.CameraPlanarRight : Vector3.right;
            
            Vector3 worldMove = fwd * InputIntent.Move.y + right * InputIntent.Move.x;
            if (worldMove.sqrMagnitude > 1f) worldMove.Normalize();

            if (Aim != null && fwd.sqrMagnitude >= 0.0004f)
            {
                Quaternion target = Quaternion.LookRotation(fwd, Vector3.up);
                ModelRoot.rotation = Quaternion.RotateTowards(ModelRoot.rotation, target, rotationSpeed * dt);
                _modelRotation.Value = ModelRoot.rotation;
            }

            Vector3 localMove = ModelRoot.InverseTransformDirection(worldMove);
            if (animator != null)
            {
                animator.SetFloat(HashMoveX, localMove.x, animDamp, dt);
                animator.SetFloat(HashMoveY, localMove.z, animDamp, dt);
                animator.SetFloat(HashSpeed, worldMove.magnitude, animDamp, dt);
            }

            Vector3 velocity = worldMove * (moveSpeed * speedMultiplier) + Vector3.up * VerticalVelocity;
            Controller.Move(velocity * dt);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                var aimProvider = FindAnyObjectByType<CameraAimProvider>();
                if (aimProvider != null)
                {
                    SetAimProvider(aimProvider);
                }

                var cinemachineCam = FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
                if (cinemachineCam != null)
                {
                    var target = transform.Find("CameraTarget");
                    if (target != null)
                    {
                        cinemachineCam.Follow = target;
                        Debug.Log("[CharacterMotor] Set CinemachineCamera follow target to CameraTarget");
                    }
                }
            }
        }

        public void OnAnimationFinished(string animName)
        {
            CurrentState?.OnAnimationFinished(animName);
        }
    }
}
