using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using UnityEngine;

namespace ElementsOfHarmony
{
	public static class Log
	{
		private static readonly object MessageMutex = new object();
		private static StreamWriter? LogFile;
		private static TcpClient? Client;
		private static NetworkStream? Stream;

		public static void InitDebug()
		{
			if (Settings.Debug)
			{
				if (Settings.DebugLog)
				{
					try
					{
						LogFile = new StreamWriter(Settings.DebugLogFile);
					}
					catch (Exception)
					{ }
				}
				if (Settings.DebugTCPEnabled)
				{
					try
					{
						// connect to local server to support immediate log display
						Client = new TcpClient();
						Client.Connect(new IPEndPoint(IPAddress.Parse(Settings.DebugTCPIP), Settings.DebugTCPPort));
						Stream = Client.GetStream();
						Message("Connection success");
					}
					catch (Exception e)
					{
					repeat:
						Message(StackTraceUtility.ExtractStackTrace());
						Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
						if (e.InnerException != null)
						{
							e = e.InnerException;
							goto repeat;
						}
					}
				}

				// attach our error handlers
				try
				{
					Application.logMessageReceived += LogCallback;
					AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;
				}
				catch (Exception e)
				{
				repeat:
					Message(StackTraceUtility.ExtractStackTrace());
					Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
					if (e.InnerException != null)
					{
						e = e.InnerException;
						goto repeat;
					}
				}

				try
				{
					Harmony element = new Harmony($"{typeof(Log).FullName}");
					int Num = 0;
					foreach (var Patch in typeof(Log).GetNestedTypes())
					{
						new PatchClassProcessor(element, Patch).Patch();
						Num++;
					}
					if (Num > 0)
					{
						Message($"Harmony patch for {typeof(Log).FullName} successful - {Num} Patches");
					}
				}
				catch (Exception e)
				{
				repeat:
					Message(StackTraceUtility.ExtractStackTrace());
					Message($"{e.GetType()}\n{e.StackTrace}\n{e.Message}");
					if (e.InnerException != null)
					{
						e = e.InnerException;
						goto repeat;
					}
				}
			}
		}

		public static void Message(string message)
		{
			lock (MessageMutex)
			{
				if (LogFile != null)
				{
					LogFile.WriteLine(message);
					LogFile.Flush();
				}
				if (Client != null && Stream != null && Client.Connected && Stream.CanWrite)
				{
					byte[] buffer = Encoding.UTF8.GetBytes(message);
					// first, send a 32-bit-integer to indicate the size of the string in bytes
					// if you're writing a receiving server in Java,
					// be aware that `int` is in little-endian format for C#
					// but Java is using big-endian by default
					Stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
					Stream.Flush();
					// then we send the actual string in UTF-8
					Stream.Write(buffer, 0, buffer.Length);
					Stream.Flush();
				}
			}
		}

		#region error handlers, made to record any error that may or may not caused by our mod

		public static void LogCallback(string condition, string stackTrace, UnityEngine.LogType type)
		{
			switch (type)
			{
				case LogType.Error:
				case LogType.Exception:
					Message(StackTraceUtility.ExtractStackTrace());
					Message($"condition: {condition}");
					Message($"stackTrace:\r\n{stackTrace}");
					Message("\r\n");
					break;
			}
		}
		public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			Message(StackTraceUtility.ExtractStackTrace());
			Message($"sender.GetType(): {sender.GetType()}");
			Message($"sender: {sender}");
			Message($"args: {args}");
			Message($"args.ExceptionObject.GetType(): {args.ExceptionObject.GetType()}");
			Message($"args.ExceptionObject: {args.ExceptionObject}");
			Message("\r\n");
		}

