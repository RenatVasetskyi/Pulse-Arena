using UnityEngine;

namespace Game.Combat
{
    /// <summary>
    ///     Serialized seam between the authored lasso prefab and <see cref="RopeRenderer" />: hands the renderer the
    ///     two inspector-wired <see cref="LineRenderer" />s so the rope's look (material, caps, tiling) lives on the
    ///     asset. Passive — width, colour and positions are still driven per frame by the renderer.
    /// </summary>
    public sealed class LassoRopeView : MonoBehaviour
    {
        [SerializeField] private LineRenderer _line;
        [SerializeField] private LineRenderer _wrapRing;

        public LineRenderer Line => _line;

        public LineRenderer WrapRing => _wrapRing;
    }
}
