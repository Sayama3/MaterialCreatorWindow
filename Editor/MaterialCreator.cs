using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEditor;
using System.Linq;
using UnityEngine.Rendering;
using System.IO;
using System.Text.RegularExpressions;

namespace Sayama.MaterialCreatorWindow.Editor
{
	public class MaterialCreator : EditorWindow
	{
		private Shader ShaderRef;
		private List<string> PropertiesName;
		private List<string> TexturesPropertiesName;
		private ShaderPropertyType[] PropertiesType;
		private string Suffix = "";

		private List<MaterialParam> MaterialParams = new List<MaterialParam>
		{
			new MaterialParam("_MainTex","albedo","Albedo Map"),
			new MaterialParam("_ParallaxMap","height","Height Map"),
			new MaterialParam("_MetallicGlossMap","metalic","Metalic Map"),
			new MaterialParam("_BumpMap","normal","Normal Map"),
		};

		private bool SetMaterialsInSubFolders = false;
		//Add new Parameters
		private bool addNewParameter = false;
		private MaterialParam paramToCreate; 
		
		//Remove existing parameters
		private bool removeParameter = false;
		
		//the differents foldout
		private bool foldoutShader = false;
		private bool foldoutFiles = false;

		//Current selected folder
		private string SelectedFolder;
		
		// Custom search
		private bool useCustomSearch = false;
		private string regexSearch = "";
		private RegexSearchMode regexSearchMode = RegexSearchMode.Split;
		private RegexOptions regexOptions = RegexOptions.Multiline | RegexOptions.CultureInvariant;
		private string testRegex = "";

		// Add menu named "My Window" to the Window menu
		[MenuItem("Tools/Material Creator")]
		static void Init()
		{
			// Get existing open window or if none, make a new one:
			MaterialCreator window = (MaterialCreator) EditorWindow.GetWindow(typeof(MaterialCreator));
			window.ShaderRef = Shader.Find("Standard");
			window.UpdateShaderProperties();
			window.titleContent.text = "Material Creator";
			window.Show();
		}

		private void OnSelectionChange()
		{
			Repaint();
		}

