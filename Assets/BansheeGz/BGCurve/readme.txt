BG Curve v.1.2.7 by BancheeGz (05/2019)
Bezier spline editor for Unity
License: MIT License

www.bansheegz.com/BGCurve
Support: 
1) Github: https://github.com/bansheeGz/BGCurve/issues
2) Unity forum thread: https://forum.unity3d.com/threads/bgcurve-free-bezier-spline-editor-support-thread-official.456983

=====================================================

-----------------------------------------------------
Package structure:
-----------------------------------------------------
Curve - basic curve's classes + math (distance, positions, tangents, position by closest point).
Components - components are meant to add some functions to curve without any scripting.
Examples - examples and tests for different use-cases. 

-----------------------------------------------------
Quick tips:
-----------------------------------------------------
1) To create a curve chose GameObjecty->Create Other->BG Curve or AddComponent and search "BGCurve".
2) You can learn a lot by looking at fields tooltips and contextual tips in the editor. Example scene could also be helpfull.
3) You can save & load curve settings on/from disk.
4) You can select several points and perform group operations. Hold shift to use rectangular selection.
5) Use components- they are meant to work without any additional scripting.
5) BGCurve class does not contain any Math operations- to use Math- add BGCcMath component
6) Use 2D mode if needed, it helps with 2d curves.
7) Use Lock view to disable selecting of any object except curve's points.
8) Use point's and selection menus in Scene View (hold Ctrl and hover over a point or selection's center).
9) Use standard Unity's AnimationView to animate splines
10) View demo video.



=====================================================
Versions History:
-----------------------------------------------------
Version 1.3 changes:
-----------------------------------------------------
Unity 5.x support discontinued
Editor code refactoring

-----------------------------------------------------
Version 1.2 changes:
-----------------------------------------------------
Animation support
Custom fields for points
Snapping (to terrain etc.)
New components (Scale and Triangulate)
New approximation type (Adaptive)
Base math switched to fastest algorithm available for approximation (25% faster)
Components (ChangeCursor and Rotate) are upgraded to support custom points fields
Points store options (inlined, component, gameobjects)

-----------------------------------------------------
Version 1.1 changes:
-----------------------------------------------------
Components are added (for using without scripting)
Rectangular selection for points.
Context menu for points and selections in SceneView.
BGCurveBaseMath optimization.
New points now has preview in Scene View before adding.
Point creation now respect tangents, controls are scaled properly, in 2D mode Ctrl+Click now respects curve's 2D plane, rather than some mesh.
Show tangents option in SceneView
Examples are reworked.
Minor bugs fixing.
code cleanup and refactoring.
Unity 5.4 compatibility

