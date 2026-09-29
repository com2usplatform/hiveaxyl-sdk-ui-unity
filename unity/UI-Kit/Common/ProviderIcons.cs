// Copyright (c) Com2uS Platform Corp. All rights reserved.

using UnityEngine;

namespace Hive.Axyl.UIKit
{
    /// <summary>
    /// The provider icon slot, resolved from sprite files under
    /// Common/ProviderMarks/Resources/UIKit/ProviderMarks/&lt;providerId&gt;.png. Every screen
    /// that shows a provider (login, account) reads the same file, so an icon stays
    /// identical across screens. The Kit bundles no brand assets: each provider's icon is
    /// the app's input — download the official asset from the provider's brand guide and
    /// drop it onto that path, named after the provider id; no code changes. Import the PNG
    /// with Texture Type "Sprite (2D and UI)": a project in 3D mode imports a new PNG as a
    /// plain texture, and a non-sprite file loads as null here — the provider silently
    /// falls back to its letter chip. Guest is the one icon the Kit ships (its own default,
    /// not a brand).
    ///
    /// Official sources (check the latest guide when dropping an asset in):
    ///   Google            https://developers.google.com/identity/branding-guidelines
    ///   Apple             https://developer.apple.com/design/resources/
    ///   Google Play Games https://developer.android.com/games/pgs/branding
    ///   Steam             https://partner.steamgames.com/doc/marketing/branding
    ///   X                 https://about.x.com/en/who-we-are/brand-toolkit
    ///
    /// Use each official asset exactly as delivered — shape, proportions, colors and
    /// clear space are the provider's rules to keep; screens display it at 20×20.
    /// Record each delivered asset's source and version,
    /// light/dark usability, and whether the app supplied it — the login screen README's
    /// "Provider icons" section carries the checklist. A provider with no delivered asset
    /// keeps the letter-chip placeholder; never substitute an unofficial icon — ask the
    /// app developer for the asset instead.
    /// </summary>
    internal static class ProviderIcons
    {
        private const string k_PathPrefix = "UIKit/ProviderMarks/";

        /// <summary>The icon for <paramref name="providerId"/>, or null when no file was
        /// dropped in (the caller keeps the letter-chip placeholder).</summary>
        public static Sprite TryGet(string providerId)
        {
            return string.IsNullOrEmpty(providerId)
                ? null
                : Resources.Load<Sprite>(k_PathPrefix + providerId);
        }
    }
}
