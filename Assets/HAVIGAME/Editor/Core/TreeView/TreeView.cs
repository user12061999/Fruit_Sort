using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace HAVIGAME.Editor {
    public class TreeViewItem {
        public const int indentPixel = 1;

        protected TreeViewItem parent;
        protected List<TreeViewItem> children;

        public Action<TreeViewItem> onSelected;
        public bool isExpanded;
        public GUIContent content;
        public bool HasChild => children.Count > 0;
        public virtual float GetHeight(int depth) {
            return EditorGUIUtility.singleLineHeight * Mathf.Max(1f, 1.72f - depth * 0.18f);
        }

        public TreeViewItem Parent => parent;
        public IEnumerable<TreeViewItem> GetChilds => children;

        public TreeViewItem(GUIContent content) {
            this.content = content;

            this.children = new List<TreeViewItem>();
            this.parent = null;
            this.isExpanded = false;
        }

        public void AddChild(TreeViewItem child) {
            children.Add(child);
            child.parent = this;
        }

        public virtual void OnGUI(int depth, TreeViewItem selectedItem, GUIStyle normalStyle, GUIStyle activeStyle) {

            if (depth >= 0) {
                float height = GetHeight(depth);

                GUILayout.BeginHorizontal();
                GUILayout.Space(indentPixel + depth * height);

                bool isCurrentSelected = selectedItem == this;

                if (GUILayout.Button(content, isCurrentSelected ? activeStyle : normalStyle, GUILayout.Height(height))) {
                    isExpanded = !isExpanded;
                    Select();
                }
                
                GUILayout.EndHorizontal();
            }

            if (depth < 0 || isExpanded) {
                foreach (TreeViewItem child in GetChilds) {
                    child.OnGUI(depth + 1, selectedItem, normalStyle, activeStyle);
                }
            }
        }

        public virtual void Select() {
            onSelected?.Invoke(this);
        }
    }

    public abstract class TreeView {
        protected TreeViewItem root;
        protected TreeViewItem selectedItem;
        protected GUIStyle normalStyle;
        protected GUIStyle activeStyle;

        public TreeView() {

        }

        protected abstract TreeViewItem BuildRoot();

        public void Build() {
            root = BuildRoot();

            foreach (var item in All(root)) {
                item.onSelected = OnItemSelected;
            }

            if (root.HasChild) {
                root.GetChilds.First().Select();
            }
        }

        public void OnGUI() {
            root.OnGUI(-1, selectedItem, GetNormalStyle(), GetActiveStyle());
        }

        protected virtual void OnItemSelected(TreeViewItem item) {
            selectedItem = item;
        }

        protected IEnumerable<TreeViewItem> All(TreeViewItem root) {
            yield return root;
            if (root.HasChild) {
                foreach (TreeViewItem child in root.GetChilds) {
                    foreach (var item in All(child)) {
                        yield return item;
                    }
                }
            }
        }

        protected virtual GUIStyle GetNormalStyle() {
            if (normalStyle != null) {
                return normalStyle;
            }

            GUIStyle style = EditorUtility.Styles.BoldLabel;

            normalStyle = style;
            return normalStyle;
        }

        protected virtual GUIStyle GetActiveStyle() {
            if (activeStyle != null) {
                return activeStyle;
            }
            GUIStyle style = EditorUtility.Styles.BoldLabel;
            style.normal.textColor 
                = style.active.textColor 
                = style.hover.textColor 
                = style.focused.textColor
                = style.onNormal.textColor 
                = style.onActive.textColor 
                = style.onHover.textColor 
                = style.onFocused.textColor 
                = Color.green;
            
            style.normal.background
                = style.active.background
                = style.hover.background
                = style.focused.background
                = style.onNormal.background
                = style.onActive.background
                = style.onHover.background
                = style.onFocused.background
                = Texture2D.grayTexture;

            activeStyle = style;
            return activeStyle;
        }
    }
}
