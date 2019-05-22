using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace BansheeGz.BGSpline.Editor
{
    //popup menu in SceneView
    public class BGPopupMenu
    {
        private const int HeaderHeight = 16;


        private readonly Texture2D backTexture;
        private readonly Texture2D selectedTexture;
        private readonly Texture2D currentTexture;

        private readonly string title;


        public Vector2 Point2DPosition;
        public Vector3 Point3DPosition;
        public bool Active;
        public MenuItem ActiveItem;

        private float height;
        private float width;
        private Rect targetRect;
        private BGTransition.SimpleTransition onTransition;

        private readonly List<MenuItem> items = new List<MenuItem>();

        private GUIStyle TitleStyle
        {
            get
            {
                return new GUIStyle("Label")
                {
                    fontStyle = FontStyle.Bold,
                    alignment = TextAnchor.MiddleCenter,
                    normal =
                    {
                        textColor = Color.white,
                        background = BGBinaryResources.BGBoxWithBorder123
                    },
                };
            }
        }

        public BGPopupMenu(string title)
        {
            this.title = title;
            backTexture = BGEditorUtility.Texture1X1(new Color32(46, 144, 168, 80));
            selectedTexture = BGEditorUtility.Texture1X1(new Color32(0, 255, 0, 100));
            currentTexture = BGEditorUtility.Texture1X1(new Color32(200, 200, 200, 200));
        }

        public void Add(MenuItem item)
        {
            items.Add(item);
        }

        public MenuItem Get(int index)
        {
            return items[index];
        }

        public virtual void On(Vector3 position)
        {
            if (Active) return;

            Active = true;

            onTransition = new BGTransition.SimpleTransition(.2, false);
            Point3DPosition = position;
            Point2DPosition = BGEditorUtility.GetSceneViewPosition(Point3DPosition);

            //target size (go first)
            height = width = 0;
            foreach (var size in items.Where(item => !item.Disabled).Select(item => item.Size))
            {
                if (height < size.y * 2) height = size.y * 2;
                width += size.x;
            }

            targetRect.size = new Vector2(width, height + HeaderHeight);


            //target position (go second)
            targetRect.x = Point2DPosition.x - targetRect.size.x * .5f;
            targetRect.y = Point2DPosition.y - targetRect.size.y * .75f;
        }

        public void OnGui(Event currentEvent)
        {
            if (!Active) return;

            var mousePosition = currentEvent.mousePosition;

            if (onTransition == null && !targetRect.Contains(mousePosition))
            {
                Active = false;
                SceneView.RepaintAll();
                return;
            }

            BGEditorUtility.HandlesGui(() =>
            {
                if (onTransition != null && !onTransition.Tick())
                {
                    //animating transition
                    GUI.DrawTexture(new Rect(Vector2.Lerp(Point2DPosition, targetRect.position, onTransition.Ratio), targetRect.size * onTransition.Ratio), backTexture, ScaleMode.StretchToFill);
                }
                else
                {
                    //ready
                    onTransition = null;

                    GUI.DrawTexture(targetRect, backTexture, ScaleMode.StretchToFill);
                    GUI.Label(new Rect(targetRect) {height = HeaderHeight}, title, TitleStyle);

                    ActiveItem = null;
                    var cursor = targetRect.x;

                    foreach (var item in items.Where(item => !item.Disabled))
                    {
                        var itemRect = new Rect(cursor, targetRect.y + HeaderHeight, item.Size.x, item.Size.y);
                        var selected = itemRect.Contains(mousePosition);

                        //if not separator
                        if (selected && item.Description != null)
                        {
                            ActiveItem = item;

                            if (!currentEvent.control && item is MenuItemButton) ((MenuItemButton) item).Action();
                        }

                        //icon
                        if (item.Icon != null)
                        {
                            GUI.DrawTexture(itemRect, BGBinaryResources.BGMenuItemBackground123, ScaleMode.StretchToFill);

                            if (selected) GUI.DrawTexture(itemRect, selectedTexture, ScaleMode.StretchToFill);

                            GUI.DrawTexture(itemRect, item.Icon, ScaleMode.StretchToFill);
                        }

                        if (item.Current)
                        {
                            GUI.DrawTexture(itemRect, currentTexture, ScaleMode.StretchToFill);
                        }

                        cursor += itemRect.width;
                    }
                }
            });

            if (!currentEvent.control) Active = false;
        }

        //===================================================================================  menu items
        //---------------------- abstract
        public abstract class MenuItem
        {
            public abstract string Description { get; }
            public abstract Texture2D Icon { get; }
            public abstract Vector2 Size { get; }

            public bool Disabled;

            public bool Current;
        }

        //---------------------- separator
        public class MenuSeparator : MenuItem
        {
            private readonly Vector2 size = new Vector2(16, 32);

            public override string Description
            {
                get { return null; }
            }

            public override Texture2D Icon
            {
                get { return null; }
            }

            public override Vector2 Size
            {
                get { return size; }
            }
        }

        //---------------------- menu item
        public class MenuItemButton : MenuItem
        {
            public readonly Action Action;

            private readonly Func<Texture2D> iconTexture;
            private readonly string description;
            private readonly Vector2 size = new Vector2(32, 32);


            public override string Description
            {
                get { return description; }
            }

            public override Texture2D Icon
            {
                get { return iconTexture(); }
            }

            public override Vector2 Size
            {
                get { return size; }
            }

            public MenuItemButton(Func<Texture2D> iconTexture, string description, Action action)
            {
                this.iconTexture = iconTexture;
                this.description = description;
                Action = action;
            }
        }
    }
}