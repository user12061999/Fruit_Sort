using UnityEngine;
using UnityEngine.UI;

namespace HAVIGAME.UI {
    [ExecuteInEditMode()]
    [AddComponentMenu("HAVIGAME/UI/Round Layout Group", 30)]
    public class RoundLayoutGroup : LayoutGroup {
        public Vector2 cellSize;
        [Range(0f, 360f)] public float childSpace;
        public bool clockwise;
        public float radius;
        [Range(0f, 360f)] public float startAngle;
        public int groupMember = 1;
        [Range(0f, 360f)] public float groupSpace;
        private float rotateAngle;

        public float RotateAngle {
            get { return rotateAngle; }
            set {
                rotateAngle = value;
                CalculateLayoutInputRound();
            }
        }

        protected override void OnEnable() {
            base.OnEnable();
            CalculateLayoutInputRound();
        }

        public override void SetLayoutHorizontal() {

        }
        public override void SetLayoutVertical() {

        }

        public override void CalculateLayoutInputVertical() {
            CalculateLayoutInputRound();
        }
        public override void CalculateLayoutInputHorizontal() {
            CalculateLayoutInputRound();
        }

        protected void CalculateLayoutInputRound() {
            if (transform.childCount == 0)
                return;

            m_Tracker.Clear();

            float cellAngle = startAngle + rotateAngle;

            for (int i = 0; i < transform.childCount; i++) {
                RectTransform childRect = (RectTransform)transform.GetChild(i);
                if (childRect != null) {
                    //Adding the elements to the tracker stops the user from modifiying their positions via the editor.
                    m_Tracker.Add(this, childRect,
                    DrivenTransformProperties.Anchors |
                    DrivenTransformProperties.AnchoredPosition |
                    DrivenTransformProperties.Pivot);

                    float cellRadian = cellAngle * Mathf.Deg2Rad;
                    Vector3 childPosition = new Vector3(
                                                        Mathf.Cos(cellRadian),
                                                        Mathf.Sin(cellRadian),
                                                        0);
                    childRect.localPosition = childPosition * radius;

                    //Force objects to be center aligned, this can be changed however I'd suggest you keep all of the objects with the same anchor points.
                    childRect.anchorMin = childRect.anchorMax = childRect.pivot = new Vector2(0.5f, 0.5f);
                    childRect.sizeDelta = cellSize;
                    if (clockwise) {
                        cellAngle -= childSpace;
                        if ((i + 1) % groupMember == 0) {
                            cellAngle -= groupSpace;
                        }
                    } else {
                        cellAngle += childSpace;
                        if ((i + 1) % groupMember == 0) {
                            cellAngle += groupSpace;
                        }
                    }
                }
            }
        }
    }
}