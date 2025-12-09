using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevInterface;

namespace RegionKit.Modules.Objects.AdvancedShaderController
{
	public class AdvancedShaderUVPanel : Panel
	{
		public AdvancedShaderRepresentation rep => (parentNode.parentNode as AdvancedShaderRepresentation)!;
		public AdvancedShader.Data data => rep.data;

		private readonly UnboundVectorControl[] uvControls;
		private readonly Cycler restrictUVsButton;

		public AdvancedShaderUVPanel(DevUI owner, string IDstring, DevUINode parentNode, Vector2 pos) : base(owner, IDstring, parentNode, pos, new Vector2(250f, 265f), "Vertex UVs")
		{
			foreach (FSprite sprite in fSprites)
			{
				// fuck you
				sprite.RemoveFromContainer();
				owner.placedObjectsContainer.AddChild(sprite);
			}

			size = new Vector2(250f, 5f + 60f * data.vertices.Length + 20f);

			subNodes.Add(restrictUVsButton = new Cycler(owner, "AdvancedShader_UVPanel_Restrict", this, new Vector2(5f, size.y - 20f), 240f, "Clamp UVs: ", ["NO", "YES"]));
			restrictUVsButton.currentAlternative = data.restrictUVs ? 1 : 0;

			uvControls = new UnboundVectorControl[data.uvs.Length];
			for (int i = 0; i < data.uvs.Length; i++)
			{
				uvControls[i] = new UnboundVectorControl(owner, $"AdvancedShader_UVPanel_Vertex{i}", this, new Vector2(5f, 5f + 60f * (data.vertices.Length - i - 1)), 240f, data.uvs[i], data.restrictColors, $"Vertex {i}");
				subNodes.Add(uvControls[i]);
			}

			Refresh();
		}

		public override void Refresh()
		{
			base.Refresh();
			
			bool restrictUVs = restrictUVsButton.currentAlternative == 1;
			if (restrictUVs != data.restrictUVs)
			{
				data.restrictUVs = restrictUVs;
				for (int i = 0; i < uvControls.Length; i++)
				{
					uvControls[i].Restrict = data.restrictUVs;
				}
			}

			for (int i = 0; i < uvControls.Length; i++)
			{
				data.uvs[i] = uvControls[i].Value;
			}
		}
	}
}
