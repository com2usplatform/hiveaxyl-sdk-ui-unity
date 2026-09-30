// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>The username-login screen's bake step: imports the screen's form glyphs
    /// (the shared AxylUIInputGroup prefab itself is baked with the Common widgets). Lives
    /// with the baker as a partial so the screen adds itself to the bake without touching the
    /// shared file.</summary>
    public static partial class AxylUIBaker
    {
        private const string k_UsernameIconsDir =
            k_Root + "/UsernameLogin/Sprites/Resources/UIKit/UsernameIcons";

        [AxylUIBakeStep]
        private static void BakeUsernameLoginWidgets(TMP_FontAsset font)
        {
            ImportIcons(k_UsernameIconsDir);
        }
    }
}
