using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CurvedUI : MonoBehaviour
{
	private List<UIElement> uiElements;
	private Dictionary<Graphic, Mesh> uiElementsDictionary;
	private Shader tmproShader, tmproShaderNoFade;
	public int facetsPerMeter=100;
	public MeshCollider meshCollider;
	[System.Serializable]
	public class UIElement
	{
		public UIElement(Graphic _graphic, Mesh _mesh)
		{
			graphic = _graphic;
			mesh = _mesh;
		}
		public Graphic graphic;
		public Mesh mesh;
	}

	public void Initialize()
	{
		Canvas.ForceUpdateCanvases();
		uiElementsDictionary = new Dictionary<Graphic, Mesh>();
		uiElements = new List<UIElement>();
		//curvedMaterial = new Material(Resources.Load<Shader>("CurvedUI"));
		tmproShader = Resources.Load<Shader>("TMPCurved");
		tmproShaderNoFade = Resources.Load<Shader>("TMPCurvedNoFade");
		ScanForGraphics(transform);
		meshCollider = gameObject.AddComponent<MeshCollider>();
		meshCollider.sharedMesh = GenerateCurved(GetComponent<Canvas>(), 16, 1);

		Debug.Log("Setting shader curve keyword");
		Shader.EnableKeyword("CURVED_ON");
		Shader.DisableKeyword("CURVED_OFF");
	}

	private void LateUpdate()
	{
		if (uiElements == null) return;
		for (int i = 0; i < uiElements.Count; ++i)
		{
			CanvasRenderer canvasRenderer = uiElements[i].graphic.canvasRenderer;
			canvasRenderer.SetMesh(uiElements[i].mesh);
			//uiElements[i].graphic.material = curvedMaterial;
		}
	}

	public void ScanForGraphics(Transform parent)
	{
		foreach (Transform child in parent) ScanForGraphics(child);
		Graphic[] componentGraphics = parent.GetComponents<Graphic>();
		//Debug.Log(parent.name + " has " + componentGraphics.Length + " graphics");
		if (componentGraphics != null && componentGraphics.Length != 0)
		{
			for (int i = 0; i < componentGraphics.Length; ++i)
			{
				if (componentGraphics[i].GetType() == typeof(TMPro.TextMeshProUGUI))
				{
					TMPro.TextMeshProUGUI tmpro = (TMPro.TextMeshProUGUI)componentGraphics[i];
					tmpro.fontMaterial.shader = componentGraphics[i].gameObject.name == "TextNoFade" ? tmproShaderNoFade : tmproShader;
					tmpro.UpdateMeshPadding();
					tmpro.UpdateFontAsset();
					continue;
				}

				if (componentGraphics[i].GetType() == typeof(TMPro.TMP_SubMeshUI))
				{
					TMPro.TMP_SubMeshUI tmProSub = (TMPro.TMP_SubMeshUI)componentGraphics[i];
					tmProSub.UpdateMeshPadding(false, false);
					continue;
				}
				
				//componentGraphics[i].material = curvedMaterial;
				RectTransform componentTransform = (RectTransform)componentGraphics[i].transform;
				Vector2 size = new Vector2(componentTransform.rect.width, componentTransform.rect.height);
				Vector2 sizeDelta = ((RectTransform)componentGraphics[i].transform).sizeDelta;
				int xSub = (int)(size.x / (1f/ facetsPerMeter) * componentTransform.lossyScale.x);
				int ySub = 1;
				Mesh m = GeneratePlane(xSub, ySub, size,componentTransform.pivot, componentGraphics[i].color);
				UIElement uiElement = new UIElement(componentGraphics[i], m);
				uiElements.Add(uiElement);
				uiElementsDictionary.Add(uiElement.graphic, uiElement.mesh);
			}
		}
	}

	public void SetMeshColor(Graphic graphic, Color color)
	{
		Mesh m = uiElementsDictionary[graphic];
		Color[] c = new Color[m.vertexCount];
		for (int i = 0; i < 4; ++i) c[i] = color;
		m.colors = c;
	}

	public Mesh GetMesh(Graphic graphic)
	{
		return uiElementsDictionary[graphic];
	}

	public Mesh GeneratePlane(int xRes, int yRes, Vector2 scale, Vector2 pivot, Color color)
	{
		Mesh m = new Mesh();
		//Debug.Log("Generating mesh with resolution " + xRes + " x " + yRes);
		Vector3[] vertices = new Vector3[(xRes + 1) * (yRes + 1)];
		Vector2[] uvs = new Vector2[vertices.Length];
		Color[] c = new Color[vertices.Length];
		Vector3[] normals = new Vector3[vertices.Length];
		int[] triangles = new int[xRes * yRes * 6];
		int index = 0;
		pivot -= new Vector2(0.5f, 0.5f);
		for (float y = 0.0f; y < (yRes + 1); y++)
		{
			for (float x = 0.0f; x < (xRes + 1); x++)
			{
				vertices[index] = new Vector3(x * (scale.x / xRes) - scale.x / 2f, y * (scale.y / yRes) - scale.y / 2f, 0);
				vertices[index] += new Vector3(-pivot.x* scale.x, -pivot.y* scale.y, 0);
				normals[index] = -Vector3.forward;
				c[index] = color;
				uvs[index++] = new Vector2(x * (1.0f / xRes), y * (1.0f / yRes));
				
			}
		}
		index = 0;
		for (int y = 0; y < yRes; y++)
		{
			for (int x = 0; x < xRes; x++)
			{
				triangles[index] = (y * (xRes + 1)) + x;
				triangles[index + 1] = ((y + 1) * (xRes + 1)) + x;
				triangles[index + 2] = (y * (xRes + 1)) + x + 1;
				triangles[index + 3] = ((y + 1) * (xRes + 1)) + x;
				triangles[index + 4] = ((y + 1) * (xRes + 1)) + x + 1;
				triangles[index + 5] = (y * (xRes + 1)) + x + 1;
				index += 6;
			}
		}
		m.vertices = vertices;
		m.uv = uvs;
		m.triangles = triangles;
		m.normals = normals;
		m.colors = c;
		m.bounds = new Bounds(Vector3.zero, new Vector3(100, 100, 100));
		return m;
	}

	public Mesh GenerateCurved(Canvas canvas, int subdivisions, float depth)
	{
		Mesh m = new Mesh();
		Vector3[] v = new Vector3[subdivisions * 2 + 2];
		Vector3[] n = new Vector3[v.Length];
		Vector2[] uv = new Vector2[v.Length];
		int[] t = new int[subdivisions * 2 * 3];
		RectTransform canvasTransform = (RectTransform)canvas.transform;
		Vector2 size = new Vector2(canvasTransform.rect.width, canvasTransform.rect.height) ;
		depth = canvas.transform.position.z;
		float curve = size.x * 180f / Mathf.PI / depth;
		for (int i = 0; i < subdivisions + 1; ++i)
		{
			float angle = (-curve / 2f) + (curve / subdivisions * i);
			v[i] = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * depth, size.y / 2f, Mathf.Cos(angle * Mathf.Deg2Rad) * depth - depth);
			n[i] = Vector3.Normalize(-v[i]);
			uv[i] = new Vector2(1f / (subdivisions) * i, 1);
			v[i + subdivisions + 1] = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * depth, -size.y / 2f, Mathf.Cos(angle * Mathf.Deg2Rad) * depth - depth);
			n[i + subdivisions + 1] = Vector3.Normalize(-v[i + subdivisions + 1]);
			uv[i + subdivisions + 1] = new Vector2(1f / (subdivisions) * i, 0);
		}
		for (int i = 0; i < subdivisions; ++i)
		{
			t[i * 6] = i;
			t[i * 6 + 1] = i + 1;
			t[i * 6 + 2] = i + subdivisions + 1;
			t[i * 6 + 3] = i + 1;
			t[i * 6 + 4] = i + subdivisions + 2;
			t[i * 6 + 5] = i + subdivisions + 1;
		}
		m.vertices = v;
		m.triangles = t;
		m.normals = n;
		m.uv = uv;
		return m;
	}
	private void OnDisable()
	{
		Shader.EnableKeyword("CURVED_OFF");
		Shader.DisableKeyword("CURVED_ON");
	}
}