		void OnGUI()
		{
			UpdateSelectedFolder();
			
			string currentFolderLabel = String.Join(" / ", SelectedFolder.Split('/', '\\'));
			
			EditorGUILayout.BeginVertical(new GUIStyle("HelpBox"));
			
			GUILayout.Label("Selected Folder :",GUIHelper.GetCurrentFolderLabelStyle());
			GUILayout.Label(currentFolderLabel,GUIHelper.GetCurrentFolderStyle());
			
			EditorGUILayout.EndVertical();
			
			//Draw Shader Ref
			GUILayout.Space(8f);
			int shaderId = ShaderRef.GetInstanceID();
			ShaderRef = EditorGUILayout.ObjectField(nameof(ShaderRef).AddSpacesToSentence(), ShaderRef, typeof(Shader), false, null) as Shader;
			if(shaderId != ShaderRef.GetInstanceID()) UpdateShaderProperties();

			useCustomSearch = EditorGUILayout.Toggle("Use a RegeXP for searching", useCustomSearch);
			if (useCustomSearch)
			{
				EditorGUILayout.LabelField("Regex");
				regexSearch = EditorGUILayout.TextArea(regexSearch);
				EditorGUILayout.LabelField("Test");
				testRegex = EditorGUILayout.TextArea(testRegex);
				var matches = Regex.Matches(testRegex, regexSearch);
			}
			else
			{
				Suffix = EditorGUILayout.TextField(nameof(Suffix), Suffix);
			}

			SetMaterialsInSubFolders = EditorGUILayout.Toggle(nameof(SetMaterialsInSubFolders).AddSpacesToSentence(), SetMaterialsInSubFolders);

			GUILayout.Space(8f);

			#region Adding Shader Param

			GUIContent AddNewParameterButton = new GUIContent("Add New Parameter");
			
			addNewParameter = EditorGUILayout.BeginFoldoutHeaderGroup(addNewParameter,AddNewParameterButton,EditorStyles.miniPullDown);
			if (addNewParameter)
			{
				EditorGUILayout.BeginVertical(new GUIStyle("FrameBox"));
				if (paramToCreate == null) paramToCreate = new MaterialParam(String.Empty, String.Empty, String.Empty);
				
				paramToCreate.ParameterName = EditorGUILayout.TextField(nameof(paramToCreate.ParameterName).AddSpacesToSentence(), paramToCreate.ParameterName);
				paramToCreate.MaterialParamName = EditorGUILayout.TextField(nameof(paramToCreate.MaterialParamName).AddSpacesToSentence(), paramToCreate.MaterialParamName);
				if (!ParameterExistInShader(paramToCreate.MaterialParamName))
				{
					HelpMaterialParamNameDontExist(paramToCreate.MaterialParamName);
				}
				paramToCreate.FileRefName = EditorGUILayout.TextField(nameof(paramToCreate.FileRefName).AddSpacesToSentence(), paramToCreate.FileRefName);
				
				if (GUILayout.Button("Add"))
				{
					MaterialParams.Add(new MaterialParam(paramToCreate));
					paramToCreate = null;
					//addNewParameter = false;
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			#endregion

			#region Remove Shader Param

			GUIContent RemoveParameterButton = new GUIContent("Remove Parameter");
			removeParameter = EditorGUILayout.BeginFoldoutHeaderGroup(removeParameter,RemoveParameterButton,EditorStyles.miniPullDown);
			if (removeParameter)
			{
				EditorGUILayout.BeginVertical(new GUIStyle("FrameBox"));
				int indexToRemove = -1;
				for (int i = 0; i < MaterialParams.Count; i++)
				{
					EditorGUILayout.BeginHorizontal(new GUIStyle("FrameBox"));
					EditorGUILayout.LabelField(MaterialParams[i].ParameterName,EditorStyles.boldLabel);
					if (GUILayout.Button("Remove")) indexToRemove = i;
					EditorGUILayout.EndHorizontal();
				}

				if (indexToRemove >= 0 && indexToRemove < MaterialParams.Count)
				{
					MaterialParams.RemoveAt(indexToRemove);
					//removeParameter = false;
				}
				EditorGUILayout.EndVertical();
			}
			EditorGUILayout.EndFoldoutHeaderGroup();

			#endregion

			#region Materials Parameters Name

			GUILayout.Space(8f);
			
			foldoutShader = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutShader, "Materials Parameters Name");
			if (foldoutShader)
			{
				for (int i = 0; i < MaterialParams.Count; i++)
				{
					MaterialParams[i].MaterialParamName = EditorGUILayout.TextField(MaterialParams[i].ParameterName, MaterialParams[i].MaterialParamName);
					
					if (!ParameterExistInShader(MaterialParams[i].MaterialParamName))
					{
						HelpMaterialParamNameDontExist(MaterialParams[i].MaterialParamName);
					}
				}
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

			#endregion

			GUILayout.Space(4f);

			#region File Name Reference

			foldoutFiles = EditorGUILayout.BeginFoldoutHeaderGroup(foldoutFiles, "File Name Reference");
			if (foldoutFiles)
			{
				for (int i = 0; i < MaterialParams.Count; i++)
				{
					
					MaterialParams[i].FileRefName = EditorGUILayout.TextField(MaterialParams[i].ParameterName, MaterialParams[i].FileRefName);
				}
			}

			EditorGUILayout.EndFoldoutHeaderGroup();

			#endregion

			#region Buttons

			GUILayout.Space(8f);
			if (GUILayout.Button("Create SubFolders")) // Create button
			{
				CreateSubFolders();
			}

			if (GUILayout.Button("Create Materials from Subfolders"))
			{
				CreateMaterialsFromSubFolders();
			}

			if (GUILayout.Button("Destroy All Materials"))
			{
				if(EditorUtility.DisplayDialog("Confirmation",$"Do you really want to destroy all the materials at {SelectedFolder} and on his {SelectedFolder?.GetAllSubFolders().Length??0} subfolders ?","Yes, I don't need them anymore","No, they could be useful later"))
					DestroyAllMaterials();
			}

			#endregion
		}

		#region Helpers

		private void HelpMaterialParamNameDontExist(string materialParamName)
		{
			string helpText = $"The parameter {materialParamName} don't exist or isn't a Texture from \"{ShaderRef.name}\" shader.";
			if (materialParamName.Length>0&&TexturesPropertiesName.Any(name => name.Contains(materialParamName)))
			{
				List<string> possiblesNames = TexturesPropertiesName.FindAll(name => name.Contains(materialParamName));
				helpText += "\nyou might mean :";
				for (int i = 0; i < possiblesNames.Count; i++)
				{
					helpText += $"\n    - {possiblesNames[i]}";
				}
			}

			EditorGUILayout.HelpBox(helpText, MessageType.Warning);
		}

		private bool ParameterExistInShader(string materialParamName)
		{
			return TexturesPropertiesName.Exists(propName => propName == materialParamName);
		}

		private void UpdateShaderProperties()
		{
			Debug.Log("Updating Shader Properties");
			int propertyCount = ShaderRef.GetPropertyCount();
			PropertiesName = new List<string>(propertyCount);
			TexturesPropertiesName = new List<string>(propertyCount);
			PropertiesType = new ShaderPropertyType[propertyCount];
			for (int propertyIndex = 0; propertyIndex < propertyCount; propertyIndex++)
			{
				string propName = ShaderRef.GetPropertyName(propertyIndex);
				ShaderPropertyType propType = ShaderRef.GetPropertyType(propertyIndex);
				
				PropertiesName.Add(propName);
				PropertiesType[propertyIndex] = propType;
				if (propType == ShaderPropertyType.Texture)
				{
					TexturesPropertiesName.Add(propName);
				}
			}
		}

		#endregion

		private void CreateSubFolders()
		{
			string subDirectoryName;
			string subDirectory;
			string fileName;
			foreach (var file in SelectedFolder.GetAllFiles())
			{
				fileName = file.GetLastPartPath();
				subDirectoryName = fileName.GetNPart(1, '_');
				subDirectory = $"{SelectedFolder}/{subDirectoryName}";
				if (!Directory.Exists(subDirectory))
				{
					Directory.CreateDirectory(subDirectory);
					AssetDatabase.Refresh();
				}

				AssetDatabase.MoveAsset(file, $"{subDirectory}/{fileName}");
				AssetDatabase.Refresh();
			}

			Debug.Log("Subfolders created");
		}

		private void CreateMaterialsFromSubFolders()
		{
			if (ShaderRef == null) throw new NullReferenceException($"{nameof(ShaderRef)} is null.");

			string folderName;
			string materialpath;
			Material mat;
			foreach (var subFolder in SelectedFolder.GetAllSubFolders())
			{
				// Debug.Log(subFolder);
				folderName = subFolder.GetLastPartPath();
				mat = new Material(ShaderRef);
				materialpath = $"{(SetMaterialsInSubFolders ? subFolder : SelectedFolder)}/{folderName}{Suffix}.mat";
				materialpath = AssetDatabase.GenerateUniqueAssetPath(materialpath);
				AssetDatabase.CreateAsset(mat, materialpath);

				var files = subFolder.GetAllFiles();
				for (int i = 0; i < MaterialParams.Count; i++)
				{
					if (!String.IsNullOrEmpty(MaterialParams[i].MaterialParamName) && !String.IsNullOrEmpty(MaterialParams[i].FileRefName))
					{
						string albedo = files.FirstOrDefault(file => file.GetLastPartPath().ToLower().Contains(MaterialParams[i].FileRefName.ToLower()));
						
						if (ParameterExistInShader(MaterialParams[i].MaterialParamName))
						{
							mat.SetTexture(MaterialParams[i].MaterialParamName, AssetDatabase.LoadAssetAtPath<Texture>(albedo));
						}
						else
							Debug.LogWarning($"The texture name {MaterialParams[i].MaterialParamName} don't exist for the shader {ShaderRef.name}. Therefore, we didn't use it");
					}
				}

				AssetDatabase.Refresh();
			}

			Debug.Log("Materials Created");
		}

		private void DestroyAllMaterials()
		{
			//Debug.Log("Work");
			var folders = SelectedFolder.GetAllSubFolders(true);
			var materialsGuid = AssetDatabase.FindAssets($"t:{nameof(Material)}", folders);
			foreach (var materialGuid in materialsGuid)
			{
				string materialPath = AssetDatabase.GUIDToAssetPath(materialGuid);
				AssetDatabase.DeleteAsset(materialPath);
				AssetDatabase.Refresh();
			}

			Debug.Log("Materials destroyed");
		}

		private string GetCurrentPath()
		{
			var path = "";
			var obj = Selection.activeObject;
			if (obj == null) path = "Assets";
			else path = AssetDatabase.GetAssetPath(obj.GetInstanceID());
			if (path.Length > 0)
			{
				if (Directory.Exists(path))
				{
					return path;
				}
				else
				{
					var pathSplit = path.Split('/', '\\');
					path = String.Join("/", pathSplit, 0, pathSplit.Length - 1);
					return path;
				}
			}
			else
			{
				//Debug.LogWarning("Object selected is not in assets folder");
				return path;
			}
		}

		private void UpdateSelectedFolder()
		{
			SelectedFolder = GetCurrentPath();
		}
	}

	public static class GUIHelper
	{
		public static GUIStyle GetCurrentFolderLabelStyle()
		{
			GUIStyle style = new GUIStyle("BoldLabel");
			style.alignment = TextAnchor.MiddleCenter;
			return style;
		}
		public static GUIStyle GetCurrentFolderStyle()
		{
			GUIStyle style = new GUIStyle("LargeLabel");
			style.normal.textColor = Color.red;
			style.fontSize += 8;
			style.fontStyle = FontStyle.Bold;
			style.alignment = TextAnchor.MiddleCenter;
			return style;
		}
	}

	public class MaterialParam
	{
		public string ParameterName;
		public string MaterialParamName;
		public string FileRefName;

		public MaterialParam(string materialParamName, string fileRefName,string parameterName = "")
		{
			MaterialParamName = materialParamName;
			FileRefName = fileRefName;
			ParameterName = String.IsNullOrEmpty(parameterName) ? fileRefName : parameterName;
		}

		public MaterialParam(MaterialParam materialParam)
		{
			MaterialParamName = materialParam.MaterialParamName;
			FileRefName = materialParam.FileRefName;
			ParameterName = String.IsNullOrEmpty(materialParam.ParameterName) ? materialParam.FileRefName : materialParam.ParameterName;
		}
	}
}
