using System;
using System.Collections.Generic;
using BansheeGz.BGSpline.Curve;
using UnityEngine;

namespace BansheeGz.BGSpline.Components
{
    /// <summary>Triangulation algorythm implementation </summary>
    public class BGTriangulator2D
    {
        //===============================================================================================
        //                                                    Static
        //===============================================================================================
        //min scale for UV
        private const float MinUvScale = 0.000001f;
        //max scale for UV
        private const float MaxUvScale = 1000000;

        //temp List to avoid GC (it's used by traingulation algorithm)
        private static readonly List<int> V = new List<int>();

        //reusable lists
        private static readonly List<Vector3> Vertices = new List<Vector3>();
        private static readonly List<Vector2> Points = new List<Vector2>();
        private static readonly List<Vector2> Uvs = new List<Vector2>();
        private static readonly List<int> Triangles = new List<int>();

        //===============================================================================================
        //                                                    public methods
        //===============================================================================================

        /// <summary>Fill in mesh with required data </summary>
        public void Bind(Mesh mesh, List<Vector3> positions, Config config)
        {
            var positionsCount = positions.Count;
            //I dont know why, but triangulator works wrong in case first pos= last pos
            if (config.Closed) positionsCount--;

            Clear();

            if (positionsCount > 2)
            {
                //we need to triangulate
                var minMax = new Vector4(Single.MaxValue, Single.MaxValue, Single.MinValue, Single.MinValue);
                for (var i = 0; i < positionsCount; i++)
                {
                    var pos = positions[i];

                    Vector3 vert;
                    Vector2 point;
                    switch (config.Mode2D)
                    {
                        case BGCurve.Mode2DEnum.XY:
                            vert = new Vector3(pos.x, pos.y);
                            point = new Vector2(pos.x, pos.y);
                            break;
                        case BGCurve.Mode2DEnum.XZ:
                            vert = new Vector3(pos.x, 0, pos.z);
                            point = new Vector2(pos.x, pos.z);
                            break;
                        default:
                            // case BGCurve.Mode2DEnum.YZ:
                            vert = new Vector3(0, pos.y, pos.z);
                            point = new Vector2(pos.y, pos.z);
                            break;
                    }
                    Vertices.Add(vert);
                    Points.Add(point);

                    //min
                    if (point.x < minMax.x) minMax.x = point.x;
                    if (point.y < minMax.y) minMax.y = point.y;
                    //max
                    if (point.x > minMax.z) minMax.z = point.x;
                    if (point.y > minMax.w) minMax.w = point.y;
                }

                //tris
                Triangulate(Points, Triangles);
                if (config.AutoFlip)
                {
                    Vector3 BA = Points[Triangles[1]] - Points[Triangles[0]];
                    Vector3 CA = Points[Triangles[2]] - Points[Triangles[0]];
                    var crossProduct = Vector3.Cross(BA, CA);
                    //todo add requirement to have mainCamera
                    var dotProduct = Vector3.Dot(crossProduct, Camera.main.transform.forward);
                    if (dotProduct > 0) Triangles.Reverse();
                }
                else if (!config.Flip) Triangles.Reverse();


                //uvs
                if (config.UvMode == Config.UvModeEnum.Scale) Bind(minMax, config.ScaleUV, config.OffsetUV);
                else Bind(minMax, config.PixelsPerUnit, config.TextureSize);


                // doubleSided
                if (config.DoubleSided)
                {
                    //add points, we can not reuse the same points, cause we need unique UVs
                    var verticesCount = Vertices.Count;
                    for (var i = 0; i < verticesCount; i++) Vertices.Add(Vertices[i]);

                    //add triangles
                    var trianglesCount = Triangles.Count;
                    for (var i = trianglesCount - 1; i >= 0; i--) Triangles.Add(Triangles[i] + verticesCount);

                    //uvs
                    if (config.UvMode == Config.UvModeEnum.Scale) Bind(minMax, config.ScaleBackUV, config.OffsetBackUV);
                    else Bind(minMax, config.PixelsPerUnitBack, config.TextureSize);
                }
            }

            mesh.Clear();
            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetUVs(0, Uvs);
            mesh.RecalculateNormals();

            Clear();
        }

        //===============================================================================================
        //                                                    private methods
        //===============================================================================================
        private void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
            Uvs.Clear();
            Points.Clear();
        }

