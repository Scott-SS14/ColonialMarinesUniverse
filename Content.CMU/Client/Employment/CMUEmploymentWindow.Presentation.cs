using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Content.Client.Resources;
using Content.Shared.Access;
using Content.Shared.CMU14.Employment;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Employment;

public sealed partial class CMUEmploymentWindow
{
    private CredentialHardware _credential = default!;
    private StampHardware _stamp = default!;
    private Font _displayFont = default!;
    private float _stampRemaining;
    private bool _draftEdited;

    private void InitializePresentation()
    {
        _displayFont = _hardwareResources.GetFont("/Fonts/RobotoMono/RobotoMono-Bold.ttf", 14);
        _credential = new CredentialHardware();
        CredentialArt.AddChild(_credential);
        _stamp = new StampHardware();
        ResultArt.AddChild(_stamp);
        PreviewButton.Text = Loc.GetString("cmu-employment-preview");
        PreviewButton.OnPressed += _ =>
        {
            _colonyPage = 0;
            _review = !_review;
            _history = false;
            _previewDirty = true;
            Confirm.Pressed = false;
            UpdateActions();
        };
        BindButton(PreviewButton);
        DiffHeading.Text = Loc.GetString("cmu-employment-diff-heading");
        DiffHint.Text = Wrap(Loc.GetString("cmu-employment-diff-hint"), 40);
    }

    private void UpdatePresentation()
    {
        if (_updatingState || _state is not { } state)
            return;

        _credential.Powered = state.Powered && _review;
        var saved = state.Receipt?.Pending != true && !_draftEdited && state.Target != null && state.Receipt?.Card == state.Target;
        ReadStage.Text = Loc.GetString(state.Target == null ? "cmu-employment-stage-insert" : "cmu-employment-stage-read");
        WriteStage.Text = Loc.GetString(_pending ? "cmu-employment-stage-waiting"
            : saved ? "cmu-employment-stage-saved" : "cmu-employment-stage-draft");
        PrintStage.Text = Loc.GetString(state.Printing ? "cmu-employment-stage-printing"
            : state.Receipt?.Printed == true && saved ? "cmu-employment-stage-printed" : "cmu-employment-stage-output");
        ReadStage.FontColorOverride = state.Target != null ? _accent : TextSecondary;
        WriteStage.FontColorOverride = _pending ? BusyLamp : saved ? _accent : TextSecondary;
        PrintStage.FontColorOverride = state.Printing ? BusyLamp : state.Receipt?.Printed == true && saved ? _accent : TextSecondary;
        WorkflowPanel.PanelOverride = Box(_background, PanelEdge, 1);
        PreviewButton.Text = Loc.GetString(_review ? "cmu-employment-preview-back" : "cmu-employment-preview");
        PreviewButton.Disabled = state.Target == null || _pending;
        StyleButton(PreviewButton);
        OperatorIdentity.Text = !state.OperatorCardInserted ? Loc.GetString("cmu-employment-reader-empty")
            : string.IsNullOrWhiteSpace(state.OperatorCardName) ? Loc.GetString("cmu-employment-unassigned") : state.OperatorCardName;
        EmployeeIdentity.Text = state.Target == null ? Loc.GetString("cmu-employment-reader-empty")
            : string.IsNullOrWhiteSpace(state.TargetName) ? Loc.GetString("cmu-employment-unassigned") : state.TargetName;
        OperatorIdentity.ToolTip = OperatorIdentity.Text;
        EmployeeIdentity.ToolTip = EmployeeIdentity.Text;
        OperatorIdentity.FontColorOverride = TextSecondary;
        EmployeeIdentity.FontColorOverride = TextSecondary;
        Brand.FontOverride = _displayFont;
        if (ResultPanel.Visible)
        {
            ResultTitle.FontOverride = _displayFont;
            ResultTitle.FontColorOverride = _stamp.Accent;
        }

        if (!_review || !_previewDirty)
            return;

        _previewDirty = false;
        var terminate = _action == CMUEmploymentAction.Terminate;
        var cardColor = terminate ? WarningText : _accent;
        _credential.Accent = cardColor;
        _credential.Powered = state.Powered;
        _credential.Theme = state.Theme;
        _credential.SetIdentity(state.Target?.GetHashCode() ?? 0);
        CredentialPanel.PanelOverride = Box(Blend(_surface, cardColor, 0.06f), PanelEdge, 1);
        DossierFrame.PanelOverride = Box(_surface, PanelEdge, 1);
        CredentialLogo.Texture = FactionLogo.Texture;
        CredentialHeading.Text = Loc.GetString(terminate ? "cmu-employment-card-revoked" : "cmu-employment-card-draft");
        CredentialName.Text = string.IsNullOrWhiteSpace(NameField.Text) ? Loc.GetString("cmu-employment-unassigned") : NameField.Text;
        CredentialTitle.Text = terminate ? Loc.GetString("cmu-employment-unassigned") : TitleField.Text;
        CredentialName.ToolTip = CredentialName.Text;
        CredentialTitle.ToolTip = CredentialTitle.Text;
        CredentialName.FontOverride = _displayFont;
        CredentialHeading.FontColorOverride = cardColor;
        CredentialIff.Text = Loc.GetString(!terminate && Iff.Pressed ? "cmu-employment-card-iff" : "cmu-employment-card-no-iff");
        CredentialIff.FontColorOverride = !terminate && Iff.Pressed ? cardColor : TextSecondary;
        CredentialIcon.Texture = terminate ? null : IconPreview.Texture;
        DossierHint.Text = Wrap(Loc.GetString(state.Theme switch
        {
            CMUEmploymentTheme.Govfor => "cmu-employment-design-govfor",
            CMUEmploymentTheme.Colony => "cmu-employment-design-colony",
            _ => "cmu-employment-design-weyland",
        }), 28);
        DossierHint.FontColorOverride = TextSecondary;
        DiffHeading.FontOverride = _displayFont;
        DiffHint.FontColorOverride = TextSecondary;

        var previous = new HashSet<ProtoId<AccessLevelPrototype>>(state.CurrentAccess);
        var selected = new HashSet<ProtoId<AccessLevelPrototype>>();
        if (!terminate)
        {
            foreach (var (tag, check) in _access)
            {
                if (check.Pressed)
                    selected.Add(tag);
            }
        }
        var added = selected.Except(previous).ToList();
        var removed = previous.Except(selected).ToList();
        var kept = selected.Intersect(previous).ToList();
        DiffSummary.Text = Loc.GetString("cmu-employment-diff-summary", ("added", added.Count),
            ("removed", removed.Count), ("kept", kept.Count));
        DiffList.RemoveAllChildren();
        AddAccessGroup("cmu-employment-diff-added", "+", added, _accent);
        AddAccessGroup("cmu-employment-diff-removed", "−", removed, WarningText);
        AddAccessGroup("cmu-employment-diff-kept", "=", kept, TextSecondary);
        if (added.Count + removed.Count + kept.Count == 0)
            DiffList.AddChild(new Label { Text = Loc.GetString("cmu-employment-form-no-access"), FontOverride = _font });
    }

