using System.Collections.Generic;
using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Liên kết định tuyến giữa các băng chuyền: item chạy hết băng này (cuối spline, t=1)
    /// sẽ đi tiếp sang các băng trong <see cref="next"/>.
    /// - 1 phần tử  = nối tiếp thẳng (Connector)
    /// - nhiều phần tử = Splitter (chia nhánh)
    /// - nhiều băng cùng trỏ về 1 băng = Merger (tự nhiên)
    /// Gắn CHUNG GameObject với <see cref="ConveyorSpline"/>.
    /// </summary>
    [RequireComponent(typeof(ConveyorSpline))]
    public class ConveyorConnections : MonoBehaviour
    {
        [Tooltip("Các băng chuyền item đi tiếp tới sau khi hết băng này. Rỗng = đích cuối.")]
        public List<ConveyorSpline> next = new List<ConveyorSpline>();
    }
}
