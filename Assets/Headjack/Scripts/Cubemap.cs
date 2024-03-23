// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
namespace Headjack
{
internal class Cubemap : MonoBehaviour {

	//private MeshFilter filter;
	private static Mesh m;
	private static List<Vector3> vertices;
	private static List<Vector2> uv;
	private static List<int> triangles;
    internal static float SqueezeFactor = 0.99f;

    internal enum SquareVec
    {
        TopLeft,
        TopRight,
        BottomRight,
        BottomLeft
    };

    // returns corner uv coordinates for a midpoint of a 3x2 cubemap video texture, 
    // with squeeze factor (for pixel overlap)
    internal static Vector2 SqueezedUV(Vector2 mid, SquareVec vec, float squeeze)
    {
        switch (vec)
        {
            case SquareVec.TopLeft:
                return new Vector2(mid.x - 0.166667f * squeeze, mid.y + 0.25f * squeeze);
            case SquareVec.TopRight:
                return new Vector2(mid.x + 0.166667f * squeeze, mid.y + 0.25f * squeeze);
            case SquareVec.BottomRight:
                return new Vector2(mid.x + 0.166667f * squeeze, mid.y - 0.25f * squeeze);
            case SquareVec.BottomLeft:
                return new Vector2(mid.x - 0.166667f * squeeze, mid.y - 0.25f * squeeze);
            default:
                return new Vector2(0, 0);
        }
    }

	internal static Mesh Create()
	{
		m = new Mesh ();
        vertices = new List<Vector3>();
		uv = new List<Vector2>();
        triangles = new List<int>();
        Vector2 mid = new Vector2();

        //Front
        vertices.Add(new Vector3(-1, 1, 1));
        vertices.Add(new Vector3(1, 1, 1));
        vertices.Add(new Vector3(-1, -1, 1));
        vertices.Add(new Vector3(1, -1, 1));
        mid = new Vector2(0.5f, 0.75f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        //Back
        vertices.Add(new Vector3(1, 1, -1));
        vertices.Add(new Vector3(-1, 1, -1));
        vertices.Add(new Vector3(1, -1, -1));
        vertices.Add(new Vector3(-1, -1, -1));
        mid = new Vector2(0.166667f, 0.25f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        //Left
        vertices.Add(new Vector3(-1, 1, -1));
        vertices.Add(new Vector3(-1, 1, 1));
        vertices.Add(new Vector3(-1, -1, -1));
        vertices.Add(new Vector3(-1, -1, 1));
        mid = new Vector2(0.166667f, 0.75f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        //right
        vertices.Add(new Vector3(1, 1, 1));
        vertices.Add(new Vector3(1, 1, -1));
        vertices.Add(new Vector3(1, -1, 1));
        vertices.Add(new Vector3(1, -1, -1));
        mid = new Vector2(0.83333f, 0.75f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        //Top
        vertices.Add(new Vector3(-1, 1, -1));
        vertices.Add(new Vector3(1, 1, -1));
        vertices.Add(new Vector3(-1, 1, 1));
        vertices.Add(new Vector3(1, 1, 1));
        mid = new Vector2(0.83333f, 0.25f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        //Bottom
        vertices.Add(new Vector3(-1, -1, 1));
        vertices.Add(new Vector3(1, -1, 1));
        vertices.Add(new Vector3(-1, -1, -1));
        vertices.Add(new Vector3(1, -1, -1));
        mid = new Vector2(0.5f, 0.25f);
        uv.Add(SqueezedUV(mid, SquareVec.TopLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.TopRight, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomLeft, SqueezeFactor));
        uv.Add(SqueezedUV(mid, SquareVec.BottomRight, SqueezeFactor));
        triangles.Add(vertices.Count - 4);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 2);
        triangles.Add(vertices.Count - 3);
        triangles.Add(vertices.Count - 1);

        Vector3[] V = new Vector3[vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            
            V[i] = vertices[i];
        }
        m.vertices = V;
        Vector2[] U = new Vector2[uv.Count];
        for (int i = 0; i < uv.Count; i++)
        {
            U[i] = uv[i];
        }
        m.uv = U;
        int[] T = new int[triangles.Count];
        for (int i = 0; i < triangles.Count; i++)
        {
            T[i] = triangles[i];
        }
        m.triangles = T;
        m.name = "Mooie Cubemap";
       return m;
	}
}
}