    private void AddAccessGroup(string heading, string marker, List<ProtoId<AccessLevelPrototype>> access, Color color)
    {
        if (access.Count == 0)
            return;
        DiffList.AddChild(new Label
        {
            Text = Loc.GetString(heading), FontOverride = _font, FontColorOverride = color,
            Margin = new Thickness(0, 8, 0, 2),
        });
        var names = new List<string>(access.Count);
        foreach (var tag in access)
        {
            var name = _access.TryGetValue(tag, out var check) ? check.Text : null;
            names.Add(name ?? (_prototypes.TryIndex(tag, out var prototype) ? prototype.GetAccessLevelName() : tag.Id));
        }
        names.Sort(StringComparer.CurrentCulture);
        foreach (var name in names)
            DiffList.AddChild(new Label
            {
                Text = marker + " " + name, ToolTip = name, ClipText = true,
                FontOverride = _font, FontColorOverride = color,
            });
    }

    private void UpdateReceipt(CMUEmploymentState? previous)
    {
        if (_state is not { } state)
            return;
        if (!state.Powered)
        {
            _stampRemaining = 0f;
            ResultPanel.Visible = false;
            return;
        }
        // Opening a terminal must not replay the last operator's completion animation.
        if (previous == null || state.Receipt is not { } receipt)
            return;
        if (receipt.Pending && previous.Receipt?.Revision != receipt.Revision)
            ShowStamp(Loc.GetString("cmu-employment-request-submitted"), receipt.Name, BusyLamp);
        else if (!receipt.Pending && receipt.Printed && (previous.Receipt?.Revision != receipt.Revision || previous.Receipt?.Printed != true))
            ShowStamp(Loc.GetString("cmu-employment-stamp-printed"), receipt.Name, _accent);
        else if (previous.Receipt?.Revision != receipt.Revision)
        {
            ShowStamp(Loc.GetString(receipt.Action switch
            {
                CMUEmploymentAction.Hire => "cmu-employment-stamp-issued",
                CMUEmploymentAction.Edit => "cmu-employment-stamp-updated",
                _ => "cmu-employment-stamp-revoked",
            }), receipt.Name, receipt.Action == CMUEmploymentAction.Terminate ? WarningText : _accent);
        }
        UpdatePresentation();
    }

    private void ShowStamp(string title, string name, Color color)
    {
        _stampRemaining = 2.8f;
        _stamp.Accent = color;
        _stamp.Progress = 0f;
        ResultTitle.Text = title;
        ResultTitle.FontOverride = _displayFont;
        ResultTitle.FontColorOverride = color;
        ResultDetail.Text = name;
        ResultDetail.FontOverride = _font;
        ResultDetail.FontColorOverride = TextPrimary;
        ResultDetail.ClipText = true;
        ResultPanel.MaxWidth = 460;
        ResultPanel.PanelOverride = Box(_background.WithAlpha(0.96f), color.WithAlpha(0.55f), 1);
        ResultPanel.Visible = true;
    }

