﻿using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IStatable
	{
		public void LoadStateBinary(BinaryReader reader)
		{
			_elf.LoadStateBinary(reader);
			// other variables
			Frame = reader.ReadInt32();
			LagCount = reader.ReadInt32();
			IsLagFrame = reader.ReadBoolean();
			_discIndex = reader.ReadInt32();
			_prevDiskPressed = reader.ReadBoolean();
			_nextDiskPressed = reader.ReadBoolean();
			// any managed pointers that we sent to the core need to be resent now!
			Core.gpgx_set_input_callback(_inputCallback);
			RefreshMemCallbacks();
			Core.gpgx_set_cdd_callback(cd_callback_handle);
			Core.gpgx_invalidate_pattern_cache();
			Core.gpgx_set_draw_mask(_settings.GetDrawMask());
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_elf.SaveStateBinary(writer);
			// other variables
			writer.Write(Frame);
			writer.Write(LagCount);
			writer.Write(IsLagFrame);
			writer.Write(_discIndex);
			writer.Write(_prevDiskPressed);
			writer.Write(_nextDiskPressed);
		}
	}
}
