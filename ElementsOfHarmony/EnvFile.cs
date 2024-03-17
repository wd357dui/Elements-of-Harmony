using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ElementsOfHarmony
{
	public class EnvFile
	{
		private class KeyValuePair
		{
			public string Name;
			public string Value;
		}

		private readonly List<KeyValuePair> keyValuePairs = new List<KeyValuePair>();

		private readonly string Path;

		public EnvFile(string FilePath)
		{
			Path = FilePath;
			if (File.Exists(Path))
			{
				string[] lines = File.ReadAllLines(Path);
				foreach (string line in lines)
				{
					string[] parts = line.Split('=');
					if (parts.Length >= 2)
					{
						string name = parts[0].Trim();
						string value = String.Join("=", parts.Skip(1).ToArray()).Trim();
						if (name == "") continue;
						keyValuePairs.Add(
							new KeyValuePair()
							{
								Name = name,
								Value = value
							}
						);
					}
				}
			}
		}

		public string ReadString(string name, string defaultValue = null)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				return keyValuePair.Value;
			}
			return defaultValue;
		}

		public void WriteString(string name, string value)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				keyValuePair.Value = value;
				return;
			}
			keyValuePair = new KeyValuePair()
			{
				Name = name,
				Value = value
			};
			keyValuePairs.Add(keyValuePair);
		}

		public bool ReadBoolean(string name, bool defaultValue = false)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				return keyValuePair.Value == "true";
			}
			return defaultValue;
		}

		public void WriteBoolean(string name, bool value)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				keyValuePair.Value = value ? "true" : "false";
				return;
			}
			keyValuePair = new KeyValuePair()
			{
				Name = name,
				Value = value ? "true" : "false"
			};
			keyValuePairs.Add(keyValuePair);
		}

		public int ReadInteger(string name, int defaultValue = 0)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				try
				{
					return int.Parse(keyValuePair.Value);
				}
				catch (Exception) { }
			}
			return defaultValue;
		}

		public void WriteInteger(string name, int value)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				keyValuePair.Value = value.ToString();
				return;
			}
			keyValuePair = new KeyValuePair()
			{
				Name = name,
				Value = value.ToString()
			};
			keyValuePairs.Add(keyValuePair);
		}

		public bool? ReadBooleanOptional(string name, bool? defaultValue = false)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				return keyValuePair.Value == "true";
			}
			return defaultValue;
		}

		public void WriteBooleanOptional(string name, bool? value)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				keyValuePair.Value = value.HasValue ? (value.Value ? "true" : "false") : "";
				return;
			}
			keyValuePair = new KeyValuePair()
			{
				Name = name,
				Value = value.HasValue ? (value.Value ? "true" : "false") : ""
			};
			keyValuePairs.Add(keyValuePair);
		}

		public int? ReadIntegerOptional(string name, int? defaultValue = 0)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				try
				{
					return int.Parse(keyValuePair.Value);
				}
				catch (Exception) { }
			}
			return defaultValue;
		}

		public void WriteIntegerOptional(string name, int? value)
		{
			var keyValuePair = keyValuePairs.FirstOrDefault(x => x.Name == name);
			if (keyValuePair != null)
			{
				keyValuePair.Value = value?.ToString() ?? "";
				return;
			}
			keyValuePair = new KeyValuePair()
			{
				Name = name,
				Value = value?.ToString() ?? ""
			};
			keyValuePairs.Add(keyValuePair);
		}

		public void SaveConfig()
		{
			string output = "";
			foreach (KeyValuePair keyValuePair in keyValuePairs)
			{
				output += keyValuePair.Name + "=" + keyValuePair.Value + "\n";
			}
			try
			{
				File.WriteAllText(Path, output);
			}
			catch (Exception) { }
		}
	}
}
