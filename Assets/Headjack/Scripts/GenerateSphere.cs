// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
namespace Headjack
{
	internal class Generate : MonoBehaviour
	{
		internal static int Index(int x, int y, int ResolutionX)
		{
			return x + y * ResolutionX;
		}
		internal static Mesh Sphere(Vector2 size, Vector2 resolution)
		{
			int ResolutionX = Mathf.RoundToInt(resolution.x);
			int ResolutionY = Mathf.RoundToInt(resolution.y);
			Mesh m = new Mesh();
			Vector3[] vertices = new Vector3[ResolutionX * ResolutionY];
			Vector2[] uv = new Vector2[ResolutionX * ResolutionY];
			int[] triangles = new int[(ResolutionX * ResolutionY) * 6];
			int Current = 0;
			for (int h = 0; h < ResolutionY; h++)
			{
				for (int i = 0; i < ResolutionX; i++)
				{
					vertices[Current] = new Vector3(
						-(Mathf.Sin(((Mathf.PI * 2) / (resolution.x - 1)) * i) * Mathf.Sin(((Mathf.PI) / (resolution.y - 1)) * (h * 1f))),
						 Mathf.Cos(((Mathf.PI) / (resolution.y - 1)) * h),
						-(Mathf.Cos(((Mathf.PI * 2) / (resolution.x - 1)) * i) * Mathf.Sin(((Mathf.PI) / (resolution.y - 1)) * (h * 1f)))).normalized;
					uv[Current] = new Vector2((i * 1f) / ((resolution.x - 1) * 1f), 1f - ((h * 1f) / ((resolution.y - 1) * 1f)));
					if (h < ResolutionY - 1 && i < ResolutionX - 1)
					{
						triangles[Current * 6 + 5] = Index(i, h, ResolutionX);
						triangles[Current * 6 + 4] = Index(i, h + 1, ResolutionX);
						triangles[Current * 6 + 3] = Index(i + 1, h + 1, ResolutionX);
						triangles[Current * 6 + 2] = Index(i, h, ResolutionX);
						triangles[Current * 6 + 1] = Index(i + 1, h + 1, ResolutionX);
						triangles[Current * 6 + 0] = Index(i + 1, h, ResolutionX);
					}
					Current += 1;
				}
			}
			m.vertices = vertices;
			m.triangles = triangles;
			m.uv = uv;
			m.name = "Video Sphere";
			m.RecalculateBounds();
			m.bounds = new Bounds(Vector3.zero, new Vector3(90000, 90000, 90000));

			return m;
		}
		internal static Mesh SortedSphere(int Resolution)
		{
			GameObject g = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			Mesh m = g.GetComponent<MeshFilter>().mesh;
			m.name = "VideoSphere";
			int[] t = m.triangles;
			int[] inverted = new int[t.Length];
			for (int i = 0; i < t.Length; ++i)
			{
				inverted[i] = t[t.Length - 1 - i];
			}
			m.triangles = inverted;
			Destroy(g);
			return m;
		}
		internal static Mesh LaserMesh()
		{
			Mesh m = new Mesh();
			m.vertices = new Vector3[] {
			new Vector3(-.5f, -.5f, -.5f),new Vector3(-.5f, .5f, -.5f),new Vector3(.5f, .5f, -.5f),new Vector3(.5f, -.5f, -.5f),
			new Vector3(-.5f, -.5f, .5f),new Vector3(-.5f, .5f, .5f),new Vector3(.5f, .5f, .5f),new Vector3(.5f, -.5f, .5f)};
			Vector2[] uv = new Vector2[8];
			uv[0] = uv[1] = uv[2] = uv[3] = new Vector2(0, 0);
			uv[4] = uv[5] = uv[6] = uv[7] = new Vector2(0, 1);
			m.uv = uv;
			m.triangles = new int[] { 0, 3, 4, 3, 7, 4, 1, 0, 5, 4, 5, 0, 2, 1, 6, 1, 5, 6, 3, 2, 7, 2, 6, 7 };
			return m;
		}
		internal static Mesh FadeMesh()
		{
			Mesh mesh = new Mesh();
			mesh.name = "FadeMesh";
			mesh.vertices = new Vector3[]
			{
				new Vector3(-.5f, .5f, .5f),
				new Vector3(.5f, .5f, .5f),
				new Vector3(.5f, .5f, -.5f),
				new Vector3(-.5f, .5f, -.5f),
				new Vector3(-.5f, -.5f, .5f),
				new Vector3(.5f, -.5f, .5f),
				new Vector3(.5f, -.5f, -.5f),
				new Vector3(-.5f, -.5f, -.5f)
			};
			mesh.triangles = new int[]
			{
				//top
				3,1,0,
				3,2,1,
				//bottom
				6,7,4,
				5,6,4,
				//front
				1,4,0,
				5,4,1,
				//back
				7,6,2,
				3,7,2,
				//left
				4,3,0,
				4,7,3,
				//right
				2,5,1,
				6,5,2
			};
			mesh.RecalculateBounds();
			return mesh;
		}

		internal static Mesh Flat(float aspect, float height, float distance, float heightAdjust)
		{
			Mesh m = new Mesh();
			float h = height/2f;
			float ha = heightAdjust;
			float x = h * aspect;
			float d = distance;
			
			m.vertices = new Vector3[] { new Vector3(-x, h + ha, d), new Vector3(x, h + ha, d), new Vector3(-x, -h + ha, d), new Vector3(x, -h + ha, d) };
			m.uv = new Vector2[] { new Vector2(0, 1), new Vector2(1,1), new Vector2(0, 0), new Vector2(1, 0) };
			m.triangles = new int[] { 0, 1, 2, 2, 1, 3 };
			return m;
		}
	}
}
