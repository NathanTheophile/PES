using UnityEngine;

namespace TacticalPort.View
{
    public static class CombatViewSpriteFactory
    {
        #region _____________________________| VALUES

        private static Sprite _WhiteSprite;

        #endregion

        #region _____________________________| ACCESSORS

        public static Sprite WhiteSprite
        {
            get
            {
                if (_WhiteSprite == null)
                {
                    Texture2D lTexture = Texture2D.whiteTexture;
                    _WhiteSprite = Sprite.Create(
                        lTexture,
                        new Rect(0f, 0f, lTexture.width, lTexture.height),
                        new Vector2(0.5f, 0.5f),
                        lTexture.width);

                    _WhiteSprite.name = "CombatViewWhiteSprite";
                    _WhiteSprite.hideFlags = HideFlags.HideAndDontSave;
                }

                return _WhiteSprite;
            }
        }

        #endregion
    }
}
