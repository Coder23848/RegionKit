using System.Globalization;
using System.Text.RegularExpressions;

namespace RegionKit.Modules.Objects
{
	public class BGFlatLight : CosmeticSprite
	{
		public readonly PlacedObject pObj;
		public Data data => (pObj.data as Data)!;

		public BGFlatLight(PlacedObject pObj)
		{
			this.pObj = pObj;
			pos = pObj.pos;
		}

		public override void Update(bool eu)
		{
			base.Update(eu);
			pos = pObj.pos;
		}

		public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
		{
			sLeaser.sprites = [
				new FSprite("Futile_White") {
					shader = rCam.game.rainWorld.Shaders["BGFlatLight"]
				}];
			AddToContainer(sLeaser, rCam, null!);
		}

		public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
		{
			sLeaser.sprites[0].SetPosition(pos - camPos);
			sLeaser.sprites[0].color = GetColor(rCam);
			sLeaser.sprites[0].scale = data.handlePos.magnitude / 8f;
		}

		public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
		{
			newContatiner ??= rCam.ReturnFContainer("ForegroundLights");
			newContatiner.AddChild(sLeaser.sprites[0]);
		}

		public Color GetColor(RoomCamera rCam)
		{
			// GetPixel operates from bottom left
			if (data.mode == Mode.EffectColor1)
			{
				return rCam.paletteTexture.GetPixel(30, 5);
			}
			if (data.mode == Mode.EffectColor1Far)
			{
				return rCam.paletteTexture.GetPixel(30, 4);
			}
			if (data.mode == Mode.EffectColor2)
			{
				return rCam.paletteTexture.GetPixel(30, 3);
			}
			if (data.mode == Mode.EffectColor2Far)
			{
				return rCam.paletteTexture.GetPixel(30, 2);
			}
			if (data.mode == Mode.White)
			{
				return rCam.paletteTexture.GetPixel(30, 1);
			}
			if (data.mode == Mode.FogColor)
			{
				return rCam.paletteTexture.GetPixel(1, 7);
			}
			return data.CustomColor;
		}

		public class Data : PlacedObject.ResizableObjectData
		{
			public Vector2 panelPos = new(100f, 100f);
			public Mode mode = Mode.CustomColor;
			public float r = 1f, g = 1f, b = 1f;
			public float strength = 1f;

			public Color CustomColor => new(r, g, b, strength);

			public Data(PlacedObject owner) : base(owner)
			{
			}

			public override string ToString()
			{
				string text = string.Format(CultureInfo.InvariantCulture,
					"{0}~{1}~{2}~{3}~{4}~{5}~{6}~{7}~{8}",
					handlePos.x,
					handlePos.y,
					panelPos.x,
					panelPos.y,
					mode,
					r, g, b,
					strength);
				text = SaveState.SetCustomData(this, text);
				return SaveUtils.AppendUnrecognizedStringAttrs(text, "~", unrecognizedAttributes);
			}

			public override void FromString(string s)
			{
				string[] array = Regex.Split(s, "~");
				handlePos.x = float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture);
				handlePos.y = float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.x = float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture);
				panelPos.y = float.Parse(array[3], NumberStyles.Any, CultureInfo.InvariantCulture);
				mode = new Mode(array[4], false);
				r = float.Parse(array[5], NumberStyles.Any, CultureInfo.InvariantCulture);
				g = float.Parse(array[6], NumberStyles.Any, CultureInfo.InvariantCulture);
				b = float.Parse(array[7], NumberStyles.Any, CultureInfo.InvariantCulture);
				strength = float.Parse(array[8], NumberStyles.Any, CultureInfo.InvariantCulture);
				unrecognizedAttributes = SaveUtils.PopulateUnrecognizedStringAttrs(array, 9);
			}
		}

		public class Mode(string value, bool register = false) : ExtEnum<Mode>(value, register)
		{
			public static readonly Mode CustomColor = new("Custom", true);
			public static readonly Mode EffectColor1 = new("EffectColor1", true);
			public static readonly Mode EffectColor1Far = new("EffectColor1Far", true);
			public static readonly Mode EffectColor2 = new("EffectColor2", true);
			public static readonly Mode EffectColor2Far = new("EffectColor2Far", true);
			public static readonly Mode White = new("White", true);
			public static readonly Mode FogColor = new("FogColor", true);
		}
	}
}
