using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace OutlineEffect.Game.Tests.Visual;

[TestFixture]
public partial class TestSceneOutlineEffect : OutlineEffectTestScene
{
    private OutlineContainer container;

    [BackgroundDependencyLoader]
    private void load()
    {
        Add(container = new SpriteText
        {
            Text = "I have an outline!",
            Font = new FontUsage(size: 96),
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
        }.WithEffect(new OutlineEffect
        {
            OutlineWidth = 10,
            OutlineColour = Color4.LightCoral
        }));
    }

    [Test]
    public void TestOutline()
    {
        AddSliderStep("Outline width", 0f, 32f, 10f, value => container.OutlineWidth = value);
    }
}
