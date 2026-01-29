using System.Runtime.CompilerServices;
using Random = UnityEngine.Random;

namespace RegionKit.Modules.ShelterBehaviors
{
	public class ShelterDataManager
	{
		private static readonly ConditionalWeakTable<Room, ShelterDataManager> dataCWT = new();
		public static bool TryGetShelterDataManager(Room room, out ShelterDataManager? data)
		{
			if (dataCWT.TryGetValue(room, out var shelterDataManager))
			{
				data = shelterDataManager;
				return true;
			}
            else if (room.abstractRoom.shelter && !room.abstractRoom.isAncientShelter)
            {
                data = new ShelterDataManager(room);
                dataCWT.Add(room, data);
                return true;
            }
            else
			{
				data = null;
				return false;
			}
		}

		private readonly Room room;

		private readonly List<Vector2> spawnPoints = [];
		private readonly List<IntRect> triggerZones = [];
		private readonly List<IntRect> noTriggerZones = [];

		private ShelterDataManager(Room room)
		{
			this.room = room;
			foreach (PlacedObject po in room.roomSettings.placedObjects)
			{
				if (po.type == _Enums.ShelterBhvrSpawnPosition)
				{
					spawnPoints.Add(po.pos);
				}
				else if (po.type == _Enums.ShelterBhvrTriggerZone)
				{
					triggerZones.Add((po.data as PlacedObject.GridRectObjectData)!.Rect);
				}
				else if (po.type == _Enums.ShelterBhvrNoTriggerZone)
				{
					noTriggerZones.Add((po.data as PlacedObject.GridRectObjectData)!.Rect);
				}
			}
		}

		public Vector2? RandomSpawnPoint()
		{
			if (spawnPoints.Count == 0)
			{
				return null;
			}
			else if (spawnPoints.Count == 1)
			{
				return spawnPoints[0];
			}

			int seed = room.game.clock + room.game.GetStorySession.saveState.seed + room.game.GetStorySession.saveState.cycleNumber;
			Random.State oldState = Random.state;
			Random.InitState(seed);
			Vector2 pos = spawnPoints[Random.Range(0, spawnPoints.Count)];
			Random.state = oldState;
			return pos;
        }

		public bool TryGetRandomSpawnPoint(out Vector2? spawnPoint)
		{
			return (spawnPoint = RandomSpawnPoint()) != null;
		}

		public bool ZoneCheck(Creature critter)
		{
			bool inZone = true;
			foreach (BodyChunk chunk in critter.bodyChunks)
			{
				inZone &= TileInZones(room.GetTilePosition(chunk.pos));
			}
			return inZone;
		}

		public bool TileInZones(IntVector2 tile)
		{
			bool inZone = true;
			foreach (IntRect triggerRect in triggerZones)
			{
				inZone &= triggerRect.Contains(tile);
			}
			foreach (IntRect noTriggerRect in noTriggerZones)
			{
				inZone &= !noTriggerRect.Contains(tile);
			}
			return inZone;
		}
	}
}
