using System;
using UnityEngine;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;

namespace BansheeGz.BGSpline.Editor
{
    public class BGTreeView<T> where T : BGTreeNode<T>
    {
        public readonly List<T> Roots = new List<T>();

        public Config Configuration { get; private set; }

        private Texture2D linkTexture;
        private Texture2D expandedIconTexture;
        private Texture2D collapsedIconTexture;

        public Texture2D ExpandedIconTexture
        {
            get { return expandedIconTexture ?? (expandedIconTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGExpanded123)); }
        }

        public Texture2D CollapsedIconTexture
        {
            get { return collapsedIconTexture ?? (collapsedIconTexture = BGEditorUtility.LoadTexture2D(BGEditorUtility.Image.BGCollapsed123)); }
        }

        public BGTreeView(Config config)
        {
            Configuration = config;
        }

        public virtual void OnInspectorGui()
        {
            IterateRoots((i, root) =>
            {
                if (i != 0) GUILayout.Space(Configuration.VerticalSpace*2);
                root.OnInspectorGui();
            });
        }

        public void ExpandCollapseAll(bool collapsed)
        {
            IterateRoots((i, root) => root.ExpandCollapsed(collapsed, true));
        }

        protected void IterateRoots(Action<int, T> action)
        {
            for (var i = 0; i < Roots.Count; i++) action(i, Roots[i]);
        }

        public virtual Texture2D GetLinkTexture(int level)
        {
            BGEditorUtility.Assign(ref linkTexture, () => BGEditorUtility.Texture1X1(Color.black));
            return linkTexture;
        }

        //=========================================================  Config
        public class Config
        {
            public int VerticalSpace { get; private set; }
            public int HorizontalLinkOffset { get; private set; }
            public int HorizontalSpace { get; private set; }
            public int LinkLineSize { get; private set; }

            public int ExpandCollapseIconSize { get; private set; }

            public Config(int verticalSpace, int horizontalSpace, int horizontalLinkOffset, int linkLineSize, int expandCollapseIconSize)
            {
                VerticalSpace = verticalSpace;
                HorizontalLinkOffset = horizontalLinkOffset;
                HorizontalSpace = horizontalSpace;
                LinkLineSize = linkLineSize;
                ExpandCollapseIconSize = expandCollapseIconSize;
            }
        }
    }


    //=========================================================  Node
    //curiously recurring template pattern (something wrong here)
    public abstract class BGTreeNode<T> where T : BGTreeNode<T>
    {
        private readonly BGTreeView<T> tree;
        private T parent;
        private List<T> children;

        private GUIStyle buttonStyle;

        public virtual bool Collapsed { get; set; }

        public Rect Rect { get; set; }

        public bool HasChildren
        {
            get { return !BGEditorUtility.Empty(children); }
        }

        private List<Action> postActions;

        public T Parent
        {
            get { return parent; }
            set
            {
                if (parent == value) return;

                //remove from old parent
                if (parent != null && parent.children != null) parent.children.Remove((T) this);

                parent = value;
                if (parent == null) return;

                //recursion check
                var currentParent = parent;
                var nestingLevel = 0;
                while (currentParent != null)
                {
                    if (currentParent == this) throw new BGCc.CcException("Recursion check fail!");
                    if (nestingLevel > 10) throw new BGCc.CcException("Recursion check fail! Unacceptable Nesting Level " + nestingLevel);

                    currentParent = currentParent.Parent;
                    nestingLevel++;
                }

                //add to parent
                parent.children = parent.children ?? new List<T>();
                if (!parent.children.Contains((T) this)) parent.children.Add((T) this);
            }
        }

        public int Level
        {
            get
            {
                if (Parent == null) return 0;

                var parentLevel = Parent.Level;
                //just in case (recursion check)
                if (parentLevel > 10) throw new BGCc.CcException("Recursion check fail! Unacceptable Nesting Level " + parentLevel);
                return parentLevel + 1;
            }
        }

        public BGTreeView<T> Tree
        {
            get { return tree; }
        }


        protected BGTreeNode(BGTreeView<T> tree)
        {
            if (tree == null) throw new BGCc.CcException("tree can not be null!");
            this.tree = tree;
        }

        public void ExpandCollapsed(bool collapsed, bool recursive)
        {
            Collapsed = collapsed;
            if (recursive) IterateChildren(child => child.ExpandCollapsed(collapsed, true));
        }

        protected void IterateChildren(Action<T> action)
        {
            if (BGEditorUtility.Empty(children)) return;

            foreach (var child in children) action(child);
        }


        public virtual Rect OnInspectorGui()
        {
            var level = Level;

            var myRect = new Rect();
            BGEditorUtility.Vertical(() =>
            {
                BGEditorUtility.Horizontal(() =>
                {
                    if (level > 0 && tree.Configuration.HorizontalSpace > 0) GUILayout.Space(tree.Configuration.HorizontalSpace*level);

                    BGEditorUtility.Vertical(() => { OnInspectorGuiInternal(level); });

                    myRect = GUILayoutUtility.GetLastRect();
                });
            });

            if (Event.current.type == EventType.Repaint) Rect = myRect;

            if (children == null || children.Count == 0 || Collapsed) return myRect;


            //Children
            var texture = tree.GetLinkTexture(level);

            var linkStartX = myRect.x + tree.Configuration.HorizontalLinkOffset;

            var childRect = new Rect();

            var linkSize = tree.Configuration.LinkLineSize;

            if (postActions != null) postActions.Clear();
            foreach (var child in children)
            {
                if (tree.Configuration.VerticalSpace > 0) GUILayout.Space(tree.Configuration.VerticalSpace);

                childRect = child.OnInspectorGui();

                //horizontal link
                var linkWidth = childRect.x - linkStartX;

                if (!(linkWidth > 0)) continue;

                //child Y Center
                var childCenterY = childRect.y + childRect.size.y*.5f;

                //link
                var horizontalLink = new Rect(
                    linkStartX,
                    childCenterY - linkSize*.5f,
                    linkWidth,
                    linkSize);

                GUI.DrawTexture(horizontalLink, texture, ScaleMode.StretchToFill);

                //icon
                var iconSize = tree.Configuration.ExpandCollapseIconSize;
                if (iconSize > 0 && child.HasChildren)
                {
                    BGEditorUtility.Assign(ref buttonStyle, () => new GUIStyle("Button")
                    {
                        margin = new RectOffset(),
                        padding = new RectOffset(),
                        border = new RectOffset()
                    });

                    postActions = postActions ?? new List<Action>();
                    var childRef = child;
                    postActions.Add(() =>
                    {
                        var iconSizeHalf = iconSize*.5f;
                        if (GUI.Button(new Rect(
                            linkStartX - iconSizeHalf + linkSize*.5f,
                            childCenterY - iconSizeHalf + linkSize*.5f,
                            iconSize,
                            iconSize),
                            childRef.Collapsed ? tree.CollapsedIconTexture : tree.ExpandedIconTexture, buttonStyle))
                        {
                            childRef.Collapsed = !childRef.Collapsed;
                        }
                    });
                }
            }

            if (linkStartX < childRect.x)
            {
                //vertical link
                GUI.DrawTexture(new Rect(
                    linkStartX,
                    myRect.yMax,
                    linkSize,
                    childRect.center.y - myRect.yMax
                    ), texture, ScaleMode.StretchToFill);
            }


            if (postActions != null && postActions.Count > 0) foreach (var postAction in postActions) postAction();

            return myRect;
        }

        public abstract void OnInspectorGuiInternal(int level);
        public abstract void ProcessStructure();
    }
}