// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections.Generic;
namespace Headjack
{
	public class Tools : MonoBehaviour
	{
		/**
        <summary>
        Set texture into a material and crop it to a specific aspect ratio
        </summary>
        <example>
        <code>
            void ApplyTexture(string id, Material material)
            {
				CropTexture(material,id,1920,1080,null);
            }
        </code>
        </example>
        */
		public static void CropTexture(Material material, string id, float targetWidth, float targetHeight, string propertyName = null)
		{
			CropTexture(material, id, targetWidth / targetHeight, propertyName);
		}
		/**
        <summary>
        Set texture into a material and crop it to a specific aspect ratio
        </summary>
        <example>
        <code>
            void ApplyTexture(string id, Material material)
            {
				CropTexture(material,id,16f/9f,null);
            }
        </code>
        </example>
        */
		public static void CropTexture(Material material, string id, float targetAspectRatio, string propertyName = null)
		{
			if (id == null)
			{
				Debug.LogWarning("Crop tool: ID is null");
				return;
			}
			if (App.Data.Project.ContainsKey(id))
			{
				id = App.Data.Project[id].Thumbnail;
			}
			if (id == null)
			{
				Debug.LogWarning("Crop tool: ID is null");
				return;
			}
			if (App.Data.Category.ContainsKey(id))
			{
				id = App.Data.Category[id].Thumbnail;
			}
			if (id == null)
			{
				Debug.LogWarning("Crop tool: ID is null");
				return;
			}
			if (!App.Data.Media.ContainsKey(id))
			{
				Debug.LogWarning("Crop tool: ID not found in media or projects");
				return;
			}
			Vector2 inputSize = new Vector2(App.Data.Media[id].Width * 1f, App.Data.Media[id].Height * 1f);
			float inputAspect = (inputSize.x / inputSize.y);
			Vector2 Scale = new Vector2(1f, 1f);
			Vector2 Off = new Vector2(0f, 0f);
			if (inputAspect < targetAspectRatio)
			{
				Scale.y = (inputSize.x / targetAspectRatio) / (inputSize.x / inputAspect);
				Off.y = 0.5f - (Scale.y * 0.5f);
			}
			else
			{
				Scale.x = (inputSize.y * targetAspectRatio) / (inputSize.y * inputAspect);
				Off.x = 0.5f - (Scale.x * 0.5f);
			}
			if (propertyName == null)
			{
				material.mainTextureScale = Scale;
				material.mainTextureOffset = Off;
			}
			else
			{
				material.SetTextureScale(propertyName, Scale);
				material.SetTextureOffset(propertyName, Off);
			}
		}
		/**
        <summary>
        Generate a UV Sphere
        </summary>
		<param name="Resolution">Grid resolution</param>
        <returns>The generated mesh</returns>
		<remarks>
        &gt; [!NOTE] 
        &gt; UV Spheres have correct uv coordinates for textures 
        </remarks>
        */
		public static Mesh GenerateVideoSphere(int Resolution = 64)
		{
			return Generate.Sphere(new Vector2(1, 1), new Vector2(Resolution, Resolution));
		}
		/**
        <summary>
        Generate a Normalized Cube
        </summary>
		<param name="Resolution">Grid resolution</param>
        <returns>The generated mesh</returns>
		<remarks>
        &gt; [!NOTE] 
        &gt; Normalized cube has more spread out vertex positions
        </remarks>
        */
		public static Mesh GenerateSortedSphere(int Resolution = 16)
		{
			return Generate.SortedSphere(Resolution);
		}
		/**
        <summary>
        Resize a textmesh to fit certain bounds
        </summary>
		<param name="target">The target textmesh</param>
		<param name="newText">The text to apply</param>
		<param name="maxSize">Max physical size in Unity units</param>
		<param name="minMaxFontSize">Minimal and Maximal font size. Too high will cause aliasing, to low will cause blurriness</param>
        <returns>True on succes</returns>
		<remarks>
        &gt; [!NOTE] 
        &gt; To apply the correct font sharpness, there is a tool window under "Headjack/Tools/Font Bitmap Scaler"
        </remarks>
        <example>
        <code>
			Textmesh mesh;
            void FitInSquaredMeter(string text)
            {
                FitTextToBounds(mesh,text,new Vector2(1,1),new Vector2(16,48));
            }
        </code>
        </example>
        */
		public static bool FitTextToBounds(TextMesh target, string newText, Vector2 maxSize, Vector2 minMaxFontSize)
		{
			return FitTextToBounds(target, newText, maxSize, Mathf.RoundToInt(minMaxFontSize.x), Mathf.RoundToInt(minMaxFontSize.y));
		}
		/**
        <summary>
        Resize a textmesh to fit certain bounds
        </summary>
		<param name="target">The target textmesh</param>
		<param name="newText">The text to apply</param>
		<param name="maxSize">Max physical size in Unity units</param>
		<param name="minFontSize">Minimal font size. To low will cause blurriness</param>
		<param name="maxFontSize">Maximal font size. Too high will cause aliasing</param>
        <returns>True on succes</returns>
		<remarks>
        &gt; [!NOTE] 
        &gt; To apply the correct font sharpness, there is a tool window under "Headjack/Tools/Font Bitmap Scaler"
        </remarks>
        <example>
        <code>
			Textmesh mesh;
            void FitInSquaredMeter(string text)
            {
                FitTextToBounds(mesh,text,new Vector2(1,1),16,48);
            }
        </code>
        </example>
        */
		public static bool FitTextToBounds(TextMesh target, string newText, Vector2 maxSize, int minFontSize = 0, int maxFontSize = 0)
		{
			//Debug.Log("resizing");
			if (newText == null)
			{
				newText = "";
			}
			minFontSize = Mathf.Max(minFontSize, 8);
			Quaternion currentRotation = target.transform.rotation;
			target.transform.eulerAngles = Vector3.zero;
			if (maxFontSize != 0)
			{
				target.fontSize = maxFontSize;
			}
			string builder = "";
			target.text = "";
			MeshRenderer descrRender = target.GetComponent<MeshRenderer>();
			Vector3 localBounds = Vector3.zero;
			string[] parts = newText.Split(' ');
			for (int i = 0; i < parts.Length; ++i)
			{

				string Previous = target.text;
				// Debug.Log(Previous);
				target.text += parts[i] + " ";
				if (Mathf.Abs(descrRender.bounds.size.x) > maxSize.x)
				{
					target.text = Previous.Trim() + "\n" + parts[i] + " ";
				}
				if (Mathf.Abs(descrRender.bounds.size.y) > maxSize.y || Mathf.Abs(descrRender.bounds.size.x) > maxSize.x)
				{
					if (target.fontSize == minFontSize)
					{
						target.text = Previous;
						break;
					}
					if (target.fontSize < minFontSize)
					{
						target.fontSize = minFontSize;
					}
					else
					{
						target.fontSize -= 2;
					}
					builder = "";
					target.text = "";
					i = -1;
					continue;
				}

				builder = target.text;
			}
			//Debug.Log(target.fontSize + " fits. Start size: "+ maxFontSize+". Min size: "+minFontSize);
			target.transform.rotation = currentRotation;
			if (builder != target.text)
			{
				target.text = builder;
				return false;
			}
			else
			{
				return true;
			}
		}
		/**
        <summary>
        Blurred image's color quality
        </summary>
		<remarks>
        &gt; [!NOTE] 
        &gt; Default is always ARGB32, only use RGB565 when encountering memory issues
        </remarks>
        */
		public enum BlurQuality
		{
			q16 = RenderTextureFormat.RGB565,
			q32 = RenderTextureFormat.ARGB32
		}
		private static Material BlurMaterial;
		/**
        <summary>
        Apply Gaussian blur to a texture
        </summary>
		<param name="texture">The target texture</param>
		<param name="Mipmaps">Also generate mipmaps</param>
		<param name="Quality">Color Quality</param>
		<param name="newWidth">The output texture's width</param>
		<param name="newHeight">The output texture's height</param>
        <returns>A rendertexture with the image blurred</returns>
		<remarks>
        &gt; [!NOTE] 
        &gt; Apply a few times for a more blurred effect
        </remarks>
        */
		public static Texture BlurImage(Texture texture, bool Mipmaps = true, BlurQuality Quality = BlurQuality.q16, int newWidth = 0, int newHeight = 0)
		{
			if (BlurMaterial == null)
			{
				BlurMaterial = new Material(Resources.Load<Shader>("Shaders/BlurImage"));
			}
			if (texture == null)
			{
				Debug.LogWarning("Blur Tool: Input Texture is null");
				return null;
			}
			if (newWidth < 1 || newHeight < 1)
			{
				newWidth = Mathf.Min(texture.width, 512);
				newHeight = Mathf.Min(texture.height, 512);
			}
			else
			{
				newWidth = Mathf.Min(newWidth, 4096);
				newHeight = Mathf.Min(newHeight, 4096);
			}
			RenderTexture returnTexture = new RenderTexture(newWidth, newHeight, 0, (RenderTextureFormat)Quality);
			RenderTexture temp = RenderTexture.GetTemporary(newWidth, newHeight);

			returnTexture.autoGenerateMips = returnTexture.useMipMap = Mipmaps;
			returnTexture.wrapMode = temp.wrapMode = texture.wrapMode;

			BlurMaterial.SetVector("_Direction", new Vector4(1f / (newWidth * 1f), 0, 0, 0));
			Graphics.Blit(texture, temp, BlurMaterial);
			BlurMaterial.SetVector("_Direction", new Vector4(0, 1f / (newHeight * 1f), 0, 0));
			Graphics.Blit(temp, returnTexture, BlurMaterial);
			RenderTexture.ReleaseTemporary(temp);

			return returnTexture;
		}
		/**
        <summary>
		The active video's streaming texture
        </summary>
        <returns>The active video's streaming texture</returns>
        */
		public static Texture VideoTexture
		{
			get
			{
				if (App.Player == null)
				{
					return null;
				}
				return App.Player.transform.GetChild(0).GetComponent<MeshRenderer>().material.mainTexture;
			}
		}
		/**
        <summary>
		Remove unused Video folders in PersistentDataPath
        </summary>
        */
		public static void CleanUpPersistentDataPath()
		{
			System.Collections.Generic.List<string> videoIds = new System.Collections.Generic.List<string>();
			foreach (string s in App.GetProjects())
			{
				string videoId = App.GetProjectMetadata(s).VideoId;
				if (videoId != null) videoIds.Add(videoId);
			}
			string videoFolder = Application.persistentDataPath + "/Video";
			System.IO.DirectoryInfo videoDirectory = new System.IO.DirectoryInfo(videoFolder);
			foreach (System.IO.DirectoryInfo d in videoDirectory.GetDirectories())
			{
				if (!videoIds.Contains(d.Name)) d.Delete(true);
			}
		}
	}
}