    private void TickPresentation(float delta)
    {
        if (_stampRemaining <= 0f)
            return;
        _stampRemaining = Math.Max(0f, _stampRemaining - delta);
        _stamp.Progress = Math.Min(1f, (2.8f - _stampRemaining) / 0.24f);
        if (_stampRemaining <= 0f)
            ResultPanel.Visible = false;
    }

    private sealed class SpinningLogo : Control
    {
        public Texture? Texture { get; set; }
        public float Progress { get; set; }
        public Color Accent { get; set; }

        public SpinningLogo()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            if (Texture is not { } texture || texture.Width <= 0 || texture.Height <= 0)
                return;
            var scale = Math.Min(PixelSize.X / (float) texture.Width, PixelSize.Y / (float) texture.Height) * 0.7f;
            var half = new Vector2(texture.Width, texture.Height) * scale * 0.5f;
            var progress = Math.Clamp(Progress / 0.82f, 0f, 1f);
            var remaining = 1f - progress;
            var angle = -MathF.Tau * remaining * remaining * remaining;
            var center = new Vector2(PixelSize.X / 2f, PixelSize.Y / 2f);
            var radius = Math.Min(PixelSize.X, PixelSize.Y) * 0.47f;
            for (var i = 0; i < 12; i++)
            {
                var tickAngle = i * MathF.Tau / 12f - MathF.PI / 2f;
                var at = center + new Vector2(MathF.Cos(tickAngle), MathF.Sin(tickAngle)) * radius;
                handle.DrawRect(UIBox2.FromDimensions(at.X - UIScale, at.Y - UIScale, UIScale * 2f, UIScale * 2f),
                    i <= Progress * 12f ? Accent.WithAlpha(0.6f) : PanelEdge);
            }
            var previous = handle.GetTransform();
            try
            {
                handle.SetTransform(Matrix3x2.CreateRotation(angle) *
                    Matrix3x2.CreateTranslation(PixelSize.X / 2f, PixelSize.Y / 2f) * previous);
                handle.DrawTextureRect(texture, new UIBox2(-half, half));
            }
            finally
            {
                handle.SetTransform(previous);
            }
        }
    }

    private sealed class CredentialHardware : Control
    {
        private readonly int[] _barcode = new int[36];
        private int _identity;
        private float _clock;
        public Color Accent { get; set; }
        public bool Powered { get; set; }
        public CMUEmploymentTheme Theme { get; set; }

        public CredentialHardware()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        public void SetIdentity(int identity)
        {
            if (_identity == identity)
                return;
            _identity = identity;
            var seed = identity;
            for (var i = 0; i < _barcode.Length; i++)
            {
                seed = unchecked(seed * 1664525 + 1013904223);
                _barcode[i] = 1 + (seed & 3);
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (Powered)
                _clock = (_clock + args.DeltaSeconds) % 6f;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var w = PixelSize.X;
            var h = PixelSize.Y;
            var px = Math.Max(1f, UIScale * 2f);
            handle.DrawRect(UIBox2.FromDimensions(0, 0, w, px * 2), Accent.WithAlpha(0.6f));
            if (Theme == CMUEmploymentTheme.Weyland)
                handle.DrawRect(UIBox2.FromDimensions(0, px * 3, w, px), BusyLamp.WithAlpha(0.6f));
            else if (Theme == CMUEmploymentTheme.Colony)
            {
                for (var i = 0; i < 8; i++)
                    handle.DrawRect(UIBox2.FromDimensions(px * 2, px * (15 + i * 4), px * 2, px), TextSecondary.WithAlpha(0.2f));
            }
            var color = Theme == CMUEmploymentTheme.Weyland ? BusyLamp : TextSecondary;
            for (var i = 0; i < 5; i++)
                handle.DrawRect(UIBox2.FromDimensions(w - px * (5 + i * 3), px * 6, px, px * 8), color.WithAlpha(0.2f));
            var x = px * 8;
            foreach (var width in _barcode)
            {
                var barWidth = width * px * 0.35f;
                handle.DrawRect(UIBox2.FromDimensions(x, h - px * 13, barWidth, px * 7), TextPrimary.WithAlpha(0.65f));
                x += barWidth + px * 0.7f;
            }
            if (Powered)
            {
                var scan = (h - px * 4) * _clock / 6f;
                handle.DrawRect(UIBox2.FromDimensions(px, scan + px * 2, w - px * 2, px), Accent.WithAlpha(0.08f));
            }
        }
    }

    private sealed class StampHardware : Control
    {
        public Color Accent { get; set; }
        public float Progress { get; set; }

        public StampHardware()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var px = Math.Max(1f, UIScale * 2f);
            var inset = px * (2f + (1f - Progress) * 5f);
            var w = PixelSize.X;
            var h = PixelSize.Y;
            if (w <= inset * 2 || h <= inset * 2)
                return;
            handle.DrawRect(new UIBox2(inset, inset, w - inset, h - inset), Accent.WithAlpha(0.15f + Progress * 0.35f), filled: false);
            handle.DrawRect(UIBox2.FromDimensions(inset, inset, px * 4, px), Accent);
            handle.DrawRect(UIBox2.FromDimensions(w - inset - px * 4, h - inset - px, px * 4, px), Accent);
        }
    }
}
