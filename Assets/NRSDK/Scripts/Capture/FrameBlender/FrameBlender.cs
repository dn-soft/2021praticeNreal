﻿/****************************************************************************
* Copyright 2019 Nreal Techonology Limited. All rights reserved.
*                                                                                                                                                          
* This file is part of NRSDK.                                                                                                          
*                                                                                                                                                           
* https://www.nreal.ai/        
* 
*****************************************************************************/

namespace NRKernal.Record
{
    using UnityEngine;

    /// <summary> A frame blender. </summary>
    public class FrameBlender
    {
        /// <summary> Target camera. </summary>
        protected Camera m_TargetCamera;
        /// <summary> The encoder. </summary>
        protected IEncoder m_Encoder;
        /// <summary> The blend material. </summary>
        private Material m_BlendMaterial;
        /// <summary> The blend mode. </summary>
        protected BlendMode m_BlendMode;
        /// <summary> The RGB origin. </summary>
        protected Texture2D m_RGBOrigin;
        /// <summary> The RGB source. </summary>
        protected RenderTexture m_RGBSource;
        /// <summary> The temporary combine tex. </summary>
        protected Texture2D m_TempCombineTex;
        /// <summary> Number of frames. </summary>
        private int m_FrameCount;

        /// <summary> Gets the blend mode. </summary>
        /// <value> The blend mode. </value>
        public BlendMode BlendMode
        {
            get
            {
                return m_BlendMode;
            }
        }

        /// <summary> The blend texture. </summary>
        private RenderTexture m_BlendTexture;
        /// <summary> Gets or sets the blend texture. </summary>
        /// <value> The blend texture. </value>
        public RenderTexture BlendTexture
        {
            get
            {
                return m_BlendTexture;
            }
            protected set
            {
                m_BlendTexture = value;
            }
        }

        /// <summary> Gets the RGB texture. </summary>
        /// <value> The RGB texture. </value>
        public RenderTexture RGBTexture
        {
            get
            {
                return m_RGBSource;
            }
        }

        /// <summary> Gets the virtual texture. </summary>
        /// <value> The virtual texture. </value>
        public RenderTexture VirtualTexture
        {
            get
            {
                return m_TargetCamera.targetTexture;
            }
        }

        /// <summary> Gets or sets the width. </summary>
        /// <value> The width. </value>
        public int Width
        {
            get;
            private set;
        }

        /// <summary> Gets or sets the height. </summary>
        /// <value> The height. </value>
        public int Height
        {
            get;
            private set;
        }

        /// <summary> Gets or sets the number of frames. </summary>
        /// <value> The number of frames. </value>
        public int FrameCount
        {
            get
            {
                return m_FrameCount;
            }
            private set
            {
                m_FrameCount = value;
            }
        }

        /// <summary> Initializes this object. </summary>
        /// <param name="camera">  The camera.</param>
        /// <param name="encoder"> The encoder.</param>
        /// <param name="param">   The parameter.</param>
        public virtual void Init(Camera camera, IEncoder encoder, CameraParameters param)
        {
            Width = param.cameraResolutionWidth;
            Height = param.cameraResolutionHeight;
            m_BlendMode = param.blendMode;
            m_TargetCamera = camera;
            m_Encoder = encoder;
            Shader blendshader;
            switch (m_BlendMode)
            {
                case BlendMode.RGBOnly:
                    blendshader = Resources.Load<Shader>("Record/Shaders/NormalBlend");
                    m_BlendMaterial = new Material(blendshader);
                    BlendTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                    break;
                case BlendMode.VirtualOnly:
                    blendshader = Resources.Load<Shader>("Record/Shaders/NormalBlend");
                    m_BlendMaterial = new Material(blendshader);
                    BlendTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                    break;
                case BlendMode.Blend:
                    blendshader = Resources.Load<Shader>("Record/Shaders/AlphaBlend");
                    m_BlendMaterial = new Material(blendshader);
                    BlendTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                    break;
                case BlendMode.WidescreenBlend:
                    blendshader = Resources.Load<Shader>("Record/Shaders/NormalBlend");
                    m_BlendMaterial = new Material(blendshader);
                    BlendTexture = new RenderTexture(2 * Width, Height, 24, RenderTextureFormat.ARGB32);
                    m_RGBSource = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
                    m_TempCombineTex = new Texture2D(2 * Width, Height, TextureFormat.ARGB32, false);
                    break;
                default:
                    break;
            }
            m_TargetCamera.enabled = false;
            m_TargetCamera.targetTexture = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
        }

        /// <summary> Executes the 'frame' action. </summary>
        /// <param name="frame"> The frame.</param>
        public void OnFrame(CameraTextureFrame frame)
        {
            Texture2D frametex = frame.texture as Texture2D;
            m_RGBOrigin = frametex;

            m_TargetCamera.Render();

            switch (m_BlendMode)
            {
                case BlendMode.RGBOnly:
                    m_BlendMaterial.SetTexture("_MainTex", frame.texture);
                    Graphics.Blit(frame.texture, BlendTexture, m_BlendMaterial);
                    break;
                case BlendMode.VirtualOnly:
                    m_BlendMaterial.SetTexture("_MainTex", m_TargetCamera.targetTexture);
                    Graphics.Blit(m_TargetCamera.targetTexture, BlendTexture, m_BlendMaterial);
                    break;
                case BlendMode.Blend:
                    m_BlendMaterial.SetTexture("_MainTex", m_TargetCamera.targetTexture);
                    m_BlendMaterial.SetTexture("_BcakGroundTex", frame.texture);
                    Graphics.Blit(m_TargetCamera.targetTexture, BlendTexture, m_BlendMaterial);
                    break;
                case BlendMode.WidescreenBlend:
                    CombineTexture(frametex, m_TargetCamera.targetTexture, m_TempCombineTex, BlendTexture);
                    break;
                default:
                    break;
            }

            // Commit frame                
            m_Encoder.Commit(BlendTexture, frame.timeStamp);
            FrameCount++;
        }

        /// <summary> Combine texture. </summary>
        /// <param name="bgsource">   The bgsource.</param>
        /// <param name="foresource"> The foresource.</param>
        /// <param name="tempdest">   The tempdest.</param>
        /// <param name="dest">       Destination for the.</param>
        private void CombineTexture(Texture2D bgsource, RenderTexture foresource, Texture2D tempdest, RenderTexture dest)
        {
            m_BlendMaterial.SetTexture("_MainTex", m_RGBSource);
            Graphics.Blit(bgsource, m_RGBSource, m_BlendMaterial);

            RenderTexture prev = RenderTexture.active;
            RenderTexture.active = m_RGBSource;
            tempdest.ReadPixels(new Rect(0, 0, m_RGBSource.width, m_RGBSource.height), 0, 0);

            RenderTexture.active = foresource;
            tempdest.ReadPixels(new Rect(0, 0, foresource.width, foresource.height), foresource.width, 0);
            tempdest.Apply();
            RenderTexture.active = prev;

            Graphics.Blit(tempdest, dest);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        /// resources. </summary>
        public virtual void Dispose()
        {
            RenderTexture.active = null;
            BlendTexture?.Release();
            m_RGBSource?.Release();

            GameObject.Destroy(m_TempCombineTex);
            BlendTexture = null;
            m_RGBSource = null;
            m_TempCombineTex = null;
        }
    }
}
