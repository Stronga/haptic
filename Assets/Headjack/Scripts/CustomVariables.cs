// Copyright (c) Headjack (part of Purple Pill VR B.V.), All rights reserved.

using System.Collections.Generic;
using UnityEngine;
// using SimpleJSON;
using System.Text.RegularExpressions;
using System.Linq;

namespace Headjack
{
	public class CustomVariables
	{
		/**
		<summary>
		Get an array of all available variables in CustomVariables
		</summary>
		<returns>string[] containing all available variables in Custom Variables</returns>
		<example> 
		<code>
		void LogAllCustomVariables()
		{
			string[] allVariables = CustomVariables.availableVariables;
			if (allVariables == null)
			{
				Debug.Log("No Custom Variables Found!");
			}
			else
			{
				foreach(string s in allVariables)
				{
					Debug.Log("Custom Variable Found: " + s);
				}
			}
		}
		</code>
		</example>
		*/
		public static string[] availableVariables
		{
			get
			{
				if (App.Data == null) return null;
				if (App.Data.customVariables == null) return null;
				return App.Data.customVariables.Keys.ToArray();
			}
		}
		/**
		<summary>
		The return type of a global variable
		</summary>
		<param name="variable">The variable to check</param>
		<returns>System.Type of the variable</returns>
		<example>
		<code>
		void LogFirstVariableType()
		{
			string[] allVariables = CustomVariables.availableVariables;
			if (allVariables == null)
			{
				Debug.Log("No variables found");
			}
			else
			{
				System.Type variableType = CustomVariables.GetReturnType(allVariables[0]);
				Debug.Log("First variable's type: " + variableType);
			}
		}
		</code>
		</example>
		*/
		public static System.Type GetReturnType(string variable)
		{
			if (App.Data == null) return null;
			if (App.Data.customVariables == null) return null;
			if (!App.Data.customVariables.ContainsKey(variable)) return null;
			switch (App.Data.customVariables[variable].type)
			{
				case AppDataStruct.VariableType.Type_Color:
					return typeof(Color);
				case AppDataStruct.VariableType.Type_DateTime:
					return typeof(System.DateTime);
				case AppDataStruct.VariableType.Type_MultiSelect:
					return typeof(string[]);
				case AppDataStruct.VariableType.Type_String:
					return typeof(string);
			}
			return null;
		}
		/**
		<summary>
		Try getting a variable's value
		</summary>
		<returns>True if the variable was found and casted to the requisted type</returns>
		<param name="variable">The variable's name</param>
		<param name="result">The result will be set to this out parameter</param>
		<param name="projectId">(Optional) When a "per project" variable, use this parameter</param>
		<remarks>
        &gt; [!NOTE] 
        &gt; result will be set to the requisted type's default value if failed
        </remarks>
		<example>
		<code>
		Color c;
		if (CustomVariables.TryGetVariable&lt;Color&gt;("PrimaryColor", out c))
		{
			Debug.Log("Succes!");
		}
		string s;
		if (CustomVariables.TryGetVariable&lt;string&gt;("App Title", out s))
		{
			Debug.Log("Succes!");
		}
		</code>
		</example>
		*/
		public static bool TryGetVariable<T>(string variable, out T result, string projectId = null)
		{
			try
			{
				object match=Get(variable, projectId);
				if (GetReturnType(variable) != typeof(T) || match == null) throw new System.Exception();
				result = (T)match;
				return true;
			}
			catch (System.Exception)
			{
				result = default(T);
				return false;
			}
		}
		/**
		<summary>
		Get a variable's value
		</summary>
		<returns>The value if it was found and casted to the requisted type, the type's default value on fail</returns>
		<param name="variable">The variable to check</param>
		<param name="projectId">(Optional) When a "per project" variable, use this parameter</param>
		<remarks>
        &gt; [!WARNING] 
        &gt; Will throw an error on fail, but will return the default value so your code can continue
        </remarks>
		<example>
		<code>
		System.DateTime timeToStart = GetVariable&lt;System.DateTime&gt;("TimeToStart");
		string LanguageOfFirstProject = GetVariable&lt;string&gt;("Language", App.GetProjects()[0]);
		</code>
		</example>
		*/
		public static T GetVariable<T>(string variable, string projectId = null)
		{
			try
			{
				return (T)Get(variable, projectId);
				
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message + "\n" + e.StackTrace);
				return default(T);
			}
		}
		private static object Get(string variable, string id = null)
		{
			if (App.Data == null ||
				App.Data.customVariables == null)
			{
				throw new System.Exception("Server response does not contain custom variables");
			}
			if (variable == null || !App.Data.customVariables.ContainsKey(variable))
			{
				throw new System.Exception("Unrecognized variable: " + variable);
			}

			object target = null;
			if (!App.Data.customVariables[variable].projectSpecific)
			{
				target = App.Data.customVariables[variable].value;
			}
			else
			{
				if (id != null)
				{
					Dictionary<string, object> dic = (Dictionary<string, object>)App.Data.customVariables[variable].value;
					if (dic.ContainsKey(id)) target = dic[id];
				}
			}
			if (target == null)
			{
				throw new System.Exception("Could not parse value: " + variable);
			}
			return target;
		}

		private static Regex colorRegex = new Regex(@"rgba?\(([0-9]+),\s?([0-9]+),\s?([0-9]+)(?>\)|,\s?([0-9.]+)\))");
		private static Color textToColor(string text)
		{
			try
			{
				GroupCollection g = colorRegex.Match(text).Groups;
				return new Color(
					float.Parse(g[1].Value) / 255f,
					float.Parse(g[2].Value) / 255f,
					float.Parse(g[3].Value) / 255f,
					(g[4].Value == "") ? 1f : float.Parse(g[4].Value)
				);
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message);
				return Color.black;
			}
		}
    } 
}