// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
#if UNITY_IOS
using UnityEngine.iOS;
#endif

namespace Headjack
{
	internal class UHDSupportInfo
	{
		/// <summary>
		/// conservative support returns "not supported" on videos over a modest resolution,
		/// regardless of video source
		/// </summary>
		public static bool useConservativeSupport = false;

		private static Regex rawRegex = new Regex(@"\/Media\/");

#if UNITY_ANDROID && !UNITY_EDITOR
		public static bool Check(AppDataStruct.FormatBlock formatBlock)
		{		
			if (useConservativeSupport && ((formatBlock.Width * formatBlock.Height) > (2048 * 2048))) {
				return false;
			}

			if (formatBlock.Codec == null || formatBlock.Width < 1 || formatBlock.Height < 1)
			{
				return true;
			}

			// always support raw video upload
			if (!useConservativeSupport && rawRegex.IsMatch(formatBlock.DownloadUrl)) {
				return true;
			}

			if ((formatBlock.Width * formatBlock.Height) <= (4096*2160)) {
				return true;
			}
			return false;
		}
#endif
#if UNITY_STANDALONE || UNITY_EDITOR
		public static bool Check(AppDataStruct.FormatBlock formatBlock)
		{
			if (useConservativeSupport && ((formatBlock.Width * formatBlock.Height) > (2048 * 2048))) {
				return false;
			}

			return true;
		}
#endif
#if UNITY_IOS && !UNITY_EDITOR
		private static Dictionary<AppDataStruct.FormatBlock, bool> supported;
		public static bool Check(AppDataStruct.FormatBlock formatBlock)
		{
			if (supported == null) supported = new Dictionary<AppDataStruct.FormatBlock, bool>();

			if (useConservativeSupport && ((formatBlock.Width * formatBlock.Height) > (2048 * 2048))) {
				return false;
			}

			// always support raw video upload
			if (!useConservativeSupport && rawRegex.IsMatch(formatBlock.DownloadUrl)) {
				return true;
			}

			// return cached result
			if (supported.ContainsKey(formatBlock)) return supported[formatBlock];

			// check codec support
			string codec = (formatBlock.Codec != null)? formatBlock.Codec.ToLower() : null;
			if (codec == null)
			{
				Debug.Log("Supported: true, Support all videos with codec null on ios");
				supported.Add(formatBlock, true);
				return true;
			}
			if (codec == "vp9")
			{
				supported.Add(formatBlock, false);
				return false;
			}

			// "small" H264 video is supported on all iOS devices
			if (formatBlock.Width < 2000)
			{
				if (codec == "h264" || codec == "avc" || codec == "x264")
				{
					supported.Add(formatBlock, true);
					return true;
				}
			}

			bool supp = false;
			DeviceGeneration generation = Device.generation;
			switch (generation)
			{
				case DeviceGeneration.iPhone:
				case DeviceGeneration.iPhone3G:
				case DeviceGeneration.iPhone3GS:
				case DeviceGeneration.iPodTouch1Gen:
				case DeviceGeneration.iPodTouch2Gen:
				case DeviceGeneration.iPodTouch3Gen:
				case DeviceGeneration.iPad1Gen:
				case DeviceGeneration.iPhone4:
				case DeviceGeneration.iPodTouch4Gen:
				case DeviceGeneration.iPad2Gen:
				case DeviceGeneration.iPhone4S:
				case DeviceGeneration.iPad3Gen:
				case DeviceGeneration.iPhone5:
				case DeviceGeneration.iPodTouch5Gen:
				case DeviceGeneration.iPadMini1Gen:
				case DeviceGeneration.iPad4Gen:
				case DeviceGeneration.iPhone5C:
				case DeviceGeneration.iPhone5S:
				case DeviceGeneration.iPadAir1:
				case DeviceGeneration.iPadMini2Gen:
				case DeviceGeneration.iPhone6:
				case DeviceGeneration.iPhone6Plus:
				case DeviceGeneration.iPadMini3Gen:
				case DeviceGeneration.iPadAir2:
				case DeviceGeneration.iPadMini4Gen:
				case DeviceGeneration.iPodTouch6Gen:
					supp = false;
					Debug.Log("Not supported on older ios device");
					break;
				default:
					try
					{
						string[] sv = Device.systemVersion.Split("."[0]);
						supp = (int.Parse(sv[0]) > 10);
						Debug.Log("IOS SYSTEM VERSION " + int.Parse(sv[0]));
					}
					catch (System.Exception e)
					{
						Debug.LogError(e.Message + " - " + e.StackTrace);
						supp = true;
					}
					break;
			}
			supported.Add(formatBlock, supp);
			return supp;
		}
#endif
	}
}
