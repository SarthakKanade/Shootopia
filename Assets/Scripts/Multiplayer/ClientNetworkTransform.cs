using Unity.Netcode.Components;
using UnityEngine;

namespace Unity.Netcode.Components
{
    /// <summary>
    /// Custom NetworkTransform that overrides authority to allow client-authoritative
    /// movement and provides jitter-free 2-tick snapshot interpolation for remote players.
    /// </summary>
    [DisallowMultipleComponent]
    public class ClientNetworkTransform : NetworkTransform
    {
        /// <summary>
        /// 2-tick interpolation delay (~66ms at 30 TPS) gives remote clients a smooth cushion
        /// to absorb packet arrival jitter without buffer starvation or sudden snapping.
        /// </summary>
        public const int TargetInterpolationTickOffset = 2;

        protected override void Awake()
        {
            AuthorityMode = AuthorityModes.Owner;
            InterpolationBufferTickOffset = TargetInterpolationTickOffset;

            ConfigureInterpolation();
            base.Awake();
        }

        private void ConfigureInterpolation()
        {
            Interpolate = true;
            AutoOwnerAuthorityTickOffset = true;

            // SmoothDampening eliminates linear abrupt stops and absorbs velocity changes smoothly
            PositionInterpolationType = InterpolationTypes.SmoothDampening;
            RotationInterpolationType = InterpolationTypes.SmoothDampening;
            PositionMaxInterpolationTime = 0.25f;
            RotationMaxInterpolationTime = 0.25f;
            PositionLerpSmoothing = true;
            RotationLerpSmoothing = true;

            // Absolute updates rather than unreliable deltas eliminate desync snaps on dropped packets
            UseUnreliableDeltas = false;
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
