using DevInterface;

namespace RegionKit.Modules.Objects
{
	internal class BGFlatLightRepresentation : ResizeableObjectRepresentation
	{
		public BGFlatLight.Data Data => (pObj.data as BGFlatLight.Data)!;

		private readonly BGFlatLightPanel panel;
		private readonly FSprite panelLine;

		public BGFlatLightRepresentation(DevUI owner, string IDstring, DevUINode parentNode, PlacedObject pObj, bool newObject) : base(owner, IDstring, parentNode, pObj, "BG Flat Light", true)
		{
			fSprites.Add(panelLine = new FSprite("pixel") { anchorY = 0f });
			subNodes.Add(panel = new BGFlatLightPanel(owner, IDstring, this, Data.panelPos));
			owner.placedObjectsContainer.AddChild(panelLine);

			if (newObject)
			{
				owner.room.AddObject(new BGFlatLight(pObj));
			}
		}

		public override void Refresh()
		{
			base.Refresh();

			Data.panelPos = panel.pos;

			panelLine.SetPosition(absPos + new Vector2(0.01f, 0.01f));
			panelLine.scaleY = Data.panelPos.magnitude;
			panelLine.rotation = Custom.AimFromOneVectorToAnother(absPos, panel.absPos);
		}

		private class BGFlatLightPanel : Panel, IDevUISignals
		{
			public BGFlatLightRepresentation Rep => (parentNode as BGFlatLightRepresentation)!;
			public BGFlatLight.Data Data => (Rep.pObj.data as BGFlatLight.Data)!;

			private Cycler modeCycler;
			private BGFlatLightSlider strengthSlider, redSlider, greenSlider, blueSlider;

			private BGFlatLight.Mode lastMode;

			public BGFlatLightPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 45f), "BG Flat Light")
			{
				subNodes.Add(modeCycler = new Cycler(owner, "BGFlatLight_Cycler", this, new Vector2(5f, 25f), 240f, "Mode: ", BGFlatLight.Mode.values.entries));
				subNodes.Add(strengthSlider = new BGFlatLightSlider(SliderType.Strength, owner, "BGFlatLight_Slider_Strength", this, new Vector2(5f, 5f), "Strengtth:"));

				modeCycler.currentAlternative = Math.Max(Data.mode.index, 0);

				// These get added in Refresh
				redSlider = null!;
				greenSlider = null!;
				blueSlider = null!;
				lastMode = null!;

				Refresh();
			}

			public override void Refresh()
			{
				// Update data
				Data.mode = new BGFlatLight.Mode(BGFlatLight.Mode.values.entries[Mathf.Clamp(modeCycler.currentAlternative, 0, BGFlatLight.Mode.values.Count)], false);

				// Update sliders
				if (lastMode != BGFlatLight.Mode.CustomColor && Data.mode == BGFlatLight.Mode.CustomColor)
				{
					redSlider = new BGFlatLightSlider(SliderType.Red, owner, "BGFlatLight_Slider_R", this, new Vector2(5f, 85f), "Red:");
					greenSlider = new BGFlatLightSlider(SliderType.Green, owner, "BGFlatLight_Slider_G", this, new Vector2(5f, 65f), "Green:");
					blueSlider = new BGFlatLightSlider(SliderType.Blue, owner, "BGFlatLight_Slider_B", this, new Vector2(5f, 45f), "Blue:");
					subNodes.Add(redSlider);
					subNodes.Add(greenSlider);
					subNodes.Add(blueSlider);
					size = new Vector2(250f, 105f);
				}
				else if (lastMode == BGFlatLight.Mode.CustomColor && Data.mode != BGFlatLight.Mode.CustomColor)
				{
					redSlider.ClearSprites();
					greenSlider.ClearSprites();
					blueSlider.ClearSprites();
					subNodes.Remove(redSlider);
					subNodes.Remove(greenSlider);
					subNodes.Remove(blueSlider);
					redSlider = null!;
					greenSlider = null!;
					blueSlider = null!;
					size = new Vector2(250f, 45f);
				}
				lastMode = Data.mode;

				// Need to refresh after in case of update
				base.Refresh();
			}

			public void Signal(DevUISignalType type, DevUINode sender, string message)
			{
			}

			private class BGFlatLightSlider : Slider
			{
				private readonly SliderType type;

				private BGFlatLight.Data Data => (parentNode as BGFlatLightPanel)!.Data;
				private BGFlatLightRepresentation Rep => (parentNode as BGFlatLightPanel)!.Rep;

				public BGFlatLightSlider(SliderType type, DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos, string title) : base(owner, IDstring, parentNode, pos, title, false, 110f)
				{
					this.type = type;
				}

				public override void Refresh()
				{
					base.Refresh();

					float num = type switch
					{
						SliderType.Strength => Data.strength,
						SliderType.Red => Data.r,
						SliderType.Green => Data.g,
						SliderType.Blue => Data.b,
						_ => throw new NotImplementedException(),
					};
					NumberText = num.ToString("0.000");
					RefreshNubPos(num);
				}

				public override void NubDragged(float nubPos)
				{
					switch (type)
					{
					case SliderType.Strength: Data.strength = nubPos; break;
					case SliderType.Red: Data.r = nubPos; break;
					case SliderType.Green: Data.g = nubPos; break;
					case SliderType.Blue: Data.b = nubPos; break;
					}

					Rep.Refresh();
					Refresh();
				}
			}

			private enum SliderType
			{
				Strength,
				Red,
				Green,
				Blue
			}
		}
	}
}
