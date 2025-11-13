// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Resources.Localisation.Web;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Graphics.UserInterface
{
    public partial class SearchTextBox : FocusedTextBox
    {
        protected virtual bool AllowCommit => false;

        private static readonly Regex filter_regex = new Regex(
            @"\b(?<key>\w+)(?<op>(!?(:|=)|(>|<)(:|=)?))(?<value>("".*?""[!]?)|(\S*))",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private Container filterHighlightContainer = null!;

        public SearchTextBox()
        {
            Height = 35;
            PlaceholderText = HomeStrings.SearchPlaceholder;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            TextContainer.Add(filterHighlightContainer = new Container
            {
                Depth = float.MaxValue,
                RelativeSizeAxes = Axes.Both,
            });
        }

        protected override void OnUserTextAdded(string added)
        {
            base.OnUserTextAdded(added);
            updateFilterHighlights();
        }

        private void updateFilterHighlights()
        {
            filterHighlightContainer.Clear();

            if (string.IsNullOrEmpty(Text))
                return;

            var matches = filter_regex.Matches(Text);
            var textChars = TextFlow.Children.ToList();

            foreach (Match match in matches)
            {
                if (!match.Success || match.Index >= textChars.Count)
                    continue;

                int startIndex = match.Index;
                int endIndex = match.Index + match.Length;

                if (endIndex > textChars.Count)
                    endIndex = textChars.Count;

                float startX = startIndex > 0 ? textChars[startIndex - 1].DrawPosition.X + textChars[startIndex - 1].DrawWidth : 0;
                float endX = endIndex > 0 && endIndex <= textChars.Count ? textChars[endIndex - 1].DrawPosition.X + textChars[endIndex - 1].DrawWidth : startX;

                float width = endX - startX;

                if (width <= 0)
                    continue;

                filterHighlightContainer.Add(new Container
                {
                    Position = new osuTK.Vector2(startX, 0),
                    Size = new osuTK.Vector2(endX, TextFlow.DrawHeight),
                    Masking = true,
                    CornerRadius = 3,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.25f
                    }
                });
            }
        }

        public override bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            switch (e.Action)
            {
                case PlatformAction.MoveBackwardLine:
                case PlatformAction.MoveForwardLine:
                // Shift+delete is handled via PlatformAction on macOS. this is not so useful in the context of a SearchTextBox
                // as we do not allow arrow key navigation in the first place (ie. the caret should always be at the end of text)
                // Avoid handling it here to allow other components to potentially consume the shortcut.
                case PlatformAction.DeleteForwardChar:
                    return false;
            }

            return base.OnPressed(e);
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (!e.ControlPressed && !e.ShiftPressed)
            {
                switch (e.Key)
                {
                    case Key.Left:
                    case Key.Right:
                    case Key.Up:
                    case Key.Down:
                        return false;
                }
            }

            if (!AllowCommit)
            {
                switch (e.Key)
                {
                    case Key.KeypadEnter:
                    case Key.Enter:
                        // even if committing per se is not allowed for this textbox,
                        // the commit flow is also responsible for terminating any active IME.
                        // ensure that the Enter press terminates IME correctly
                        // and is also handled if it needs to be, so that it doesn't leak to some other non-focused drawable and cause breakage.
                        bool wasImeComposing = ImeCompositionActive;
                        FinalizeImeComposition(true);
                        return wasImeComposing;
                }
            }

            if (e.ShiftPressed)
            {
                switch (e.Key)
                {
                    case Key.Delete:
                        return false;
                }
            }

            return base.OnKeyDown(e);
        }
    }
}