        //Based on scale value
        private static void Bind(Vector4 minMax, Vector2 scale, Vector2 offset)
        {
            var width = minMax.z - minMax.x;
            var height = minMax.w - minMax.y;
            var pointsCount = Points.Count;
            var scaleX = Mathf.Clamp(scale.x, MinUvScale, MaxUvScale);
            var scaleY = Mathf.Clamp(scale.y, MinUvScale, MaxUvScale);
            for (var i = 0; i < pointsCount; i++)
            {
                var point = Points[i];
                Uvs.Add(new Vector2(offset.x + ((point.x - minMax.x)/width)*scaleX, offset.y + ((point.y - minMax.y)/height)*scaleY));
            }
        }

        //Based on PPU
        private void Bind(Vector4 minMax, BGPpu pixelsPerUnit, BGPpu textureSize)
        {
            var scaleX = pixelsPerUnit.X/(float) textureSize.X;
            var scaleY = pixelsPerUnit.Y/(float) textureSize.Y;

            var pointsCount = Points.Count;
            for (var i = 0; i < pointsCount; i++)
            {
                var point = Points[i];
                var x = (point.x - minMax.x)*scaleX;
                var y = (point.y - minMax.y)*scaleY;
                Uvs.Add(new Vector2(x, y));
            }
        }

        //==================================== This code from http://wiki.unity3d.com/index.php?title=Triangulator
        // Triangulate simple polygon. I have no idea how good this algorithm is and what it's actually doing
        private static void Triangulate(List<Vector2> points, List<int> tris)
        {
            tris.Clear();

            int n = points.Count;
            if (n < 3) return;

            V.Clear();

            if (Area(points) > 0) for (int v = 0; v < n; v++) V.Add(v);
            else for (int v = 0; v < n; v++) V.Add((n - 1) - v);

            int nv = n;
            int count = 2*nv;
            for (int m = 0, v = nv - 1; nv > 2;)
            {
                if ((count--) <= 0) return;

                int u = v;
                if (nv <= u) u = 0;

                v = u + 1;

                if (nv <= v) v = 0;
                int w = v + 1;
                if (nv <= w) w = 0;

                if (Snip(points, u, v, w, nv, V))
                {
                    int a, b, c, s, t;
                    a = V[u];
                    b = V[v];
                    c = V[w];
                    tris.Add(a);
                    tris.Add(b);
                    tris.Add(c);
                    m++;
                    for (s = v, t = v + 1; t < nv; s++, t++) V[s] = V[t];
                    nv--;
                    count = 2*nv;
                }
            }
        }

        private static float Area(List<Vector2> points)
        {
            int n = points.Count;
            float A = 0.0f;
            for (int p = n - 1, q = 0; q < n; p = q++)
            {
                Vector2 pval = points[p];
                Vector2 qval = points[q];
                A += pval.x*qval.y - qval.x*pval.y;
            }
            return (A*0.5f);
        }

        private static bool Snip(List<Vector2> points, int u, int v, int w, int n, List<int> V)
        {
            int p;
            Vector2 A = points[V[u]];
            Vector2 B = points[V[v]];
            Vector2 C = points[V[w]];
            if (Mathf.Epsilon > (((B.x - A.x)*(C.y - A.y)) - ((B.y - A.y)*(C.x - A.x)))) return false;
            for (p = 0; p < n; p++)
            {
                if ((p == u) || (p == v) || (p == w)) continue;
                Vector2 P = points[V[p]];
                if (InsideTriangle(A, B, C, P)) return false;
            }
            return true;
        }

        private static bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
        {
            float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
            float cCROSSap, bCROSScp, aCROSSbp;

            ax = C.x - B.x;
            ay = C.y - B.y;
            bx = A.x - C.x;
            @by = A.y - C.y;
            cx = B.x - A.x;
            cy = B.y - A.y;
            apx = P.x - A.x;
            apy = P.y - A.y;
            bpx = P.x - B.x;
            bpy = P.y - B.y;
            cpx = P.x - C.x;
            cpy = P.y - C.y;

            aCROSSbp = ax*bpy - ay*bpx;
            cCROSSap = cx*apy - cy*apx;
            bCROSScp = bx*cpy - @by*cpx;

            return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
        }


        /// <summary>all possible triangulation parameters </summary>
        public class Config
        {
            public enum UvModeEnum
            {
                Scale,
                PPU
            }

            public UvModeEnum UvMode = UvModeEnum.Scale;

            public bool Closed;
            public bool Flip;
            public bool AutoFlip;
            public BGCurve.Mode2DEnum Mode2D;
            public bool DoubleSided;


            //Scale
            public Vector2 ScaleUV = Vector2.one;
            public Vector2 OffsetUV = Vector2.zero;
            public Vector2 ScaleBackUV = Vector2.one;
            public Vector2 OffsetBackUV = Vector2.zero;

            //PPU
            public BGPpu TextureSize;
            public BGPpu PixelsPerUnit;
            public BGPpu PixelsPerUnitBack;
        }
    }
}