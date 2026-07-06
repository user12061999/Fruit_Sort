using UnityEngine;

namespace FruitSort
{
    /// <summary>
    /// Load <see cref="LevelData"/> thành scene lúc runtime. Đặt 1 cái trong scene gameplay
    /// (cùng scene với FallingPixelManager/GamePlayManager), gán level và bấm Play.
    /// Bucket/ConveyorSpline tự register vào FallingPixelManager khi OnEnable.
    /// </summary>
    public class LevelLoader : MonoBehaviour
    {
        [Tooltip("Level sẽ dựng khi Start.")]
        public LevelData level;
        [Tooltip("Tự dựng level trong Start(). Tắt nếu muốn tự gọi Load() (VD: qua menu chọn level).")]
        public bool loadOnStart = true;

        GameObject _root;

        /// <summary>Root của level đang dựng (null nếu chưa load).</summary>
        public GameObject Root => _root;

        void Start()
        {
            if (loadOnStart) Load();
        }

        /// <summary>Dựng lại level (xoá bản cũ nếu có). Gọi Load(khác) để đổi level runtime.</summary>
        public void Load() { Load(level); }

        public void Load(LevelData data)
        {
            if (data == null)
            {
                Debug.LogError("[LevelLoader] Chưa gán LevelData.", this);
                return;
            }
            level = data;
            Clear();
            _root = LevelBuilder.Build(data);
        }

        public void Clear()
        {
            if (_root == null) return;
            if (Application.isPlaying) Destroy(_root);
            else DestroyImmediate(_root);
            _root = null;
        }
    }
}
