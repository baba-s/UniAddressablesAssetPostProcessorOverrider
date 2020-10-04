using UnityEditor;
using UnityEngine;

namespace Kogane.Internal
{
	/// <summary>
	/// Preferences における設定を管理する ScriptableObject
	/// </summary>
	internal sealed class AddressablesAssetPostProcessorOverriderSettings : ScriptableObjectForPreferences<AddressablesAssetPostProcessorOverriderSettings>
	{
		//================================================================================
		// 定数
		//================================================================================
		private static readonly string PACKAGE_NAME        = "UniAddressablesAssetPostProcessorOverrider";
		private static readonly bool   DEFAULT_ENABLED_LOG = false;
		private static readonly string DEFAULT_LOG_MESSAGE = $"[{PACKAGE_NAME}] AddressablesAssetPostProcessor.OnPostProcess を上書きしました";

		//================================================================================
		// 変数(static)
		//================================================================================
		[SerializeField] private bool   m_enabledLog = DEFAULT_ENABLED_LOG;
		[SerializeField] private string m_logMessage = DEFAULT_LOG_MESSAGE;

		//================================================================================
		// プロパティ
		//================================================================================
		public bool   EnabledLog => m_enabledLog;
		public string LogMessage => m_logMessage;

		//================================================================================
		// 関数(static)
		//================================================================================
		[SettingsProvider]
		private static SettingsProvider SettingsProvider()
		{
			return CreateSettingsProvider
			(
				settingsProviderPath: $"Kogane/{PACKAGE_NAME}",
				onGUIExtra: so =>
				{
					if ( !GUILayout.Button( "Reset to Default" ) ) return;

					so.FindProperty( nameof( m_enabledLog ) ).boolValue   = DEFAULT_ENABLED_LOG;
					so.FindProperty( nameof( m_logMessage ) ).stringValue = DEFAULT_LOG_MESSAGE;
				}
			);
		}
	}
}