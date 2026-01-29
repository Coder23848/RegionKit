
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace RegionKit.Modules.ShelterBehaviors;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Shelter Behaviors")]
public static class _Module
{
	public const string SHELTERS_POM_CATEGORY = RK_POM_CATEGORY + "-Shelters";

	internal static void Setup()
	{
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrNoTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrSpawnPosition), SHELTERS_POM_CATEGORY, null!, null!); // No data required :)
		RegisterFullyManagedObjectType([new IntVector2Field("dir", new IntVector2(0,1), IntVector2Field.IntVectorReprType.fourdir)], null!, nameof(_Enums.ShelterBhvrPlacedDoor), SHELTERS_POM_CATEGORY);
		RegisterManagedObject<HoldToTriggerTutorialObject, HoldToTriggerTutorialData, ManagedRepresentation>(nameof(_Enums.ShelterBhvrHTTTutorial), SHELTERS_POM_CATEGORY);
		RegisterFullyManagedObjectType([
			new IntegerField("min", -1, 30, 3, displayName:"Consum. Cooldown Min"),
			new IntegerField("max", 0, 30, 6, displayName:"Consum. Cooldown Max"),
			], null!, nameof(_Enums.ShelterBhvrConsumableShelter), SHELTERS_POM_CATEGORY);
	}

	private static readonly List<IDetour> _manualHooks = [];

	internal static void Enable()
	{
		try
		{
			On.ShelterDoor.IsTileInsideShelterRange += ShelterDoor_IsTileInsideShelterRange;
			On.ShelterDoor.Close += ShelterDoor_Close;
			On.ShelterDoor.ctor += ShelterDoor_ctor;
			On.ShelterDoor.ShelterEntranceOverrides += ShelterDoor_ShelterEntranceOverrides;
			On.ShelterDoor.Update += ShelterDoor_Update;
			On.Room.AddObject += Room_AddObject;
			IL.Player.Update += Player_Update;
			_manualHooks.Add(new Hook(typeof(ShelterDoor).GetProperty(nameof(ShelterDoor.Broken)).GetGetMethod(), ShelterDoor_get_Broken));
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
			On.ShelterDoor.ShelterEntranceOverrides -= ShelterDoor_ShelterEntranceOverrides;
			On.ShelterDoor.Update -= ShelterDoor_Update;
			On.Room.AddObject -= Room_AddObject;
			IL.Player.Update -= Player_Update;
			foreach (IDetour hook in _manualHooks)
			{
				hook.Undo();
				hook.Dispose();
			}
			_manualHooks.Clear();
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
		// This part may be overkill but oh well
		if (ShelterDataManager.TryGetShelterDataManager(self.room, out var manager) && !self.room.PlayersInRoom.All(manager!.ZoneCheck))
		{
			return;
		}
		orig(self);
		if (self.IsClosing)
		{
			manager?.ConsumeShelter();
		}
	}

	private static void ShelterDoor_ctor(On.ShelterDoor.orig_ctor orig, ShelterDoor self, Room room)
	{
		orig(self, room);
		if (ShelterDataManager.TryGetShelterDataManager(room, out var manager) && manager!.TryGetRandomSpawnPoint(out var spawnPoint) && spawnPoint != null)
		{
			self.playerSpawnPos = room.GetTilePosition(spawnPoint.Value);
		}
	}

	private static IntVector2? ShelterDoor_ShelterEntranceOverrides(On.ShelterDoor.orig_ShelterEntranceOverrides orig, ShelterDoor self)
	{
		if (ShelterDataManager.TryGetShelterDataManager(self.room, out var manager) && manager!.firstShelterDoorSpot != null)
		{
			self.dir = manager.firstShelterDoorSpot.Value.dir.ToVector2();
			return manager.firstShelterDoorSpot.Value.pos;
		}
		return orig(self);
	}

	private static void ShelterDoor_Update(On.ShelterDoor.orig_Update orig, ShelterDoor self, bool eu)
	{
		float lastClosedFac = self.closedFac;
		orig(self, eu);
		if (lastClosedFac != self.closedFac)
		{
			ShelterEventHandler.FireShelterEvent(self.room, self.closedFac, self.closeSpeed);
		}
	}

	private static void Room_AddObject(On.Room.orig_AddObject orig, Room self, UpdatableAndDeletable obj)
	{
		orig(self, obj);
		if (obj is IReactToShelterEvents subscriber)
		{
			ShelterEventHandler.SubscribeObject(self, subscriber);
		}
	}

	private static void Player_Update(ILContext il)
	{
		var c = new ILCursor(il);
		int distLocRef = 40;
		ILLabel brIfFalse = null!;

		// Extend shelter door manhattan distance check
		c.GotoNext(x => x.MatchLdstr("wtdb_s02"));
		c.GotoNext(x => x.MatchStloc(out distLocRef));
		c.GotoNext(MoveType.After, x => x.MatchBle(out brIfFalse));
		
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc, distLocRef);
		c.EmitDelegate((Player self, int distance) =>
		{
			if (self.room != null && ShelterDataManager.TryGetShelterDataManager(self.room, out ShelterDataManager? manager) && manager != null)
			{
				IntVector2 pos = self.abstractCreature.pos.Tile;
                return manager.cosmeticShelterDoors.All(x => Custom.ManhattanDistance(x.origPos, pos) > distance); // All returns true if empty so this should be fine
			}
			return true;
		});
		c.Emit(OpCodes.Brfalse, brIfFalse);
	}

	private static bool ShelterDoor_get_Broken(Func<ShelterDoor, bool> orig, ShelterDoor self)
	{
		bool consumed = false;
		if (ShelterDataManager.TryGetShelterDataManager(self.room, out var manager) && manager != null)
		{
			consumed = manager.ShelterConsumed;
		}
		return orig(self) || consumed;
	}
}
