﻿using System;
using System.Runtime.InteropServices;
using System.Text;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public abstract class LibMAME
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		// enums
		public enum OutputChannel : int
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		}

		// constants
		public const int ROMENTRYTYPE_SYSTEM_BIOS = 9;
		public const int ROMENTRYTYPE_DEFAULT_BIOS = 10;
		public const int ROMENTRY_TYPEMASK = 15;
		public const int BIOS_INDEX = 24;
		public const int BIOS_FIRST = 1;
		public const string BIOS_LUA_CODE = "bios";

		// main launcher
		[BizImport(cc, Compatibility = true)]
		public abstract uint mame_launch(int argc, string[] argv);

		[BizImport(cc)]
		public abstract bool mame_coswitch();

		[BizImport(cc)]
		public abstract byte mame_read_byte(uint address);

		[BizImport(cc)]
		public abstract IntPtr mame_input_get_field_ptr(string tag, string field);

		[BizImport(cc)]
		public abstract void mame_input_set_fields(IntPtr[] fields, int[] inputs, int length);

		[BizImport(cc)]
		public abstract int mame_sound_get_samples(short[] buffer);

		[BizImport(cc)]
		public abstract int mame_video_get_dimensions(out int width, out int height);

		[BizImport(cc)]
		public abstract int mame_video_get_pixels(int[] buffer);

		[UnmanagedFunctionPointer(cc)]
		public delegate void FilenameCallbackDelegate(string name);

		[BizImport(cc)]
		public abstract void mame_nvram_get_filenames(FilenameCallbackDelegate cb);

		[BizImport(cc)]
		public abstract void mame_nvram_save();

		[BizImport(cc)]
		public abstract void mame_nvram_load();

		// log
		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCallbackDelegate(OutputChannel channel, int size, string data);

		[BizImport(cc)]
		public abstract void mame_set_log_callback(LogCallbackDelegate cb);

		// base time
		[UnmanagedFunctionPointer(cc)]
		public delegate long BaseTimeCallbackDelegate();

		[BizImport(cc)]
		public abstract void mame_set_base_time_callback(BaseTimeCallbackDelegate cb);

		// input poll
		[UnmanagedFunctionPointer(cc)]
		public delegate void InputPollCallbackDelegate();

		[BizImport(cc)]
		public abstract void mame_set_input_poll_callback(InputPollCallbackDelegate cb);

		// execute
		[BizImport(cc)]
		public abstract void mame_lua_execute(string code);

		// get int
		[BizImport(cc)]
		public abstract int mame_lua_get_int(string code);

		// get long
		// nb: this is actually a double cast to long internally
		[BizImport(cc)]
		public abstract long mame_lua_get_long(string code);

		// get bool
		[BizImport(cc)]
		public abstract bool mame_lua_get_bool(string code);

		/// <summary>
		/// MAME's luaengine uses lua strings to return C strings as well as
		/// binary buffers. You're meant to know which you're going to get and
		/// handle that accordingly. When we want to get a C string, we
		/// Marshal.PtrToStringAnsi(). With buffers, we Marshal.Copy()
		/// to our new buffer. MameGetString() only covers the former
		/// because it's the same steps every time, while buffers use to
		/// need aditional logic. In both cases MAME wants us to manually
		/// free the string buffer. It's made that way to make the buffer
		/// persist actoss C API calls.
		/// </summary>

		// get string
		[BizImport(cc)]
		public abstract IntPtr mame_lua_get_string(string code, out int length);

		// free string
		[BizImport(cc)]
		public abstract bool mame_lua_free_string(IntPtr pointer);
	}
}
