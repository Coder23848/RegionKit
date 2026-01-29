
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
			On.ShelterDoor.IsTileInsideShelterRange += ShelterDoor_IsTileInsideShelterRange;
			On.ShelterDoor.Close += ShelterDoor_Close;
			On.ShelterDoor.ctor += ShelterDoor_ctor;
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
			On.ShelterDoor.IsTileInsideShelterRange -= ShelterDoor_IsTileInsideShelterRange;
			On.ShelterDoor.Close -= ShelterDoor_Close;
			On.ShelterDoor.ctor -= ShelterDoor_ctor;
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private static bool ShelterDoor_IsTileInsideShelterRange(On.ShelterDoor.orig_IsTileInsideShelterRange orig, AbstractRoom room, IntVector2 tile)
	{
		bool flag = true;
		if (room.realizedRoom is Room r && ShelterDataManager.TryGetShelterDataManager(r, out var manager))
		{
			flag = manager!.TileInZones(tile);
		}
		return flag && orig(room, tile);
	}

	private static void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
	{
		if (ShelterDataManager.TryGetShelterDataManager(self.room, out var manager) && !self.room.PlayersInRoom.All(manager!.ZoneCheck))
		{
			return;
		}
		orig(self);
	}

	private static void ShelterDoor_ctor(On.ShelterDoor.orig_ctor orig, ShelterDoor self, Room room)
	{
		orig(self, room);
		if (ShelterDataManager.TryGetShelterDataManager(room, out var manager) && manager!.TryGetRandomSpawnPoint(out var spawnPoint) && spawnPoint != null)
		{
			self.playerSpawnPos = room.GetTilePosition(spawnPoint.Value);
		}
	}
}
