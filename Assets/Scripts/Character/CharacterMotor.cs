using System.Collections;
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
        public float moveSpeed = 5f;
        public float rotationSpeed = 900f;
        public float gravity = -24f;
        public float jumpForce = 2.2f;
        public float dashSpeed = 14.5f;
        public float rollSpeed = 14.5f;
        public float rollDuration = 0.55f;
        public float animDamp = 0.1f;

        public CharacterController Controller { get; private set; }
        public Animator Animator => animator;
        public PlayerInputRouter InputRouter => input;
        public IAimProvider Aim { get; private set; }
        public Transform ModelRoot => modelRoot != null ? modelRoot : transform;
        
        public float VerticalVelocity { get; set; }
        public float MoveSpeedMultiplier { get; set; } = 1f;
        private readonly NetworkVariable<bool> _isInLava = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
        public bool IsInLava
        {
            get => _isInLava.Value;
            set
            {
                if (IsOwner) _isInLava.Value = value;
            }
        }
        public bool IsGrounded => Controller.isGrounded;
        public PlayerInputIntent InputIntent => input != null ? input.Intent : default;
        public bool IsMoving => InputIntent.Move.sqrMagnitude > 0.01f;
        public float Gravity => gravity;
        public float JumpForce => jumpForce;
        public float DashSpeed => dashSpeed;
        public float RollSpeed => rollSpeed;
        public float RollDuration => rollDuration;

        [Header("Audio Configuration")]
        [SerializeField] private PlayerAudioSO playerAudioConfig;
        [SerializeField] private float footstepInterval = 0.32f;
        private float _footstepTimer = 0f;
        private int _footstepIndex = 0;

        private readonly NetworkVariable<Quaternion> _modelRotation = new NetworkVariable<Quaternion>(Quaternion.identity, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

        public PlayerAudioSO AudioConfig => playerAudioConfig;
        
        public void SyncModelRotation()
        {
            if (IsOwner) _modelRotation.Value = ModelRoot.rotation;
        }

        // FSM Context
        public BaseState CurrentState { get; set; }
        public bool IsInCombat { get; set; } = false;
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
            float dt = Time.deltaTime;
            UpdateFootsteps(dt);

            // Handle fall animation when sinking in lava across all clients
            if (_isInLava.Value && animator != null)
            {
                animator.SetBool(HashGrounded, false);
                animator.SetBool(HashIsFiring, false);
                var health = GetComponent<PlayerHealth>();
                bool isDead = health != null && health.IsDead;

                if (!isDead)
                {
                    bool isCurrentJumpAir = animator.GetCurrentAnimatorStateInfo(0).IsName("JumpAir");
                    bool isNextJumpAir = animator.IsInTransition(0) && animator.GetNextAnimatorStateInfo(0).IsName("JumpAir");

                    if (!isCurrentJumpAir && !isNextJumpAir)
                    {
                        animator.CrossFadeInFixedTime("JumpAir", 0.15f, 0);
                    }
                }
            }

            // Only the owning client drives FSM, input, and animator triggers.
            if (!IsOwner)
            {
                ModelRoot.rotation = _modelRotation.Value;
                return;
            }
            if (input == null) return;

            // Handle independent upper body logic directly
            if (animator != null)
            {
                if (InputIntent.Reload) animator.SetTrigger(HashReload);
                if (InputIntent.SwapWeapon) animator.SetTrigger(HashSwapWeapon);
            }

            CurrentState?.UpdateStates(dt);
        }

        public void PlayJumpAudio(float delay = 0.11f)
        {
            // Jump sound removed per design request
        }

        private IEnumerator DelayedJumpAudioRoutine(float delay)
        {
            yield break;
        }

        private void TriggerJumpAudio()
        {
            // Jump sound removed per design request
        }

        public void PlayDashAudio()
        {
            if (AudioManager.Instance == null) return;
            AudioClip clip = (playerAudioConfig != null && playerAudioConfig.dashClip != null) ? playerAudioConfig.dashClip : null;
            if (clip != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, 0.75f);
            }
        }

        public void PlayRollAudio()
        {
            if (AudioManager.Instance == null) return;
            AudioClip clip = (playerAudioConfig != null && playerAudioConfig.rollClip != null) ? playerAudioConfig.rollClip : null;
            if (clip != null)
            {
                AudioManager.Instance.PlaySFXAtPosition(clip, transform.position, 0.70f);
            }
        }

        private void UpdateFootsteps(float dt)
        {
            // Walk / footstep sounds removed per design request
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
                float activeRotSpeed = (IsInCombat || (animator != null && animator.GetBool(HashIsFiring))) ? 1400f : rotationSpeed;
                ModelRoot.rotation = Quaternion.RotateTowards(ModelRoot.rotation, target, activeRotSpeed * dt);
                _modelRotation.Value = ModelRoot.rotation;
            }

            Vector3 localMove = ModelRoot.InverseTransformDirection(worldMove);
            if (animator != null)
            {
                animator.SetFloat(HashMoveX, localMove.x, animDamp, dt);
                animator.SetFloat(HashMoveY, localMove.z, animDamp, dt);
                animator.SetFloat(HashSpeed, worldMove.magnitude, animDamp, dt);
            }

            Vector3 velocity = worldMove * (moveSpeed * speedMultiplier * MoveSpeedMultiplier) + Vector3.up * VerticalVelocity;
            Controller.Move(velocity * dt);

            // Immediate zero-latency ground contact detection
            if (Controller.isGrounded && VerticalVelocity <= 0f && !_isInLava.Value)
            {
                if (animator != null)
                {
                    animator.SetBool(HashGrounded, true);
                }
                if (CurrentState is AirborneState)
                {
                    CurrentState.ExitState();
                    CurrentState = _factory.Grounded();
                    CurrentState.EnterState();
                    VerticalVelocity = -2f;
                }
            }
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
                        cinemachineCam.Target.TrackingTarget = target;
                        cinemachineCam.Target.LookAtTarget = null;
                        cinemachineCam.PreviousStateIsValid = false;
                        Debug.Log("[CharacterMotor] Set CinemachineCamera follow target to CameraTarget");
                    }

                    // Ensure ThirdPersonCameraController exists on the camera
                    if (cinemachineCam.GetComponent<ThirdPersonCameraController>() == null)
                    {
                        cinemachineCam.gameObject.AddComponent<ThirdPersonCameraController>();
                        Debug.Log("[CharacterMotor] Auto-added ThirdPersonCameraController to CinemachineCamera");
                    }
                }

                // If spawned in a gameplay arena and elevation is at or below void level, snap immediately to spawn point
                string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                if (MapRegistry.IsGameplayMap(sceneName))
                {
                    float voidThreshold = MapRegistry.GetVoidThreshold(sceneName);
                    if (transform.position.y <= voidThreshold && SpawnPointManager.Instance != null)
                    {
                        var sp = SpawnPointManager.Instance.GetSpawnPoint(SpawnPointType.FFA);
                        if (sp != null)
                        {
                            if (Controller != null) Controller.enabled = false;
                            transform.position = sp.position;
                            transform.rotation = sp.rotation;
                            if (Controller != null) Controller.enabled = true;
                            VerticalVelocity = 0f;
                            IsInLava = false;
                        }
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