		[HarmonyPatch]
		public static class ExceptionConstructor
		{
			public static IEnumerable<MethodBase> TargetMethods()
			{
				return new MethodBase[] {
					AccessTools.Constructor(typeof(Exception), new Type[0]),
					AccessTools.Constructor(typeof(Exception), new Type[1]{ typeof(string) }),
					AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(string), typeof(Exception) }),
					AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(SerializationInfo), typeof(StreamingContext) }),
				};
			}
			public static void Postfix(Exception __instance)
			{
			repeat:
				if (!StackTraceUtility.ExtractStackTrace().Contains($"{nameof(ElementsOfHarmony)}.{nameof(Log)}.{nameof(Message)}")) // prevent infinite loop
				{
					Message(StackTraceUtility.ExtractStackTrace());
					Message($"Exception.StackTrace:\r\n{__instance.StackTrace}");
					Message($"Exception.GetType(): {__instance.GetType()}");
					Message($"Exception.HResult: 0x{__instance.HResult:X8}");
					Message($"Exception.Message: {__instance.Message}");
					Message("\r\n");
					if (__instance is FileLoadException fl)
					{
						Message($"FileLoadException.FileName: {fl.FileName}");
					}
					if (__instance is FileNotFoundException f)
					{
						Message($"FileNotFoundException.FileName: {f.FileName}");
					}
					if (__instance.InnerException != null)
					{
						__instance = __instance.InnerException;
						goto repeat;
					}
				}
			}
		}

		[HarmonyPatch(typeof(Debug), nameof(Debug.LogError), typeof(object))]
		public static class LogError
		{
			public static void Postfix(object message)
			{
				Message(StackTraceUtility.ExtractStackTrace());
				Message($"Message.GetType(): {message.GetType()}");
				Message($"Message: {message}");
				Message("\r\n");
			}
		}

		[HarmonyPatch(typeof(Debug), nameof(Debug.LogException), typeof(Exception))]
		public static class LogException
		{
			public static void Postfix(Exception exception)
			{
				Message(StackTraceUtility.ExtractStackTrace());
				Message($"Exception.StackTrace:\r\n{exception.StackTrace}");
				Message($"Exception.GetType(): {exception.GetType()}");
				Message($"Exception.HResult: 0x{exception.HResult:X8}");
				Message($"Exception.Message: {exception.Message}");
				Message("\r\n");
			}
		}

		[HarmonyPatch(typeof(Marshal), nameof(Marshal.GetExceptionForHR), typeof(int), typeof(IntPtr))]
		public static class LogMarshalHRException
		{
			public static void Postfix(int errorCode, Exception? __result)
			{
				if (__result == null) return;
				Message(StackTraceUtility.ExtractStackTrace());
				Message($"errorCode: 0x{(uint)errorCode:X8}");
				Message($"__result.StackTrace:\r\n{__result.StackTrace}");
				Message($"__result.GetType(): {__result.GetType()}");
				Message($"__result.HResult: 0x{__result.HResult:X8}");
				Message($"__result.Message: {__result.Message}");
				Message("\r\n");
			}
		}

		#endregion

		[DllImport("User32.dll", CharSet = CharSet.Unicode)]
		public extern static int MessageBox(IntPtr hWnd, string Text, string Caption, uint uType);

		public const uint MB_OK = 0x00000000;
		public const uint MB_OKCANCEL = 0x00000001;
		public const uint MB_ABORTRETRYIGNORE = 0x00000002;
		public const uint MB_YESNOCANCEL = 0x00000003;
		public const uint MB_YESNO = 0x00000004;
		public const uint MB_RETRYCANCEL = 0x00000005;
		public const uint MB_CANCELTRYCONTINUE = 0x00000006;

		public const uint MB_ICONERROR = 0x00000010;
		public const uint MB_ICONQUESTION = 0x00000020;
		public const uint MB_ICONWARNING = 0x00000030;
		public const uint MB_ICONINFORMATION = 0x00000040;

		public const uint IDOK = 1;
		public const uint IDCANCEL = 2;
		public const uint IDABORT = 3;
		public const uint IDRETRY = 4;
		public const uint IDIGNORE = 5;
		public const uint IDYES = 6;
		public const uint IDNO = 7;
		public const uint IDTRYAGAIN = 10;
		public const uint IDCONTINUE = 11;
	}
}
