using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;

namespace OutlineEffect.Game;

public class OutlineEffect : IEffect<OutlineContainer>
{
    public float OutlineWidth;

    public ColourInfo OutlineColour;

    public OutlineContainer ApplyTo(Drawable drawable) => new OutlineContainer
    {
        OutlineWidth = OutlineWidth,
        OutlineColour = OutlineColour,
    }.Wrap(drawable);
}
