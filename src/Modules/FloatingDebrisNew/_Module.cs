using Watcher;

namespace RegionKit.Modules.FloatingDebrisNew;

[RegionKitModule(nameof(Enable), nameof(Disable), moduleName: "Floating Debris")]
internal static class _Module
{
	internal static void Enable()
	{
		try
		{
			LoadShaders();
			FloatingDebris.types["Dust"] = new Dust.DustSpawner(false);
			FloatingDebris.types["White Dust"] = new Dust.DustSpawner(true);
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	internal static void Disable()
	{
		try
		{
			FloatingDebris.types.Remove("Dust");
			FloatingDebris.types.Remove("White Dust");
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private static void LoadShaders()
	{
		AssetBundle bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath("assets/regionkit/rkfloatingdebris"));
		Custom.rainWorld.Shaders["RKDust"] = FShader.CreateShader("RKDust", bundle.LoadAsset<Shader>("Assets/Shaders/FloatingDust.shader"));
		Custom.rainWorld.Shaders["RKWhiteDust"] = FShader.CreateShader("RKWhiteDust", bundle.LoadAsset<Shader>("Assets/Shaders/FloatingDust.shader"), ["lightdust"]);
	}
}
