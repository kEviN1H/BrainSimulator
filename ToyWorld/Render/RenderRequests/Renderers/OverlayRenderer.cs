﻿using System;
using System.Collections.Generic;
using System.Linq;
using GoodAI.ToyWorld.Control;
using OpenTK.Graphics.OpenGL;
using Render.RenderObjects.Effects;
using RenderingBase.Renderer;
using RenderingBase.RenderObjects.Geometries;
using RenderingBase.RenderObjects.Textures;
using TmxMapSerializer.Elements;
using VRageMath;
using World.GameActors.GameObjects;
using World.ToyWorldCore;

namespace Render.RenderRequests
{
    internal class OverlayRenderer
        : RRRendererBase<OverlaySettings, RenderRequest>, IDisposable
    {
        #region Fields

        protected NoEffectOffset m_overlayEffect;
        protected TilesetTexture m_overlayTexture;

        protected Quad QuadOffset;

        #endregion

        #region Genesis

        public OverlayRenderer(RenderRequest owner)
            : base(owner)
        { }

        public virtual void Dispose()
        {
            if (m_overlayEffect != null)
                m_overlayEffect.Dispose();
            if (m_overlayTexture != null)
                m_overlayTexture.Dispose();

            QuadOffset.Dispose();
        }

        #endregion

        #region Init

        public override void Init(RendererBase<ToyWorld> renderer, ToyWorld world, OverlaySettings settings)
        {
            Settings = settings;

            // Set up overlay textures
            IEnumerable<Tileset> tilesets = world.TilesetTable.GetOverlayImages();
            TilesetImage[] tilesetImages = tilesets.Select(t =>
                    new TilesetImage(
                        t.Image.Source,
                        new Vector2I(t.Tilewidth, t.Tileheight),
                        new Vector2I(t.Spacing),
                        world.TilesetTable.TileBorder))
                .ToArray();

            m_overlayTexture = renderer.TextureManager.Get<TilesetTexture>(tilesetImages);

            // Set up overlay shader
            m_overlayEffect = renderer.EffectManager.Get<NoEffectOffset>();
            renderer.EffectManager.Use(m_overlayEffect); // Need to use the effect to set uniforms

            // Set up static uniforms
            Vector2I tileSize = tilesetImages[0].TileSize;
            Vector2I tileMargins = tilesetImages[0].TileMargin;
            Vector2I tileBorder = tilesetImages[0].TileBorder;

            Vector2I fullTileSize = tileSize + tileMargins + tileBorder * 2; // twice the border, on each side once
            Vector2 tileCount = (Vector2)m_overlayTexture.Size / (Vector2)fullTileSize;
            m_overlayEffect.TexSizeCountUniform(new Vector3I(m_overlayTexture.Size.X, m_overlayTexture.Size.Y, (int)tileCount.X));
            m_overlayEffect.TileSizeMarginUniform(new Vector4I(tileSize, tileMargins));
            m_overlayEffect.TileBorderUniform(tileBorder);

            m_overlayEffect.AmbientUniform(new Vector4(1, 1, 1, 1));

            QuadOffset = renderer.GeometryManager.Get<Quad>();
        }

        #endregion

        #region Draw

        public override void Draw(RendererBase<ToyWorld> renderer, ToyWorld world)
        {
            if ((Settings.EnabledOverlays ^ RenderRequestOverlay.InventoryTool) == RenderRequestOverlay.None)
                return;

            Owner.FrontFbo.Bind();

            // Bind stuff to GL
            renderer.TextureManager.Bind(Owner.TilesetTexture);
            renderer.EffectManager.Use(Owner.Effect);
            Owner.Effect.TextureUniform(0);


            // some stuffs
        }

        protected void DrawAvatarTool(RendererBase<ToyWorld> renderer, IAvatar avatar, Vector2 size, Vector2 position, ToolBackgroundType type = ToolBackgroundType.BrownBorder)
        {
            GL.Enable(EnableCap.Blend);

            if (Owner.FlipYAxis)
            {
                size.Y = -size.Y;
                position.Y = -position.Y;
            }

            Matrix transform = Matrix.CreateScale(size);
            transform *= Matrix.CreateTranslation(position.X, position.Y, 0.01f);


            // Draw the inventory background
            renderer.TextureManager.Bind(m_overlayTexture, Owner.GetTextureUnit(RenderRequest.TextureBindPosition.Ui));
            renderer.EffectManager.Use(m_overlayEffect);
            m_overlayEffect.TextureUniform((int)RenderRequest.TextureBindPosition.Ui);
            m_overlayEffect.TileTypesTextureUniform((int)RenderRequest.TextureBindPosition.TileTypes);
            m_overlayEffect.TileTypesIdxOffsetUniform(0);
            m_overlayEffect.ModelViewProjectionUniform(ref transform);

            Owner.LocalTileTypesBuffer[0] = (ushort)type;
            if (avatar.Tool != null)
                Owner.LocalTileTypesBuffer[1] = (ushort)avatar.Tool.TilesetId;

            GL.BindBuffer(BufferTarget.PixelUnpackBuffer, 0);
            Owner.TileTypesTexure.Update1D(2, dataType: PixelType.UnsignedShort, data: Owner.LocalTileTypesBuffer);
            QuadOffset.Draw();


            // Draw the inventory Tool
            if (avatar.Tool != null)
            {
                renderer.TextureManager.Bind(Owner.TilesetTexture);
                renderer.EffectManager.Use(Owner.Effect);
                Owner.Effect.DiffuseUniform(new Vector4(1, 1, 1, 1));
                Owner.Effect.TileTypesIdxOffsetUniform(1);

                Matrix toolTransform = Matrix.CreateScale(0.7f) * transform;
                Owner.Effect.ModelViewProjectionUniform(ref toolTransform);

                QuadOffset.Draw();
            }
        }

        #endregion
    }
}
