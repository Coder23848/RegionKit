
using MonoMod.Cil;

namespace RegionKit.Modules.ShelterBehaviors;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Shelter Behaviors")]
public static class _Module
{
	public const string SHELTERS_POM_CATEGORY = RK_POM_CATEGORY + "-Shelters";

	internal static void Setup()
	{
		RegisterManagedObject<HoldToTriggerTutorialObject, HoldToTriggerTutorialData, ManagedRepresentation>(nameof(_Enums.ShelterBhvrHTTTutorial), SHELTERS_POM_CATEGORY);
		RegisterFullyManagedObjectType(new ManagedField[]{
				new IntVector2Field("dir", new RWCustom.IntVector2(0,1), IntVector2Field.IntVectorReprType.fourdir), }
		, null!, nameof(_Enums.ShelterBhvrPlacedDoor), SHELTERS_POM_CATEGORY);
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrNoTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrSpawnPosition), SHELTERS_POM_CATEGORY, null!, null!); // No data required :)
	}

	internal static void Enable()
	{
		try
		{
			On.ShelterDoor.Close += ShelterDoor_Close;
			IL.ShelterDoor.ctor += ShelterDoor_ctor;
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
			On.ShelterDoor.Close -= ShelterDoor_Close;
			IL.ShelterDoor.ctor -= ShelterDoor_ctor;
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private static void ShelterDoor_ctor(ILContext il)
	{
		var c = new ILCursor(il);
	}

	private static void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
	{
		orig(self);
	}
}
