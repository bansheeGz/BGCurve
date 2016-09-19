BG Curve v.1.1 by BancheeGz (09/2016)

support email: banshee.gzzz@gmail.com

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
8) Use point's and selectoin menus in Scene View (hold Ctrl and hover over a point or selection's center).
9) View demo video.

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

