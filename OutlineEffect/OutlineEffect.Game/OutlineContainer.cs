using System.Numerics;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Layout;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;
using Vector2 = osuTK.Vector2;

namespace OutlineEffect.Game;

public partial class OutlineContainer : Container, IBufferedDrawable
{
    private ColourInfo outlineColour = Color4.White;

    public ColourInfo OutlineColour
    {
        get => outlineColour;
        set
        {
            if (outlineColour.Equals(value))
                return;

            outlineColour = value;
            Invalidate(Invalidation.DrawNode);
        }
    }

    private float outlineWidth;

    public float OutlineWidth
    {
        get => outlineWidth;
        set
        {
            if (outlineWidth.Equals(value))
                return;

            outlineWidth = value;
            Invalidate(Invalidation.DrawNode);
        }
    }

    private BlendingParameters effectBlending = BlendingParameters.Inherit;

    public BlendingParameters EffectBlending
    {
        get => effectBlending;
        set
        {
            if (effectBlending == value)
                return;

            effectBlending = value;
            Invalidate(Invalidation.DrawNode);
        }
    }

    private Color4 backgroundColour = new Color4(0, 0, 0, 0);

    /// <summary>
    /// The background colour of the framebuffer. Transparent black by default.
    /// </summary>
    public Color4 BackgroundColour
    {
        get => backgroundColour;
        set
        {
            if (backgroundColour == value)
                return;

            backgroundColour = value;
            ForceRedraw();
        }
    }


    private Vector2 frameBufferScale = Vector2.One;

    public Vector2 FrameBufferScale
    {
        get => frameBufferScale;
        set
        {
            if (frameBufferScale == value)
                return;

            frameBufferScale = value;
            ForceRedraw();
        }
    }

    public readonly bool UsingCachedFrameBuffer;

    private bool redrawOnScale = true;

    public bool RedrawOnScale
    {
        get => redrawOnScale;
        set
        {
            if (redrawOnScale == value)
                return;

            redrawOnScale = value;
            screenSpaceSizeBacking?.Invalidate();
        }
    }

    /// <summary>
    /// Forces a redraw of the framebuffer before it is blitted the next time.
    /// Only relevant if <see cref="UsingCachedFrameBuffer"/> is true.
    /// </summary>
    public void ForceRedraw() => Invalidate(Invalidation.DrawNode);

    /// <summary>
    /// In order to signal the draw thread to re-draw the buffered container we version it.
    /// Our own version (update) keeps track of which version we are on, whereas the
    /// drawVersion keeps track of the version the draw thread is on.
    /// When forcing a redraw we increment updateVersion, pass it into each new drawnode
    /// and the draw thread will realize its drawVersion is lagging behind, thus redrawing.
    /// </summary>
    private long updateVersion;

    public IShader TextureShader { get; private set; }
    private IShader jumpFloodShader;
    private IShader outlineShader;

    private readonly OutlineContainerDrawNodeSharedData sharedData = new OutlineContainerDrawNodeSharedData();

    public OutlineContainer(bool cachedFrameBuffer = true)
    {
        UsingCachedFrameBuffer = cachedFrameBuffer;

        AddLayout(screenSpaceSizeBacking);
    }

    [BackgroundDependencyLoader]
    private void load(ShaderManager shaders)
    {
        TextureShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, FragmentShaderDescriptor.TEXTURE);
        jumpFloodShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, OutlineDrawNode.JUMP_FLOOD_SHADER);
        outlineShader = shaders.Load(VertexShaderDescriptor.TEXTURE_2, OutlineDrawNode.OUTLINE_SHADER);
    }

    protected override DrawNode CreateDrawNode() => new OutlineDrawNode(this, sharedData);

    public override bool UpdateSubTreeMasking()
    {
        bool result = base.UpdateSubTreeMasking();

        childrenUpdateVersion = updateVersion;

        return result;
    }

    protected override RectangleF ComputeChildMaskingBounds() =>
        base.ScreenSpaceDrawQuad.AABBFloat; // Make sure children never get masked away

    private Vector2 lastScreenSpaceSize;

    // We actually only care about Invalidation.MiscGeometry | Invalidation.DrawInfo
    private readonly LayoutValue screenSpaceSizeBacking =
        new LayoutValue(Invalidation.Presence | Invalidation.RequiredParentSizeToFit | Invalidation.DrawInfo);

    protected override bool OnInvalidate(Invalidation invalidation, InvalidationSource source)
    {
        bool result = base.OnInvalidate(invalidation, source);

        if ((invalidation & Invalidation.DrawNode) > 0)
        {
            ++updateVersion;
            result = true;
        }

        return result;
    }

    private long childrenUpdateVersion = -1;

    protected override bool RequiresChildrenUpdate =>
        base.RequiresChildrenUpdate && childrenUpdateVersion != updateVersion;

    protected override void Update()
    {
        base.Update();

        // Invalidate drawn frame buffer every frame.
        if (!UsingCachedFrameBuffer)
            ForceRedraw();
        else if (!screenSpaceSizeBacking.IsValid)
        {
            Vector2 drawSize = ScreenSpaceDrawQuad.AABBFloat.Size;

            if (!RedrawOnScale)
            {
                Matrix3 scaleMatrix = Matrix3.CreateScale(DrawInfo.MatrixInverse.ExtractScale());
                Vector2Extensions.Transform(ref drawSize, ref scaleMatrix, out drawSize);
            }

            if (!Precision.AlmostEquals(lastScreenSpaceSize, drawSize))
            {
                ++updateVersion;
                lastScreenSpaceSize = drawSize;
            }

            screenSpaceSizeBacking.Validate();
        }
    }

    public BlendingParameters DrawEffectBlending
    {
        get
        {
            BlendingParameters blending = EffectBlending;

            blending.CopyFromParent(Blending);
            blending.ApplyDefaultToInherited();

            return blending;
        }
    }

    public override Quad ScreenSpaceDrawQuad
    {
        get
        {
            var drawQuad = base.ScreenSpaceDrawQuad;

            // casting to int here to prevent flickering when adjusting the width
            float inflateAmount = BitOperations.RoundUpToPowerOf2((uint)(outlineWidth * DrawInfo.Matrix.ExtractScale().X));

            return drawQuad.AABBFloat.Inflate(inflateAmount);
        }
    }

    public DrawColourInfo? FrameBufferDrawColour => base.DrawColourInfo;

    public override DrawColourInfo DrawColourInfo
    {
        get
        {
            // Todo: This is incorrect.
            var blending = Blending;
            blending.ApplyDefaultToInherited();

            return new DrawColourInfo(Color4.White, blending);
        }
    }

    protected override void Dispose(bool isDisposing)
    {
        base.Dispose(isDisposing);

        sharedData.Dispose();
    }
}
