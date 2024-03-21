using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF
{
    internal static class NormalMapEncodingConverter
    {
        public static async Task<Texture2D> ConvertToDxt5nmAndCheckTextureFormatAsync(Texture2D source)
        {
            Texture2D dest = source;
            Color[] pixels;

            void CreateDestinationTexture()
            {
                dest = new Texture2D(source.width, source.height, TextureFormat.RGBA32, source.mipmapCount > 0, true);
                dest.wrapMode = source.wrapMode;
                dest.wrapModeU = source.wrapModeU;
                dest.wrapModeV = source.wrapModeV;
                dest.wrapModeW = source.wrapModeW;
                dest.filterMode = source.filterMode;
                dest.anisoLevel = source.anisoLevel;
                dest.mipMapBias = source.mipMapBias;
                dest.name = source.name;
            }

            void DestroySourceTexture()
            {
#if UNITY_EDITOR
                Texture.DestroyImmediate(source);
#else
                Texture.Destroy(source);
#endif
            }

            if (!source.isReadable)
            {
                CreateDestinationTexture();

                var destRenderTexture = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.Default, RenderTextureReadWrite.Linear);
                var previousSRGBState = GL.sRGBWrite;
                GL.sRGBWrite = false;

                Graphics.Blit(source, destRenderTexture);

                dest.ReadPixels(new Rect(0, 0, destRenderTexture.width, destRenderTexture.height), 0, 0);

                GL.sRGBWrite = previousSRGBState;

                RenderTexture.ReleaseTemporary(destRenderTexture);                

                pixels = dest.GetPixels();

                DestroySourceTexture();
            }
            else
            {
                pixels = source.GetPixels();
                
                if (source.format != TextureFormat.RGBA32)
                {
                    CreateDestinationTexture();
                    DestroySourceTexture();
                }
            }
            
            await Task.Run(() =>
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    var c = pixels[i];
                    pixels[i] = new Color(1, c.g, 1, c.r);
                }
            });

            dest.SetPixels(pixels);
            return dest;
        } 
        
        public static void ConvertToDxt5nm(Texture2D source, Texture2D dest)
        {
            var pixels = source.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                pixels[i] = new Color(1, c.g, 1, c.r);
            }

            dest.SetPixels(pixels);
        }
        
        public static async Task ConvertToDxt5nmAsync(Texture2D texture)
        {
            var pixels = texture.GetPixels();
            await Task.Run(() =>
            {
                for (var i = 0; i < pixels.Length; i++)
                {
                    var c = pixels[i];
                    pixels[i] = new Color(1, c.g, 1, c.r);
                }
            });

            texture.SetPixels(pixels);
        }

    }
}