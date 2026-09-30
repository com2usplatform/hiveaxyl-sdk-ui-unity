// Copyright (c) Com2uS Platform Corp. All rights reserved.

using TMPro;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>The password-change screen's bake step: imports the screen's form glyphs
    /// (the shared AxylUIInputGroup prefab itself is baked with the Common widgets). Lives
    /// with the baker as a partial so the screen adds itself to the bake without touching the
    /// shared file.</summary>
    public static partial class AxylUIBaker
    {
        private const string k_PasswordChangeIconsDir =
            k_Root + "/PasswordChange/Sprites/Resources/UIKit/PasswordChangeIcons";

        [AxylUIBakeStep]
        private static void BakePasswordChangeWidgets(TMP_FontAsset font)
        {
            ImportIcons(k_PasswordChangeIconsDir);
        }
    }
}
