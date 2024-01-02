using System.Text.Json;
using System.Reflection;
using CounterStrikeSharp.API;

namespace SurfTimer;

public class ConfigLoader<T> where T : class, new()
{

	public T Config {get; private set;}

	public bool Loaded {get; private set;} = false;

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
		string dirName = Path.GetDirectoryName(this.path)!;
		Directory.CreateDirectory(dirName);

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
			Console.WriteLine($"[CS2 Surf] Writing default config for: {this.relativePath}");
			this.Save();
			return;
		}

		string loadingText = this.Loaded ? "Re-loading" : "Loading";
		Console.WriteLine($"[CS2 Surf] {loadingText} configuration: {this.relativePath}");

		T? cfg = null;

		try
		{
			string rawJson = File.ReadAllText(this.path);
			cfg = JsonSerializer.Deserialize<T>(rawJson);
		}
		catch (Exception e)
		{
			Console.WriteLine($"[CS2 Surf] Error while trying to load config: {this.relativePath}");
			Console.WriteLine($"[CS2 Surf] Config Error: {e.Message}");
		}

		if (cfg == null)
		{
			Console.WriteLine($"[CS2 Surf] Failed to load config! For file: {this.relativePath}");
			return;
		}

		this.Config = cfg;
		this.Loaded = true;
	}

}
