using System.Text;
using System.Text.RegularExpressions;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace RegionKit.Modules.ShelterBehaviors;
///<inheritdoc/>
[RegionKitModule(nameof(Enable), nameof(Disable), nameof(Setup), moduleName: "Shelter Behaviors")]
public static class _Module
{
	public const string SHELTERS_POM_CATEGORY = RK_POM_CATEGORY + "-Shelters";
	public const string CONSUMED_SHELTERS_SAVE_KEY = "REGIONKIT_CONSUMEDSHELTERS";

	internal static void Setup()
	{
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrNoTriggerZone), SHELTERS_POM_CATEGORY, typeof(PlacedObject.GridRectObjectData), typeof(DevInterface.GridRectObjectRepresentation));
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrSpawnPosition), SHELTERS_POM_CATEGORY, null!, null!); // No data required :)
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrDoorless), SHELTERS_POM_CATEGORY, null!, null!);
		RegisterEmptyObjectType(nameof(_Enums.ShelterBhvrHoldToTrigger), SHELTERS_POM_CATEGORY, null!, null!);
		RegisterFullyManagedObjectType([new IntVector2Field("dir", new IntVector2(0,1), IntVector2Field.IntVectorReprType.fourdir)], null!, nameof(_Enums.ShelterBhvrPlacedDoor), SHELTERS_POM_CATEGORY);
		//RegisterManagedObject<HoldToTriggerTutorialObject, HoldToTriggerTutorialData, ManagedRepresentation>(nameof(_Enums.ShelterBhvrHTTTutorial), SHELTERS_POM_CATEGORY);
		RegisterFullyManagedObjectType([
			new IntegerField("min", -1, 30, 3, displayName:"Cooldown Min"),
			new IntegerField("max", 0, 30, 6, displayName:"Cooldown Max"),
			new FloatField("chance", 0f, 1f, 1f, displayName: "Trigger Chance"),
			], null!, nameof(_Enums.ShelterBhvrConsumableShelter), SHELTERS_POM_CATEGORY);
	}

	internal static void Enable()
	{
		try
		{
			On.ShelterDoor.IsTileInsideShelterRange += ShelterDoor_IsTileInsideShelterRange;
			On.ShelterDoor.Close += ShelterDoor_Close;
			On.ShelterDoor.ctor += ShelterDoor_ctor;
			On.ShelterDoor.ShelterEntranceOverrides += ShelterDoor_ShelterEntranceOverrides;
			On.ShelterDoor.Update += ShelterDoor_Update;
			On.ShelterDoor.DrawSprites += ShelterDoor_DrawSprites;
			On.Room.AddObject += Room_AddObject;
			//On.HUD.FoodMeter.Draw += FoodMeter_Draw;
			IL.Player.Update += Player_Update;
			
			On.RegionState.ctor += RegionState_ctor;
			On.RegionState.SaveToString += RegionState_SaveToString;

			On.Player.Update += (orig, self, eu) =>
			{
				orig(self, eu);
				if (self.room is not null) DebugDrawing.DrawText(self.room, "Force sleep: " + self.forceSleepCounter, self.firstChunk.pos, Color.white);
			};
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
			On.ShelterDoor.DrawSprites -= ShelterDoor_DrawSprites;
			On.Room.AddObject -= Room_AddObject;
			//On.HUD.FoodMeter.Draw -= FoodMeter_Draw;
			IL.Player.Update -= Player_Update;
			
			On.RegionState.ctor -= RegionState_ctor;
			On.RegionState.SaveToString -= RegionState_SaveToString;
		}
		catch (Exception ex)
		{
			LogError(ex);
		}
	}

	private static bool ShelterDoor_IsTileInsideShelterRange(On.ShelterDoor.orig_IsTileInsideShelterRange orig, AbstractRoom room, IntVector2 tile)
	{
		bool flag = true;
		if (room.realizedRoom is Room r && ShelterDataManager.TryGetManager(r, out var manager))
		{
			flag = manager!.TileInZones(tile);
		}
		return flag && orig(room, tile);
	}

	private static void ShelterDoor_Close(On.ShelterDoor.orig_Close orig, ShelterDoor self)
	{
		// This part may be overkill but oh well
		if (ShelterDataManager.TryGetManager(self.room, out var manager) && !self.room.PlayersInRoom.All(manager!.ZoneCheck))
		{
			return;
		}
		orig(self);
		if (self.IsClosing && manager != null)
		{
			manager.ConsumeShelter();
			if (manager.doorless)
			{
				self.closedFac = 1f;
				self.closeSpeed = 1f;
				ShelterEventHandler.FireShelterEvent(self.room, 1f, 1f);
				self.DoorClosed();
			}
		}
	}

	private static void ShelterDoor_ctor(On.ShelterDoor.orig_ctor orig, ShelterDoor self, Room room)
	{
		orig(self, room);
		if (ShelterDataManager.TryGetManager(room, out var manager))
		{
			if (manager!.TryGetRandomSpawnPoint(out var spawnPoint) && spawnPoint != null)
			{
				self.playerSpawnPos = room.GetTilePosition(spawnPoint.Value);
			}

			if (self.closedFac > 0 && manager.doorless)
			{
				self.closedFac = 0f;
				self.closeSpeed = -1f;
				self.openUpTicks = 0f;
				self.initialWait = 0f;
			}
		}
	}

	private static IntVector2? ShelterDoor_ShelterEntranceOverrides(On.ShelterDoor.orig_ShelterEntranceOverrides orig, ShelterDoor self)
	{
		if (ShelterDataManager.TryGetManager(self.room, out var manager) && manager!.firstShelterDoorSpot != null)
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

	private static void ShelterDoor_DrawSprites(On.ShelterDoor.orig_DrawSprites orig, ShelterDoor self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
	{
		orig(self, sLeaser, rCam, timeStacker, camPos);
		if (ShelterDataManager.TryGetManager(self.room, out var manager) && manager != null && manager.doorless)
		{
			foreach (var sprite in sLeaser.sprites)
			{
				sprite.isVisible = false;
			}
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

	private static void FoodMeter_Draw(On.HUD.FoodMeter.orig_Draw orig, HUD.FoodMeter self, float timeStacker)
	{
		orig(self, timeStacker);
		if (self.hud.owner is Player player
		    && player.room is not null
		    && player.room.abstractRoom.shelter
		    && player.playerState.foodInStomach + player.FoodInRoom(player.room, false)
		    >= (player.Malnourished ? player.MaxFoodInStomach : player.slugcatStats.foodToHibernate))
		{
			self.lineSprite.color = Color.white;
			self.lineSprite.x = self.DrawPos(timeStacker).x + self.CircleDistance(timeStacker) * (Mathf.Lerp(self.lastShowSurvLim, self.showSurvLim, self.timeCounter) - 0.25f);
		}
	}

	private static void Player_Update(ILContext il)
	{
		var c = new ILCursor(il);
		
		// Part 1: prevent forceSleepCounter = 0 in like 3 different places for hold to trigger shelters
		// Part 1a: conditional shortcut as false
		c.GotoNext(MoveType.After, x => x.MatchLdfld<Player>(nameof(Player.stillInStartShelter)));

		LogDebug(c);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((bool value, Player self) =>
		{
			if (ShelterDataManager.TryGetManager(self.room, out var manager) && manager!.holdToTrigger)
			{
				value = true;
			}
			return value;
		});

		// Part 1b: conditional shortcut as false
		ILLabel httBrTo = null!;
		c.GotoNext(x => x.MatchLdfld<RainCycle>(nameof(RainCycle.cycleLength)));
		c.GotoNext(MoveType.After, x => x.MatchBle(out httBrTo));

		LogDebug(c);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((Player self) => ShelterDataManager.TryGetManager(self.room, out var manager) && manager!.holdToTrigger);
		c.Emit(OpCodes.Brtrue, httBrTo);

		// Part 1c: add increase logic
		c.GotoNext(x => x.MatchLdfld<SaveState>(nameof(SaveState.malnourished)));
		c.GotoPrev(x => x.MatchCallOrCallvirt<PhysicalObject>(nameof(PhysicalObject.IsTileSolid)));
		c.GotoNext(MoveType.After, x => x.MatchBrfalse(out httBrTo));

		LogDebug(c);
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((Player self) =>
		{
			if (ShelterDataManager.TryGetManager(self.room, out var manager) && manager!.holdToTrigger)
			{
				LogDebug("TRIGGER FORCE SLEEP!");
				self.forceSleepCounter++;
				return true;
			}
			return false;
		});
		c.Emit(OpCodes.Brtrue, httBrTo);
		
		// Part 2: reduce shelter door manhattan distance
		int distLocRef = 40;
		c.GotoNext(x => x.MatchLdstr("wtdb_s02"));
		c.GotoPrev(MoveType.AfterLabel, x => x.MatchStloc(out distLocRef));
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((int distance, Player self) =>
		{
			if (self.room != null &&
			    ShelterDataManager.TryGetManager(self.room, out ShelterDataManager? manager) &&
			    manager != null &&
			    manager.holdToTrigger)
			{
				distance = 1;
			}

			return distance;
		});

		// Part 3: extend shelter door manhattan distance check
		ILLabel brIfFalse = null!;
		c.GotoNext(x => x.MatchLdstr("wtdb_s02"));
		c.GotoNext(x => x.MatchStloc(distLocRef));
		c.GotoNext(MoveType.After, x => x.MatchBle(out brIfFalse));
		
		c.Emit(OpCodes.Ldarg_0);
		c.Emit(OpCodes.Ldloc, distLocRef);
		c.EmitDelegate((Player self, int distance) =>
		{
			if (self.room != null && ShelterDataManager.TryGetManager(self.room, out ShelterDataManager? manager) && manager != null)
			{
				IntVector2 pos = self.abstractCreature.pos.Tile;
                return manager.cosmeticShelterDoors.All(x => Custom.ManhattanDistance(x.origPos, pos) > distance); // All returns true if empty so this should be fine
			}
			return true;
		});
		c.Emit(OpCodes.Brfalse, brIfFalse);
		
		// Part 4: hold to trigger doesn't trigger the normal way
		c.GotoNext(MoveType.After, x => x.MatchLdfld<Player>(nameof(Player.touchedNoInputCounter)));
		
		c.Emit(OpCodes.Ldarg_0);
		c.EmitDelegate((int touchedNoInputCounter, Player self) =>
		{
			if (self.room != null &&
			    ShelterDataManager.TryGetManager(self.room, out ShelterDataManager? manager) &&
			    manager != null &&
			    manager.holdToTrigger)
			{
				touchedNoInputCounter = 0;
			}
			
			return touchedNoInputCounter;
		});
	}

	private static void RegionState_ctor(On.RegionState.orig_ctor orig, RegionState self, SaveState saveState, World world)
	{
		orig(self, saveState, world);

		// Load past consumed shelters
		ShelterDataManager.consumedShelters.Clear();
		string? consumedSheltersString = self.unrecognizedSaveStrings.FirstOrDefault(x => x.StartsWith(CONSUMED_SHELTERS_SAVE_KEY));
		if (consumedSheltersString != null && world.game.IsStorySession)
		{
			consumedSheltersString = Regex.Split(consumedSheltersString, "<rgB>")[1];
			var consumedShelters = Regex.Split(consumedSheltersString, "<rgC>");
			foreach (var str in consumedShelters)
			{
				var pair = str.Split('|');
				if (!ShelterDataManager.consumedShelters.ContainsKey(pair[0]) && RainWorld.roomNameToIndex.ContainsKey(pair[0]))
				{
					// Add to our dictionary
					int roomIndex = RainWorld.roomNameToIndex[pair[0]];
					int poIndex = int.Parse(pair[1]);
					if (!self.ItemConsumed(roomIndex, poIndex))
					{
						LogDebug($"CONSUMED SHELTER IN REGION STATE: {pair[0]}");
						ShelterDataManager.consumedShelters.Add(pair[0], poIndex);
					}
					
					// Also add it to the world broken shelters array
					int shelterIndex = world.shelters.IndexfOf(roomIndex);
					if (shelterIndex > -1)
					{
						world.brokenShelters[shelterIndex] = true;
					}
				}
			}
		}
	}

	private static string RegionState_SaveToString(On.RegionState.orig_SaveToString orig, RegionState self)
	{
		self.unrecognizedSaveStrings.RemoveAll(x => x.StartsWith(CONSUMED_SHELTERS_SAVE_KEY));
		if (ShelterDataManager.consumedShelters.Count > 0)
		{
			StringBuilder sb = new($"{CONSUMED_SHELTERS_SAVE_KEY}<rgB>");
			bool addSeparator = false;
			foreach (var pair in ShelterDataManager.consumedShelters)
			{
				if (addSeparator)
				{
					sb.Append("<rgC>");
				}
				addSeparator = true;
				sb.Append($"{pair.Key}|{pair.Value}");
			}

			self.unrecognizedSaveStrings.Add(sb.ToString());
		}
		return orig(self);
	}
}
