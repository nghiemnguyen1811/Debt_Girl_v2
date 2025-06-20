using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteColorizer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        public async Task ColorizeSprites(List<FObject> fobjects)
        {
            if (monoBeh.UsingSVG())
                return;

            foreach (FObject fobject in fobjects)
            {
                if (monoBeh.IsCancellationRequested(TokenType.Import))
                    return;

                if (fobject.Data.FcuImageType != FcuImageType.Downloadable)
                    continue;

                if (monoBeh.UsingSpriteRenderer())
                {
                    if (fobject.Data.Graphic.SpriteSingleColor.IsDefault())
                        continue;
                }
                else
                {
                    if (fobject.Data.Graphic.SpriteSingleColor.IsDefault() &&
                        fobject.Data.Graphic.SpriteSingleLinearGradient.IsDefault())
                        continue;
                }

                if (File.Exists(fobject.Data.SpritePath.GetFullAssetPath()) == false)
                    continue;

                byte[] rawData = File.ReadAllBytes(fobject.Data.SpritePath.GetFullAssetPath());
                Texture2D tex = new Texture2D(fobject.Data.SpriteSize.x, fobject.Data.SpriteSize.y);
                tex.LoadImage(rawData);

                tex.Colorize(Color.white);

                byte[] bytes = new byte[0];

                switch (monoBeh.Settings.ImageSpritesSettings.ImageFormat)
                {
                    case ImageFormat.PNG:
                        bytes = tex.EncodeToPNG();
                        break;
                    case ImageFormat.JPG:
                        bytes = tex.EncodeToJPG();
                        break;
                }

                File.WriteAllBytes(fobject.Data.SpritePath, bytes);

                await Task.Yield();
            }
        }
    }
}