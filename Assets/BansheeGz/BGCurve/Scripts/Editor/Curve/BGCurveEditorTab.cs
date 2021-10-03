using BansheeGz.BGSpline.Curve;
using UnityEditor;
using UnityEngine;

namespace BansheeGz.BGSpline.Editor
{
    public abstract class BGCurveEditorTab
    {
        public readonly BGCurveEditor Editor;
        public readonly SerializedObject SerializedObject;
        public readonly BGCurve Curve;

        public abstract Texture2D Header2D { get; }

        public BGCurveSettings Settings
        {
            get { return BGPrivateField.GetSettings(Curve); }
        }

        protected BGCurveEditorTab(BGCurveEditor editor, SerializedObject serializedObject)
        {
            Editor = editor;
            Curve = editor.Curve;
            SerializedObject = serializedObject;
        }


        // standard onInspector call
        public abstract void OnInspectorGui();

        //standard onscene
        public virtual void OnSceneGui(Plane[] frustum)
        {
        }


        // standard onEnable
        public virtual void OnEnable()
        {
        }

        //editor disabled callback
        public virtual void OnDisable()
        {
        }

        //editor removed callback
        public virtual void OnDestroy()
        {
        }


        //after applying the changes
        public virtual void OnApply()
        {
        }


        //sticker message (on toolbar)
        public virtual string GetStickerMessage(ref MessageType type)
        {
            return null;
        }

        public virtual void OnUndoRedo()
        {
        }

        public virtual void OnCurveChanged(BGCurveChangedArgs args)
        {
        }

        public virtual void OnSettingsLoad()
        {
        }
    }
}