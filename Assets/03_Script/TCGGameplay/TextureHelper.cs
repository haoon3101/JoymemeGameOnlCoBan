using UnityEngine;

public static class TextureHelper
{
    public static Texture2D ConvertToReadableTexture(Sprite sprite)
    {
        RenderTexture rt = RenderTexture.GetTemporary(
            sprite.texture.width,
            sprite.texture.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(sprite.texture, rt);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D newTex = new Texture2D(sprite.texture.width, sprite.texture.height, TextureFormat.RGBA32, false);
        newTex.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        newTex.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return newTex;
    }
}
