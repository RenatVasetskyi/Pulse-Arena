using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     The serialized seam between the authored lasso prefab and <see cref="RopeRenderer" />: it hands the
    ///     renderer the two <see cref="LineRenderer" />s the artist wired in the inspector, so the rope's look
    ///     (material, caps, corners, texture tiling) lives on the asset instead of being built in code.
    ///     Purely passive — width, colour and positions are still driven per frame by the renderer, because they
    ///     react to charge and rope tension.
    /// </summary>
    public sealed class LassoRopeView : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField] private LineRenderer _wrapRing;

        public LineRenderer Line => _line;

        public LineRenderer WrapRing => _wrapRing;
    }
}
