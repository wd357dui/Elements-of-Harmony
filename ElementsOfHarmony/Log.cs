using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace ElementsOfHarmony
{
	public static class Log
	{
		private static readonly object MessageMutex = new object();
		private static StreamWriter LogFile;
		private static TcpClient Client;
		private static NetworkStream Stream;

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
						// this is optional
						Client = new TcpClient();
						Client.Connect(new IPEndPoint(IPAddress.Parse(Settings.DebugTCPIP), Settings.DebugTCPPort));
						Stream = Client.GetStream();
						Message("Connection success");
					}
					catch (Exception e)
					{
						Message(e.StackTrace + "\n" + e.Message);
					}
				}

				// attach our error handlers
				UnityEngine.Application.logMessageReceived += LogCallback;
				AppDomain.CurrentDomain.UnhandledException += ExceptionHandler;

				Harmony element = new Harmony($"{typeof(Log).FullName}");
				int Num = 0;
				foreach (var Patch in typeof(Log).GetNestedTypes())
				{
					element.CreateClassProcessor(Patch).Patch();
					Num++;
				}
				if (Num > 0)
				{
					Message($"Harmony patch for {typeof(Log).FullName} successful - {Num} Patches");
				}
			}
		}

		public static void Message(string message)
		{
            // everything we want to log will be written to "Elements of Harmony/Elements of Harmony.log"
            // and to the local server if connected
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
                    Stream.Write(BitConverter.GetBytes(buffer.Length), 0, 4);
                    Stream.Flush();
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
				case UnityEngine.LogType.Error:
				case UnityEngine.LogType.Exception:
					Message(Environment.StackTrace + "\n" +
						condition + "\n" +
						stackTrace + "\n");
					break;
			}
		}
		public static void ExceptionHandler(object sender, UnhandledExceptionEventArgs args)
		{
			Message(Environment.StackTrace + "\n" +
				sender.ToString() + "\n" +
				args.ToString() + "\n");
		}

		[HarmonyPatch]
        public static class ExceptionConstructor
		{
			public static IEnumerable<MethodBase> TargetMethods()
			{
				return new List<MethodBase> {
					AccessTools.Constructor(typeof(Exception), new Type[0]),
					AccessTools.Constructor(typeof(Exception), new Type[1]{ typeof(string) }),
					AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(string), typeof(Exception) }),
					AccessTools.Constructor(typeof(Exception), new Type[2]{ typeof(SerializationInfo), typeof(StreamingContext) }),
				};
			}
			public static void Postfix(Exception __instance)
			{
				if (!Environment.StackTrace.Contains($"{nameof(ElementsOfHarmony)}.{nameof(Log)}.{nameof(Message)}")) // prevent infinite loop
				{
					Message(Environment.StackTrace + "\n" + __instance.StackTrace + "\n" + __instance.Message + "\n");
				}
			}
		}

		[HarmonyPatch(typeof(UnityEngine.Debug))]
		[HarmonyPatch(nameof(UnityEngine.Debug.LogError), typeof(object))]
		public static class LogError
		{
			public static void Postfix(object message)
			{
				Message(Environment.StackTrace + "\n" + message.ToString());
			}
		}

		[HarmonyPatch(typeof(UnityEngine.Debug))]
		[HarmonyPatch(nameof(UnityEngine.Debug.LogException), typeof(Exception))]
		public static class LogException
		{
			public static void Postfix(Exception exception)
			{
				Message(Environment.StackTrace + "\n" +
					exception.StackTrace + "\n" +
					exception.GetType().FullName + "\n" +
					exception.Message);
			}
		}

		#endregion

	}
}
