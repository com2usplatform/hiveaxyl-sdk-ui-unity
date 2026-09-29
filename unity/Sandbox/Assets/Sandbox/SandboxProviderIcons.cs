// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;

namespace Hive.Axyl.UIKit.Sandbox
{
    /// <summary>
    /// The slot where the Sandbox hands the Kit provider icons, playing the app
    /// developer's part. The Kit bundles no brand assets, and neither does this
    /// repository — each provider's mark stays with its owner. To see real icons, obtain
    /// each official asset under that provider's brand guideline and drop it at
    /// Assets/Sandbox/Resources/SandboxProviderIcons/&lt;providerId&gt;.png, imported as
    /// Sprite (2D and UI); it is passed in on the screen options (the <c>icon</c>
    /// argument). With no file, every provider renders the way a real app looks before
    /// its assets arrive — Guest through the one mark the Kit itself ships, everything
    /// else as the letter chip.
    /// </summary>
    internal static class SandboxProviderIcons
    {
        private const string k_PathPrefix = "SandboxProviderIcons/";

        /// <summary>The Sandbox's icon for <paramref name="providerId"/>, or null when it
        /// supplies none (the Kit falls back to its drop-in slot, then the chip).</summary>
        public static Sprite TryGet(string providerId)
        {
            return string.IsNullOrEmpty(providerId)
                ? null
                : Resources.Load<Sprite>(k_PathPrefix + providerId);
        }
    }
}
