using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Kogane.Internal
{
	/// <summary>
	/// AddressablesAssetPostProcessor.OnPostProcess を上書きするエディタ拡張
	/// アセットが削除された時にグループを更新しないようにします
	/// </summary>
	[InitializeOnLoad]
	internal static class AddressablesAssetPostProcessorOverrider
	{
		static AddressablesAssetPostProcessorOverrider()
		{
			//EditorApplication.update += () => AddressablesAssetPostProcessor.OnPostProcess = OnPostprocessAllAssets;
			EditorApplication.delayCall += () =>
			{
				Debug.Log( "AddressablesAssetPostProcessor.OnPostProcess を上書きしました" );
				AddressablesAssetPostProcessor.OnPostProcess = OnPostprocessAllAssets;
			};
		}

		internal static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths )
		{
	        var aa = AddressableAssetSettingsDefaultObject.Settings;
	        bool modified = false;
	        foreach (string str in importedAssets)
	        {
	            var assetType = AssetDatabase.GetMainAssetTypeAtPath(str);

	            if (typeof(AddressableAssetGroup).IsAssignableFrom(assetType))
	            {
	                AddressableAssetGroup group = aa.FindGroup(Path.GetFileNameWithoutExtension(str));
	                if (group == null)
	                {
	                    var foundGroup = AssetDatabase.LoadAssetAtPath<AddressableAssetGroup>(str);
	                    if (!aa.groups.Contains(foundGroup))
	                    {
	                        aa.groups.Add(foundGroup);
	                        group = aa.FindGroup(Path.GetFileNameWithoutExtension(str));
	                        modified = true;
	                    }
	                }
	                if (group != null)
	                    group.DedupeEnteries();
	            }

	            if (typeof(AddressableAssetEntryCollection).IsAssignableFrom(assetType))
	            {
	                aa.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(str), aa.DefaultGroup);
	                modified = true;
	            }

	            var guid = AssetDatabase.AssetPathToGUID(str);
	            if (aa.FindAssetEntry(guid) != null)
	                modified = true;

	            if (AddressableAssetUtility.IsInResources(str))
	                modified = true;
	        }
	        
	        if (deletedAssets.Length > 0)
	        {
	            // if any directly referenced assets were deleted while Unity was closed, the path isn't useful, so Remove(null) is our only option
	            //  this can lead to orphaned schema files.
	            if (aa.groups.Remove(null) ||
	                aa.DataBuilders.Remove(null) ||
	                aa.GroupTemplateObjects.Remove(null) ||
	                aa.InitializationObjects.Remove(null))
	            {
	                modified = true;
	            }

	        }

	        foreach (string str in deletedAssets)
	        {
	            if (AddressableAssetUtility.IsInResources(str))
	                modified = true;
	            else
	            {
	                if (aa.CheckForGroupDataDeletion(str))
	                {
	                    modified = true;
	                    continue;
	                }

					// アセットが削除された時にグループを更新しない
	                //var guidOfDeletedAsset = AssetDatabase.AssetPathToGUID(str);
	                //if (aa.RemoveAssetEntry(guidOfDeletedAsset))
	                //{
	                //    modified = true;
	                //}
	            }
	        }
	        for (int i = 0; i < movedAssets.Length; i++)
	        {
	            var str = movedAssets[i];
	            var assetType = AssetDatabase.GetMainAssetTypeAtPath(str);
	            if (typeof(AddressableAssetGroup).IsAssignableFrom(assetType))
	            {
	                var oldGroupName = Path.GetFileNameWithoutExtension(movedFromAssetPaths[i]);
	                var group = aa.FindGroup(oldGroupName);
	                if (group != null)
	                {
	                    var newGroupName = Path.GetFileNameWithoutExtension(str);
	                    group.Name = newGroupName;
	                }
	            }
	            else
	            {
	                var guid = AssetDatabase.AssetPathToGUID(str);
	                AddressableAssetEntry entry = aa.FindAssetEntry(guid);

	                bool isAlreadyAddressable =  entry != null;
	                bool startedInResources = AddressableAssetUtility.IsInResources(movedFromAssetPaths[i]);
	                bool endedInResources = AddressableAssetUtility.IsInResources(str);
	                bool inEditorSceneList = BuiltinSceneCache.Contains(new GUID(guid));

	                //update entry cached path
	                entry?.SetCachedPath(str);

	                //move to Resources
	                if (isAlreadyAddressable && endedInResources)
	                {
	                    var fileName = Path.GetFileNameWithoutExtension(str);
	                    Addressables.Log("You have moved addressable asset " + fileName + " into a Resources directory.  It has been unmarked as addressable, but can still be loaded via the Addressables API via its Resources path.");
	                    aa.RemoveAssetEntry(guid, false);
	                }
	                else if(inEditorSceneList)
	                    BuiltinSceneCache.ClearState();

	                //any addressables move or resources move (even resources to within resources) needs to refresh the UI.
	                modified = isAlreadyAddressable || startedInResources || endedInResources || inEditorSceneList;
	            }
	        }

	        if(modified)
	            aa.SetDirty(AddressableAssetSettings.ModificationEvent.BatchModification, null, true, true);
		}
	}
	
	internal static class AddressableAssetEntryInternal
	{
		public static void SetCachedPath( this AddressableAssetEntry entry, string newCachedPath )
		{
			var type       = typeof( AddressableAssetEntry );
			var methodInfo = type.GetMethod( nameof( SetCachedPath ), BindingFlags.Instance | BindingFlags.NonPublic );

			methodInfo.Invoke( entry, new object[] { newCachedPath } );
		}
	}
	
	internal static class AddressableAssetGroupInternal
	{
		public static void DedupeEnteries( this AddressableAssetGroup group )
		{
			var type       = typeof( AddressableAssetGroup );
			var methodInfo = type.GetMethod( nameof( DedupeEnteries ), BindingFlags.Instance | BindingFlags.NonPublic );

			methodInfo.Invoke( group, new object[] { } );
		}
	}

	internal static class AddressablesAssetPostProcessor
	{
		public static Action<string[], string[], string[], string[]> OnPostProcess
		{
			set
			{
				var assembly     = typeof( AddressableAssetSettings ).Assembly;
				var type         = assembly.GetType( "UnityEditor.AddressableAssets.Settings.AddressablesAssetPostProcessor" );
				var propertyInfo = type.GetProperty( nameof( OnPostProcess ), BindingFlags.Static | BindingFlags.Public );

				propertyInfo.SetValue( null, value );
			}
		}
	}

	internal static class AddressableAssetSettingsInternal
	{
		public static bool CheckForGroupDataDeletion( this AddressableAssetSettings settings, string str )
		{
			var type       = typeof( AddressableAssetSettings );
			var methodInfo = type.GetMethod( nameof( CheckForGroupDataDeletion ), BindingFlags.Instance | BindingFlags.NonPublic );

			return ( bool ) methodInfo.Invoke( settings, new object[] { str } );
		}
	}

	internal static class AddressableAssetUtility
	{
		public static bool IsInResources( string path )
		{
			var assembly   = typeof( AddressableAssetSettings ).Assembly;
			var type       = assembly.GetType( "UnityEditor.AddressableAssets.Settings.AddressableAssetUtility" );
			var methodInfo = type.GetMethod( nameof( IsInResources ), BindingFlags.Static | BindingFlags.NonPublic );

			return ( bool ) methodInfo.Invoke( null, new object[] { path } );
		}
	}

	internal static class BuiltinSceneCache
	{
		public static bool Contains( GUID guid )
		{
			var assembly    = typeof( AddressableAssetSettings ).Assembly;
			var type        = assembly.GetType( "UnityEditor.AddressableAssets.Settings.BuiltinSceneCache" );
			var methodInfos = type.GetMethods( BindingFlags.Static | BindingFlags.Public );
			var methodInfo  = methodInfos.First( x => x.Name == nameof( Contains ) && x.GetParameters()[ 0 ].ParameterType == typeof( GUID ) );

			return ( bool ) methodInfo.Invoke( null, new object[] { guid } );
		}

		public static void ClearState( bool clearCallbacks = false )
		{
			var assembly   = typeof( AddressableAssetSettings ).Assembly;
			var type       = assembly.GetType( "UnityEditor.AddressableAssets.Settings.BuiltinSceneCache" );
			var methodInfo = type.GetMethod( nameof( ClearState ), BindingFlags.Static | BindingFlags.Public );

			methodInfo.Invoke( null, new object[] { clearCallbacks } );
		}
	}
}