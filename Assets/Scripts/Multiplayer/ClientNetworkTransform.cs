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

            // Lerp interpolation with 0.12s cushion provides stable, constant-velocity motion without rubberbanding
            PositionInterpolationType = InterpolationTypes.Lerp;
            RotationInterpolationType = InterpolationTypes.Lerp;
            PositionMaxInterpolationTime = 0.12f;
            RotationMaxInterpolationTime = 0.12f;
            PositionLerpSmoothing = true;
            RotationLerpSmoothing = true;

            UseUnreliableDeltas = true;
        }

        protected override bool OnIsServerAuthoritative()
        {
            return false;
        }
    }
}
