using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Libretro
{
	public partial class LibretroEmulator
	{
		private readonly List<MemoryDomain> _memoryDomains = new();
		private IMemoryDomains MemoryDomains { get; set; }

		private void InitMemoryDomains()
		{
			UpdateCallbackHandler();

			foreach (LibretroApi.RETRO_MEMORY m in Enum.GetValues(typeof(LibretroApi.RETRO_MEMORY)))
			{
				var mem = api.retro_get_memory_data(m);
				var sz = api.retro_get_memory_size(m);
				if (mem != IntPtr.Zero && sz > 0)
				{
					var d = new MemoryDomainIntPtr(Enum.GetName(m.GetType(), m), MemoryDomain.Endian.Little, mem, sz, true, 1);
					_memoryDomains.Add(d);
					if (m is LibretroApi.RETRO_MEMORY.SAVE_RAM or LibretroApi.RETRO_MEMORY.RTC)
					{
						_saveramAreas.Add(d);
						_saveramSize += d.Size;
					}
				}
			}

			for (uint i = 0; i < bridge.LibretroBridge_GetNumMemDescs(); i++)
			{
				var mem = bridge.LibretroBridge_GetMemDescPtr(i);
				var sz = (long)bridge.LibretroBridge_GetMemDescSize(i);
				var name = bridge.LibretroBridge_GetMemDescName(i);
				if (mem != IntPtr.Zero && sz > 0)
				{
					var d = new MemoryDomainIntPtr(name != IntPtr.Zero ? Marshal.PtrToStringAuto(name) : $"Domain{i}", bridge.LibretroBridge_IsMdescBigEndian(i) ? MemoryDomain.Endian.Big : MemoryDomain.Endian.Little, mem, sz, true, 1);
					_memoryDomains.Add(d);
				}
			}

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			_serviceProvider.Register(MemoryDomains);
		}
	}
}
