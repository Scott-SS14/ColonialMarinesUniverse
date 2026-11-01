using System;
using Content.Client.CMU14.Interface;
using Content.Client.Resources;
using Content.Shared.CMU14.Employment;
using Robust.Client.Graphics;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Graphics.RSI;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Content.Client.CMU14.Employment;

public sealed partial class CMUEmploymentWindow
{
    private const float BootDuration = 1.8f;
    private CrtScreenControl _crt = default!;
    private IResourceCache _hardwareResources = default!;
    private string _logoPath = string.Empty;
    private SignalHardware _signal = default!;
    private BootMeter _bootMeter = default!;
    private SpinningLogo _bootLogo = default!;
    private string _bootLine = string.Empty;
    private int _bootCharacters = -1;
    private ReaderHardware _operatorReader = default!;
    private ReaderHardware _employeeReader = default!;
    private PrinterHardware _printer = default!;
    private CabinetHardware _cabinet = default!;
    private bool _powered;
    private float _bootRemaining;
    private int _bootStep = -1;

    private void InitializeHardware(IResourceCache cache)
    {
        _hardwareResources = cache;
        _bootLogo = new SpinningLogo();
        BootLogo.AddChild(_bootLogo);
        _signal = new SignalHardware();
        SignalArt.AddChild(_signal);
        _bootMeter = new BootMeter();
        BootProgress.AddChild(_bootMeter);
        _cabinet = new CabinetHardware();
        Fascia.AddChild(_cabinet);
        var reader = cache.GetResource<RSIResource>("/Textures/CMU14/ColonyEconomy/atm_reader.rsi").RSI;
        _operatorReader = new ReaderHardware(reader);
        _employeeReader = new ReaderHardware(reader);
        _operatorReader.OnPressed += _ => OnSlot?.Invoke(true);
        _employeeReader.OnPressed += _ => OnSlot?.Invoke(false);
        _printer = new PrinterHardware();
        OperatorArt.AddChild(_operatorReader);
        TargetArt.AddChild(_employeeReader);
        PrinterArt.AddChild(_printer);
        _crt = new CrtScreenControl
        {
            Source = TubeContent,
            Curvature = 0f,
            Vignette = 0.12f,
            Roll = false,
            Grain = false,
            ArtifactAmount = 0f,
        };
        CrtLayer.AddChild(_crt);
        ModelCaption.Text = Loc.GetString("cmu-employment-model");
        CabinetCaption.Text = Loc.GetString("cmu-employment-cabinet-caption");
        SkipBootButton.Text = Loc.GetString("cmu-employment-boot-skip");
        SkipBootButton.OnPressed += _ => FinishBoot();
    }

