using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Chỉ quản lý graph conveyor. Không chứa logic mesh hoặc movement.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(ConveyorSpline))]
    public sealed class ConveyorConnections : MonoBehaviour
    {
        public List<ConveyorSpline> next = new List<ConveyorSpline>();
        public ConveyorGrinder terminalGrinder;

        static readonly HashSet<ConveyorConnections> Registry =
            new HashSet<ConveyorConnections>();

        ConveyorSpline _owner;
        ConveyorSwitch _switch;

        public ConveyorSpline Owner =>
            _owner != null ? _owner : (_owner = GetComponent<ConveyorSpline>());

        void OnEnable()
        {
            Registry.Add(this);
        }

        void OnDisable()
        {
            Registry.Remove(this);
        }

        void OnValidate()
        {
            if (next == null)
                next = new List<ConveyorSpline>();

            for (int i = next.Count - 1; i >= 0; i--)
            {
                if (next[i] == Owner)
                    next[i] = null;
            }

            if (!Application.isPlaying)
                RebuildNetwork(Owner);
        }

        /// <summary>
        /// Target movement hiện tại. Switch có từ hai nhánh hợp lệ sẽ quyết định target;
        /// conveyor thường lấy target hợp lệ đầu tiên.
        /// </summary>
        public bool TryGetNext(out ConveyorSpline target)
        {
            if (next == null)
            {
                target = null;
                return false;
            }

            _switch = _switch != null ? _switch : GetComponent<ConveyorSwitch>();
            if (_switch != null && _switch.ValidBranchCount >= 2 &&
                _switch.TryGetActiveNext(out target))
                return true;

            for (int i = 0; i < next.Count; i++)
            {
                if (next[i] == null)
                    continue;

                target = next[i];
                return true;
            }

            target = null;
            return false;
        }

        /// <summary>
        /// Trả các nhánh cần render. Runtime switch chỉ render connector của nhánh active;
        /// edit mode render tất cả để dễ bố trí level.
        /// </summary>
        public void GetRenderTargets(List<ConveyorSpline> results)
        {
            results.Clear();
            if (next == null)
                return;

            _switch = _switch != null ? _switch : GetComponent<ConveyorSwitch>();
            if (Application.isPlaying && _switch != null && _switch.ValidBranchCount >= 2)
            {
                if (_switch.TryGetActiveNext(out ConveyorSpline active))
                    results.Add(active);
                return;
            }

            for (int i = 0; i < next.Count; i++)
            {
                ConveyorSpline target = next[i];
                if (target == null || target == Owner || results.Contains(target))
                    continue;

                results.Add(target);
            }
        }

        public bool IsRouteActive(ConveyorSpline target)
        {
            if (next == null || target == null || !next.Contains(target))
                return false;

            if (!Application.isPlaying)
                return true;

            _switch = _switch != null ? _switch : GetComponent<ConveyorSwitch>();
            if (_switch == null || _switch.ValidBranchCount < 2)
                return true;

            return _switch.TryGetActiveNext(out ConveyorSpline active) && active == target;
        }

        public static void GetIncomingSources(ConveyorSpline target, List<ConveyorSpline> results)
        {
            results.Clear();
            if (target == null)
                return;

            foreach (ConveyorConnections connection in Registry)
            {
                if (connection == null || connection.Owner == null || connection.Owner == target)
                    continue;
                if (!connection.IsRouteActive(target))
                    continue;

                results.Add(connection.Owner);
            }
        }

        /// <summary>
        /// Rebuild conveyor hiện tại, các target và các source đang đi vào nó.
        /// Dùng khi đổi switch hoặc sửa connection.
        /// </summary>
        public static void RebuildNetwork(ConveyorSpline conveyor)
        {
            if (conveyor == null)
                return;

            HashSet<ConveyorBeltRenderer> renderers = new HashSet<ConveyorBeltRenderer>();
            AddRenderer(conveyor, renderers);

            ConveyorConnections connection = conveyor.GetComponent<ConveyorConnections>();
            if (connection != null && connection.next != null)
            {
                for (int i = 0; i < connection.next.Count; i++)
                    AddRenderer(connection.next[i], renderers);
            }

            foreach (ConveyorConnections candidate in Registry)
            {
                if (candidate != null && candidate.next != null &&
                    candidate.next.Contains(conveyor))
                    AddRenderer(candidate.Owner, renderers);
            }

            foreach (ConveyorBeltRenderer renderer in renderers)
                renderer.InvalidateConnectionRoutes();

            foreach (ConveyorBeltRenderer renderer in renderers)
                renderer.RebuildMeshAndMaterials();
        }

        static void AddRenderer(ConveyorSpline conveyor,
            HashSet<ConveyorBeltRenderer> renderers)
        {
            if (conveyor == null)
                return;

            ConveyorBeltRenderer renderer = conveyor.GetComponent<ConveyorBeltRenderer>();
            if (renderer != null)
                renderers.Add(renderer);
        }
    }
}
