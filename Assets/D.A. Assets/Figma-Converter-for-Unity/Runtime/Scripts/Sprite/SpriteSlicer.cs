using DA_Assets.DAI;
using DA_Assets.Extensions;
using DA_Assets.FCU.Extensions;
using DA_Assets.FCU.Model;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace DA_Assets.FCU
{
    [Serializable]
    public class SpriteSlicer : MonoBehaviourLinkerRuntime<FigmaConverterUnity>
    {
        public async Task SliceSprites(List<FObject> fobjects)
        {
            foreach (FObject fobject in fobjects)
            {
                if (monoBeh.IsCancellationRequested(TokenType.Import))
                    return;

                if (!fobject.IsSprite())
                    continue;

                if (fobject.Children.IsEmpty())
                    continue;

                if (fobject.Children.Count != 9)
                    continue;

                Sprite sprite = monoBeh.SpriteProcessor.GetSprite(fobject);

                if (sprite == null)
                    continue;

                FObject child0 = fobject.Children[0];
                FObject child1 = fobject.Children[1];
                FObject child2 = fobject.Children[2];
                FObject child3 = fobject.Children[3];
                FObject child4 = fobject.Children[4];
                FObject child5 = fobject.Children[5];
                FObject child6 = fobject.Children[6];
                FObject child7 = fobject.Children[7];
                FObject child8 = fobject.Children[8];

                float imageScale = monoBeh.Settings.ImageSpritesSettings.ImageScale;

                int left = (int)(child0.Size.x * imageScale);
                int top = (int)(child0.Size.y * imageScale);
                int right = (int)(child2.Size.x * imageScale);
                int bottom = (int)(child6.Size.y * imageScale);

                monoBeh.DelegateHolder.SetSpriteRects(sprite, left, top, right, bottom);
                await Task.Yield();
            }
        }
    }
}