using System.Text.Json;
using System.Reflection;
using CounterStrikeSharp.API;

namespace SurfTimer;

public class ConfigLoader<T> where T : class, new()
{

	public T Config { get; private set; }

	private readonly string path;
	private readonly string relativePath;

	private readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions { WriteIndented = true };

	public ConfigLoader()
	{
		if (typeof(T).GetCustomAttribute(typeof(Cfg)) is not Cfg attr)
			throw new Exception();

		this.relativePath = attr.Path;
		this.path = Server.GameDirectory + attr.Path;
		this.Config = new T();

		this.Load();
	}

	public void Save(T config)
	{
		string jsonRaw = JsonSerializer.Serialize(config, this.jsonOpts);
		File.WriteAllText(this.path, jsonRaw);
	}

	public void Save()
	{
		this.Save(this.Config);
	}

	public void Load()
	{
		// Create default cfg
		if (!File.Exists(this.path))
		{
			Console.WriteLine($"Writing default config for '{this.relativePath}'");
			this.Save();
			return;
		}

		string rawJson = File.ReadAllText(this.path);
		T? cfg = JsonSerializer.Deserialize<T>(rawJson);

		if (cfg == null) return;

		this.Config = cfg;
	}

}
