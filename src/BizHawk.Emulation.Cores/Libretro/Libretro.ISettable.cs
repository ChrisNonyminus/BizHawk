using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator : ISettable<LibretroEmulator.Settings, LibretroEmulator.SyncSettings>
	{
		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			_settings = o;
			return PutSettingsDirtyBits.RebootCore;
		}

		private Settings _settings;

		public class Settings
		{
			[DisplayName("Core Options")]
			public Dictionary<string, Dictionary<string, string>> CoreOptions { get; set; } = new Dictionary<string, Dictionary<string, string>>();

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			bool ret = SyncSettings.NeedsReboot(o, _syncSettings);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private SyncSettings _syncSettings;

		public class SyncSettings
		{
			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}
		}
	}
}
