using System.Threading.Tasks;
using UnityEngine;

namespace UnityGLTF
{
    public static class NormalMapEncodingConverter
    {
        public static Texture2D ConvertToDxt5nm(Texture2D source)
        {
            var dest = new Texture2D(source.width, source.height, TextureFormat.DXT5, false);
            var pixels = source.GetPixels();
            for (var i = 0; i < pixels.Length; i++)
            {
                var c = pixels[i];
                pixels[i] = new Color(1, c.g, 1, c.r);
            }

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