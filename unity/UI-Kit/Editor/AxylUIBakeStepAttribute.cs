// Copyright (c) Com2uS Platform Corp. All rights reserved.

using System;

namespace Hive.Axyl.UIKit.Editor
{
    /// <summary>
    /// Marks a screen's bake step. A screen ships its baked assets by adding one file —
    /// <c>Editor/AxylUIBaker.&lt;Screen&gt;.cs</c>, a partial of <see cref="AxylUIBaker"/> with a
    /// <c>static void</c> method taking the Kit's main <c>TMP_FontAsset</c> and carrying this
    /// attribute. <see cref="AxylUIBaker.BakeAll"/> discovers and runs every step after the
    /// shared sprites, fonts, and Common prefabs are baked; the shared build helpers are
    /// available to the partial. The bake never needs a screen list, so screen branches and
    /// copies add or remove screens without touching the baker itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public sealed class AxylUIBakeStepAttribute : Attribute
    {
    }
}
