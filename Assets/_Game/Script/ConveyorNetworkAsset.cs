using System;
using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Bản lưu (ScriptableObject) của toàn bộ mạng băng chuyền: danh sách băng (knot world + bề rộng)
    /// và các liên kết định tuyến giữa chúng. Editor dùng để Save/Load mạng băng trong scene.
    /// Đây là định dạng dữ liệu thuần (không giữ tham chiếu GameObject) nên serialize sạch.
    /// </summary>
    [CreateAssetMenu(menuName = "LoopSort/Conveyor Network", fileName = "ConveyorNetwork")]
    public class ConveyorNetworkAsset : ScriptableObject
    {
        [Serializable]
        public class ConveyorData
        {
            public string name = "Conveyor";
            public float beltWidth = 3f;
            public List<Vector3> knots = new List<Vector3>(); // vị trí world của các knot
        }

        /// <summary>Liên kết: cuối băng <see cref="from"/> -> đầu băng <see cref="to"/> (index trong <see cref="conveyors"/>).</summary>
        [Serializable]
        public class LinkData
        {
            public int from;
            public int to;
        }

        public List<ConveyorData> conveyors = new List<ConveyorData>();
        public List<LinkData> links = new List<LinkData>();
    }
}
