namespace RegionKit.Modules.Objects
{
	public class ColoredSSFuses : SuperStructureFuses
	{
		public ColoredSSFusesData Data => (placedObject.data as ColoredSSFusesData)!;

		public Color ActiveColor => Data.ActiveColor;
		public Color BrokenColor => Data.BrokenColor;

		public ColoredSSFuses(PlacedObject placedObject, Room room) : base(placedObject, (placedObject.data as ColoredSSFusesData)!.Rect, room)
		{
		}

		public override void Update(bool eu)
		{
			broken = Data.BrokenAmt;
			rect = Data.Rect;
			base.Update(eu);
		}

		public void DrawSpritesExtra(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker)
		{
			if (culled || debugMode > 0 || slatedForDeletetion || room != rCam.room) return;

			float num = Mathf.Lerp(lastLightness, lightness, timeStacker);
			int sp = 0;
			for (int j = 0; j < lights.GetLength(0); j++)
			{
				for (int k = 0; k < lights.GetLength(1); k++)
				{
					if (smoothedMalfunction > 0.2f && lights[j, k, 0] > 0.5f && Custom.InsideRect(new IntVector2(j, k), malfunctionRect))
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, BrokenColor, Mathf.Lerp(1f, 0.25f, Mathf.Pow(smoothedMalfunction, 0.2f)) * powerFlicker);
					}
					else if (lights[j, k, 3] == 5f)
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, BrokenColor, (((int)lights[j, k, 4] % 8 > 3) ? 1f : 0f) * Mathf.Lerp(1f, 0.25f, Mathf.Pow(smoothedMalfunction, 0.2f)) * powerFlicker);
					}
					else
					{
						sLeaser.sprites[sp].color = Color.Lerp(Color.black, ActiveColor, Mathf.Pow(Mathf.Max(0f, Mathf.Lerp(lights[j, k, 1], lights[j, k, 0], timeStacker)), 2.5f - num) * Mathf.Lerp(0.3f, 1f, num) * Mathf.Lerp(1f, 0.5f, Mathf.Pow(smoothedMalfunction, 0.2f)) * powerFlicker);
					}
					sp++;
				}
			}
		}

		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
		
		internal static void Apply()
		{
			On.SuperStructureFuses.DrawSprites += SuperStructureFuses_DrawSprites;
		}

		internal static void Undo()
		{
			On.SuperStructureFuses.DrawSprites -= SuperStructureFuses_DrawSprites;
		}

		private static void SuperStructureFuses_DrawSprites(On.SuperStructureFuses.orig_DrawSprites orig, SuperStructureFuses self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			orig(self, sLeaser, rCam, timeStacker, camPos);
			(self as ColoredSSFuses)?.DrawSpritesExtra(sLeaser, rCam, timeStacker);
		}
	}
}
