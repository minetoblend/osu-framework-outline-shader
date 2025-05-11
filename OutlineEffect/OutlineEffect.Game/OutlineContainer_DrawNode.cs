using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shaders;
using osu.Framework.Graphics.Shaders.Types;
using osuTK.Graphics;
using Vector2 = osuTK.Vector2;

namespace OutlineEffect.Game;

public partial class OutlineContainer
{
    private class OutlineDrawNode : BufferedDrawNode, ICompositeDrawNode
    {
        public const string JUMP_FLOOD_SHADER = "jump_flood";
        public const string OUTLINE_SHADER = "outline";

        protected new OutlineContainer Source => (OutlineContainer)base.Source;

        protected new CompositeDrawableDrawNode Child => (CompositeDrawableDrawNode)base.Child;

        public OutlineDrawNode(OutlineContainer source, OutlineContainerDrawNodeSharedData sharedData)
            : base(source, new CompositeDrawableDrawNode(source), sharedData)
        {
        }

        private long updateVersion;

        private float outlineWidth;
        private ColourInfo outlineColour;
        private BlendingParameters effectBlending;
        private uint startSize;

        private IShader jumpFloodShader;
        private IShader outlineShader;

        public override void ApplyState()
        {
            base.ApplyState();

            updateVersion = Source.updateVersion;

            outlineWidth = Math.Min(Source.OutlineWidth * Source.DrawInfo.Matrix.ExtractScale().X, 128f);
            outlineColour = Source.OutlineColour;
            effectBlending = Source.DrawEffectBlending;
            startSize = BitOperations.RoundUpToPowerOf2((uint)Math.Ceiling(outlineWidth));

            jumpFloodShader = Source.jumpFloodShader;
            outlineShader = Source.outlineShader;
        }

        protected override long GetDrawVersion() => updateVersion;

        private IUniformBuffer<JumpFloodParameters> jumpFloodParametersBuffer;
        private IUniformBuffer<OutlineParameters> outlineParametersBuffer;

        protected override void PopulateContents(IRenderer renderer)
        {
            if (outlineWidth <= 0)
                return;

            jumpFloodParametersBuffer ??= renderer.CreateUniformBuffer<JumpFloodParameters>();

            renderer.PushScissorState(false);

            renderer.SetBlend(BlendingParameters.None);

            bool initialPass = true;
            uint size = startSize;
            while (size > 0)
            {
                jumpFloodPass(renderer, size, initialPass);

                initialPass = false;
                size /= 2;
            }

            renderer.PopScissorState();
        }

        private void jumpFloodPass(IRenderer renderer, uint size, bool initialPass)
        {
            IFrameBuffer current = SharedData.CurrentEffectBuffer;
            IFrameBuffer target = SharedData.GetNextEffectBuffer();

            using (BindFrameBuffer(target))
            {
                jumpFloodParametersBuffer.Data = new JumpFloodParameters
                {
                    InitialPass = initialPass ? 1 : 0,
                    Offset = new Vector2(size),
                    TexSize = current.Size,
                };

                jumpFloodShader.BindUniformBlock("m_JumpFloodParameters", jumpFloodParametersBuffer);
                jumpFloodShader.Bind();
                renderer.DrawFrameBuffer(current, new RectangleF(0, 0, current.Texture.Width, current.Texture.Height),
                    Color4.White);
                jumpFloodShader.Unbind();
            }
        }

        protected override void DrawContents(IRenderer renderer)
        {
            if (outlineWidth > 0)
            {

                outlineParametersBuffer ??= renderer.CreateUniformBuffer<OutlineParameters>();

                renderer.SetBlend(effectBlending);

                outlineParametersBuffer.Data = new OutlineParameters
                {
                    OutlineWidth = outlineWidth,
                    TexSize = SharedData.CurrentEffectBuffer.Size,
                };

                outlineShader.BindUniformBlock("m_OutlineParameters", outlineParametersBuffer);
                outlineShader.Bind();
                renderer.DrawFrameBuffer(SharedData.CurrentEffectBuffer, DrawRectangle, outlineColour);
                outlineShader.Unbind();
            }

            base.DrawContents(renderer);
        }

        public List<DrawNode> Children
        {
            get => Child.Children;
            set => Child.Children = value;
        }

        public bool AddChildDrawNodes => RequiresRedraw;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct JumpFloodParameters
        {
            public UniformInt InitialPass;
            private UniformPadding4 pad1;
            public UniformVector2 Offset;
            public UniformVector2 TexSize;
            private UniformPadding8 pad2;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private record struct OutlineParameters
        {
            public UniformVector2 TexSize;
            public UniformFloat OutlineWidth;
            private UniformPadding4 pad1;
        }
    }

    private class OutlineContainerDrawNodeSharedData : BufferedDrawNodeSharedData
    {
        public OutlineContainerDrawNodeSharedData()
            : base(2, clipToRootNode: false)
        {
        }
    }
}