    private void UpdateHardware()
    {
        if (_state is not { } state)
            return;

        if (_logoPath != state.Logo)
        {
            _logoPath = state.Logo;
            var logo = string.IsNullOrEmpty(_logoPath) ? null : _hardwareResources.GetTexture(_logoPath);
            FactionLogo.Texture = logo;
            _bootLogo.Texture = logo;
            FactionLogo.Visible = logo != null;
            BootLogo.Visible = logo != null;
        }
        _signal.Update(state.Powered, state.Printing, _accent);
        _bootMeter.Accent = _accent;
        _bootLogo.Accent = _accent;
        ModelCaption.Text = Loc.GetString(state.Theme switch
        {
            CMUEmploymentTheme.Govfor => "cmu-employment-model-govfor",
            CMUEmploymentTheme.Colony => "cmu-employment-model-colony",
            _ => "cmu-employment-model-weyland",
        });
        _cabinet.SetTheme(state.Theme);
        _crt.Phosphor = _accent;
        _cabinet.Accent = state.Powered ? _accent : Color.FromHex("#5A5636");
        _crt.Visible = state.Powered;
        _operatorReader.Update(state.Powered, state.OperatorCardInserted, state.Printing, state.Authenticated);
        _employeeReader.Update(state.Powered, state.Target != null, state.Printing, state.Target != null);
        _printer.Update(state.Powered, state.Printing);
        CabinetBrand.Text = Brand.Text;
        BootBrand.Text = Brand.Text;
        PowerCaption.Text = Loc.GetString(state.Powered ? "cmu-employment-power-on" : "cmu-employment-power-off");
        PrinterCaption.Text = Loc.GetString(state.Printing ? "cmu-employment-printer-working" : "cmu-employment-printer-ready");
        OperatorSlot.ToolTip = Loc.GetString("cmu-employment-reader-hint");
        _operatorReader.ToolTip = OperatorSlot.Text + "\n" + OperatorSlot.ToolTip;
        TargetSlot.ToolTip = state.TargetName + "\n" + Loc.GetString("cmu-employment-reader-hint");
        _employeeReader.ToolTip = TargetSlot.Text + "\n" + TargetSlot.ToolTip;
        CabinetBrand.FontOverride = _font;
        ModelCaption.FontOverride = _font;
        CabinetCaption.FontOverride = _font;
        PowerCaption.FontOverride = _font;
        CabinetBrand.FontColorOverride = Color.FromHex("#E4DAC2");
        ModelCaption.FontColorOverride = Color.FromHex("#BFB196");
        CabinetCaption.FontColorOverride = Color.FromHex("#302A20");
        PowerCaption.FontColorOverride = state.Powered ? _accent : Color.FromHex("#9F5746");
        CabinetPlate.PanelOverride = Box(Color.FromHex("#272821"), Color.FromHex("#A3987F"), 2);
        Bezel.PanelOverride = null;
        Screen.PanelOverride = Box(Color.FromHex("#100E09"), Color.FromHex("#373329"), 5);
        TubeContent.PanelOverride = Box(_background, Color.Transparent, 0);
        Tube.PanelOverride = Box(_background, _accent.WithAlpha(0.3f), 1);
        NavigationPanel.PanelOverride = Box(Color.FromHex("#293238"), Color.FromHex("#65716E"), 2);
        BootOverlay.PanelOverride = Box(_background, _accent.WithAlpha(0.5f), 1);
        ApplyTheme(BootOverlay);
        BootBrand.FontColorOverride = TextPrimary;
        BootStatus.FontColorOverride = TextSecondary;
        OperationsCaption.FontColorOverride = Color.FromHex("#DFD5BB");
        ReaderCaption.FontColorOverride = Color.FromHex("#DFD5BB");
        PrinterCaption.FontColorOverride = Color.FromHex("#DFD5BB");
        TerminalFooter.FontColorOverride = Color.FromHex("#BCAF94");

        if (state.Powered && !_powered)
        {
            _bootRemaining = BootDuration;
            _bootStep = -1;
            BootOverlay.Visible = true;
            _bootMeter.Progress = 0f;
            _bootLogo.Progress = 0f;
            BootStatus.Text = string.Empty;
        }
        else if (!state.Powered)
            FinishBoot();
        _powered = state.Powered;
    }

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);
        TickPresentation(args.DeltaSeconds);
        TickOfficeClocks(args.DeltaSeconds);
        if (_bootRemaining <= 0f)
            return;

        _bootRemaining = Math.Max(0f, _bootRemaining - args.DeltaSeconds);
        var progress = 1f - _bootRemaining / BootDuration;
        _bootMeter.Progress = progress;
        _bootLogo.Progress = progress;
        var step = Math.Min(2, (int) (progress * 3));
        if (step != _bootStep)
        {
            _bootStep = step;
            _bootCharacters = -1;
            _bootLine = Loc.GetString(step switch
            {
                0 => "cmu-employment-boot-memory",
                1 => "cmu-employment-boot-readers",
                _ => "cmu-employment-boot-ready",
            });
        }
        var phase = Math.Min(1f, (progress * 3f - step) * 1.6f);
        var characters = Math.Min(_bootLine.Length, (int) (phase * _bootLine.Length));
        if (characters != _bootCharacters)
        {
            _bootCharacters = characters;
            BootStatus.Text = _bootLine[..characters];
        }
        if (_bootRemaining <= 0f)
            FinishBoot();
    }

    private void FinishBoot()
    {
        _bootRemaining = 0f;
        _bootLogo.Progress = 1f;
        BootOverlay.Visible = false;
    }

    private sealed class BootMeter : Control
    {
        public float Progress { get; set; }
        public Color Accent { get; set; }

        public BootMeter()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            const int segments = 40;
            var pitch = PixelSize.X / (float) segments;
            var lit = (int) (Math.Clamp(Progress, 0f, 1f) * segments);
            for (var i = 0; i < segments; i++)
                handle.DrawRect(UIBox2.FromDimensions(i * pitch, 0, Math.Max(1f, pitch - UIScale * 2f), PixelSize.Y),
                    i < lit ? Accent.WithAlpha(0.65f) : PanelEdge);
        }
    }

    private sealed class SignalHardware : Control
    {
        private bool _powered;
        private bool _busy;
        private Color _accent;
        private float _clock;

        public SignalHardware()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        public void Update(bool powered, bool busy, Color accent)
        {
            _powered = powered;
            _busy = busy;
            _accent = accent;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (_powered)
                _clock = (_clock + args.DeltaSeconds * (_busy ? 12f : 3f)) % 8f;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var pitch = PixelSize.X / 12f;
            var color = _busy ? BusyLamp : _accent;
            var unit = PixelSize.Y / 8f;
            for (var i = 0; i < 12; i++)
            {
                var step = (i + (int) _clock) % 8;
                var height = _powered ? unit * (2 + (step < 4 ? step : 8 - step)) : unit;
                var top = PixelSize.Y - height;
                handle.DrawRect(UIBox2.FromDimensions(i * pitch, top, Math.Max(1f, pitch - UIScale * 2f), height),
                    _powered ? color.WithAlpha(0.4f) : PanelEdge);
                if (_powered)
                    handle.DrawRect(UIBox2.FromDimensions(i * pitch, top, Math.Max(1f, pitch - UIScale * 2f), UIScale), TextPrimary);
            }
        }
    }

    private sealed class CabinetHardware : Control
    {
        private Color _metal = Color.FromHex("#837763");
        private Color _highlight = Color.FromHex("#B8AB90");
        private Color _shadow = Color.FromHex("#393329");
        private static readonly Color Edge = Color.FromHex("#100E09");
        public Color Accent { get; set; }

        public CabinetHardware()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        public void SetTheme(CMUEmploymentTheme theme)
        {
            _metal = Color.FromHex(theme switch
            {
                CMUEmploymentTheme.Govfor => "#59614F",
                CMUEmploymentTheme.Colony => "#5B6C70",
                _ => "#837763",
            });
            _highlight = Blend(_metal, Color.FromHex("#DBD8C7"), 0.4f);
            _shadow = Blend(_metal, Color.FromHex("#151B20"), 0.6f);
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var w = PixelSize.X;
            var h = PixelSize.Y;
            var px = Math.Max(1f, UIScale * 2f);
            handle.DrawRect(PixelSizeBox, Edge);
            handle.DrawRect(new UIBox2(px, px, w - px, h - px), _metal);
            handle.DrawRect(new UIBox2(px, px, w - px, px * 4), _highlight);
            handle.DrawRect(new UIBox2(px, px, px * 4, h - px), _highlight);
            handle.DrawRect(new UIBox2(px * 4, h - px * 5, w - px, h - px), _shadow);
            handle.DrawRect(new UIBox2(w - px * 5, px * 4, w - px, h - px), _shadow);
            for (var column = 0; column < 2; column++)
            for (var row = 0; row < 2; row++)
            {
                var x = column == 0 ? px * 4 : w - px * 7;
                var y = row == 0 ? px * 4 : h - px * 8;
                handle.DrawRect(UIBox2.FromDimensions(x, y, px * 3, px * 3), _shadow);
                handle.DrawRect(UIBox2.FromDimensions(x, y, px * 2, px), _highlight);
                handle.DrawRect(UIBox2.FromDimensions(x, y + px, px * 3, px), Edge);
            }
            handle.DrawRect(UIBox2.FromDimensions(w - px * 22, h - px * 14, px * 10, px * 2), Accent.WithAlpha(0.6f));
        }
    }

    private static readonly Color ReaderSlot = Color.FromHex("#050403");
    private static readonly Color ReaderEdge = Color.FromHex("#17140F");
    private static readonly Color PrinterEdge = Color.FromHex("#18160F");
    private static readonly Color ReaderRecess = Color.FromHex("#29241C");
    private static readonly Color ReadyLamp = Color.FromHex("#46B858");
    private static readonly Color CardStripe = Color.FromHex("#586A63");
    private static readonly Color UnpoweredLamp = Color.FromHex("#5A5636");
    private static readonly Color PrinterMetal = Color.FromHex("#706A54");
    private static readonly Color ReaderMetal = Color.FromHex("#72664F");
    private static readonly Color CardText = Color.FromHex("#837E77");
    private static readonly Color PaperText = Color.FromHex("#8C8A76");
    private static readonly Color CardPortrait = Color.FromHex("#8F9D92");
    private static readonly Color ReaderRim = Color.FromHex("#A1A67D");
    private static readonly Color PaperStock = Color.FromHex("#DBD5BD");
    private static readonly Color CardStock = Color.FromHex("#DDD7C1");
    private static readonly Color BusyLamp = Color.FromHex("#FFB547");

    private sealed class ReaderHardware : Button
    {
        private readonly RSI _rsi;
        private Texture[] _frames = Array.Empty<Texture>();
        private float[] _delays = Array.Empty<float>();
        private string? _signal;
        private int _frame;
        private float _elapsed;
        private float _card;
        private bool _inserted;

        public ReaderHardware(RSI rsi)
        {
            _rsi = rsi;
            MouseFilter = MouseFilterMode.Stop;
            StyleBoxOverride = new StyleBoxFlat { BackgroundColor = Color.Transparent };
        }

        public void Update(bool powered, bool inserted, bool busy, bool authorized)
        {
            _inserted = inserted;
            var signal = !powered ? "lights_idle" : busy ? "lights_reading"
                : inserted ? authorized ? "lights_ok" : "lights_locked" : "lights_ready";
            if (_signal == signal)
                return;
            _signal = signal;
            _frame = 0;
            _elapsed = 0f;
            if (_rsi.TryGetState(signal, out var state))
            {
                _frames = state.GetFrames(RsiDirection.South);
                _delays = state.GetDelays();
            }
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            _card = _inserted ? Math.Min(1f, _card + args.DeltaSeconds * 5f)
                : Math.Max(0f, _card - args.DeltaSeconds * 5f);
            if (_frames.Length <= 1)
                return;
            _elapsed += args.DeltaSeconds;
            if (_elapsed < _delays[_frame])
                return;
            // A stalled UI resumes at the next frame instead of replaying missed flashes.
            _elapsed = 0f;
            _frame = (_frame + 1) % _frames.Length;
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var scale = PixelSize.Y / 42f;
            var x = (PixelSize.X - 51f * scale) / 2f;
            void Rect(float left, float top, float width, float height, Color color)
                => handle.DrawRect(UIBox2.FromDimensions(x + left * scale, top * scale, width * scale, height * scale), color);
            Rect(3, 2, 45, 38, ReaderEdge);
            Rect(4, 3, 43, 36, ReaderMetal);
            Rect(7, 7, 37, 31, ReaderRecess);
            Rect(16, 10, 18, 25, ReaderRim);
            Rect(17, 11, 16, 23, ReaderSlot);
            if (_card > 0f)
            {
                var top = (float) Math.Floor(3f + _card * 12f);
                Rect(19, top, 12, 17, CardStock);
                Rect(19, top, 12, 3, CardStripe);
                Rect(21, top + 5, 4, 5, CardPortrait);
                Rect(26, top + 5, 3, 1, CardText);
                Rect(26, top + 8, 3, 1, CardText);
            }
            Rect(16, 30, 18, 6, ReaderRecess);
            Rect(16, 30, 18, 1, ReaderRim);
            if (_frames.Length > 0)
                handle.DrawTextureRect(_frames[_frame], UIBox2.FromDimensions(x, 0f, 51f * scale, 42f * scale));
            if (DrawMode is DrawModeEnum.Hover or DrawModeEnum.Pressed)
                handle.DrawRect(UIBox2.FromDimensions(x + 3f * scale, 2f * scale, 45f * scale, 38f * scale),
                    CardStock.WithAlpha(0.6f), filled: false);
        }
    }

    private sealed class PrinterHardware : Control
    {
        private bool _powered;
        private bool _printing;
        private float _feed;
        private float _clock;

        public PrinterHardware()
        {
            MouseFilter = MouseFilterMode.Ignore;
        }

        public void Update(bool powered, bool printing)
        {
            _powered = powered;
            if (printing && !_printing)
                _feed = 0f;
            _printing = printing;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            if (_powered && _printing)
            {
                _feed = Math.Min(1f, _feed + args.DeltaSeconds * 0.7f);
                _clock = (_clock + args.DeltaSeconds) % 0.5f;
            }
            else if (!_printing)
                _feed = Math.Max(0f, _feed - args.DeltaSeconds * 1.5f);
        }

        protected override void Draw(DrawingHandleScreen handle)
        {
            var w = PixelSize.X;
            var h = PixelSize.Y;
            var px = Math.Max(1f, UIScale * 2f);
            handle.DrawRect(new UIBox2(0, h / 3f, w, h - px), PrinterEdge);
            handle.DrawRect(new UIBox2(px, h / 3f + px, w - px, h - px * 2), PrinterMetal);
            handle.DrawRect(new UIBox2(px * 4, h / 3f + px * 2, w - px * 4, h / 3f + px * 6), ReaderSlot);
            var paperTop = h / 3f + px * 4;
            var paperHeight = (float) Math.Floor((h - paperTop - px * 2) * _feed);
            if (paperHeight > 0f)
            {
                handle.DrawRect(UIBox2.FromDimensions(px * 8, paperTop, w - px * 16, paperHeight), PaperStock);
                if (paperHeight > px * 3)
                    handle.DrawRect(UIBox2.FromDimensions(px * 10, paperTop + px, px * 6, px), CardStripe);
                for (var y = paperTop + px * 4; y < paperTop + paperHeight - px; y += px * 3)
                    handle.DrawRect(UIBox2.FromDimensions(px * 10, y, w - px * 21, px), PaperText);
            }
            var lamp = !_powered ? UnpoweredLamp : _printing && _clock < 0.25f ? BusyLamp : ReadyLamp;
            handle.DrawRect(UIBox2.FromDimensions(px, px * 2, px * 3, px * 2), lamp);
            if (_powered && _printing)
            {
                var travel = (float) Math.Floor(_clock * 12f) * px;
                handle.DrawRect(UIBox2.FromDimensions(w / 2f - px * 3 + travel, h / 3f + px * 3, px * 2, px), BusyLamp);
            }
        }
    }
}
