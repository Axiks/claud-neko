using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CloudNeko;

public partial class MainWindow : Window
{
    private static readonly TimeSpan DoneCelebrationDuration = TimeSpan.FromSeconds(3.2);
    private static readonly TimeSpan PetReactionDuration = TimeSpan.FromSeconds(1.6);

    private static readonly Dictionary<NekoState, string> PortraitFiles = new()
    {
        [NekoState.Waiting] = "akari_sleepy.png",
        [NekoState.Working] = "akari_determined.png",
        [NekoState.Polling] = "akari_surprised.png",
        [NekoState.Done] = "akari_love.png",
    };

    private const string PetPortraitFile = "akari_blush.png";

    private readonly StateFileWatcher _watcher = new();
    private readonly Stopwatch _clock = Stopwatch.StartNew();
    private readonly DispatcherTimer _tickTimer = new() { Interval = TimeSpan.FromMilliseconds(50) };
    private readonly Dictionary<NekoState, BitmapImage> _portraits = new();
    private BitmapImage _petPortrait = null!;

    private readonly TranslateTransform _dot0T = new();
    private readonly TranslateTransform _dot1T = new();
    private readonly TranslateTransform _dot2T = new();

    private NekoState _aggregateState = NekoState.Waiting;
    private bool _celebrating;
    private double _celebrationStartT;
    private double? _petUntilT;
    private bool _wasPetting;
    private NekoState? _displayedPortrait;
    private bool _displayedIsPet;
    private Point _dragAnchorScreen;
    private bool _isDragging;

    public MainWindow()
    {
        InitializeComponent();

        foreach (var (state, file) in PortraitFiles)
        {
            var bmp = new BitmapImage(new Uri($"pack://application:,,,/Assets/Akari/{file}"));
            bmp.Freeze();
            _portraits[state] = bmp;
        }

        _petPortrait = new BitmapImage(new Uri($"pack://application:,,,/Assets/Akari/{PetPortraitFile}"));
        _petPortrait.Freeze();

        Dot0.RenderTransform = _dot0T;
        Dot1.RenderTransform = _dot1T;
        Dot2.RenderTransform = _dot2T;

        Loaded += (_, _) => PositionOnScreen();

        _watcher.StateChanged += OnAggregateChanged;
        _watcher.PetRequested += () => Dispatcher.Invoke(TriggerPet);
        _watcher.DoneCelebrationRequested += () => Dispatcher.Invoke(TriggerDoneCelebration);
        _aggregateState = _watcher.ReadCurrent();

        _tickTimer.Tick += (_, _) => Render();
        _tickTimer.Start();

        ApplyBubbleStyle(_aggregateState);
        Render();
    }

    private void PositionOnScreen()
    {
        var area = SystemParameters.WorkArea;
        Left = area.Right - Width - 24;
        Top = area.Bottom - Height - 24;
    }

    /// <summary>Агрегований стан за всіма активними сесіями (waiting/working/polling).</summary>
    private void OnAggregateChanged(NekoState newState)
    {
        Dispatcher.Invoke(() =>
        {
            _aggregateState = newState;
            if (!_celebrating)
            {
                ApplyBubbleStyle(EffectiveState());
            }
        });
    }

    private NekoState EffectiveState() => _celebrating ? NekoState.Done : _aggregateState;

    private void Render()
    {
        double t = _clock.Elapsed.TotalSeconds;

        if (_celebrating && t - _celebrationStartT > DoneCelebrationDuration.TotalSeconds)
        {
            _celebrating = false;
            ApplyBubbleStyle(EffectiveState());
        }

        var state = EffectiveState();
        bool petting = _petUntilT.HasValue && t < _petUntilT.Value;
        if (!petting && _petUntilT.HasValue)
        {
            _petUntilT = null;
        }

        if (petting != _wasPetting || (!petting && _displayedPortrait != state) || (petting && !_displayedIsPet))
        {
            CharacterImage.Source = petting ? _petPortrait : _portraits[state];
            _displayedPortrait = state;
            _displayedIsPet = petting;
        }

        if (petting != _wasPetting)
        {
            ApplyBubbleStyle(petting ? null : state);
            _wasPetting = petting;
        }

        // --- Трансформації портрета ---
        double breatheAmplitude = petting ? 0.05 : 0.02;
        double breathe = 1 + breatheAmplitude * Math.Sin(t * (petting ? 5 : 2));
        CharScale.ScaleX = breathe;
        CharScale.ScaleY = breathe;

        CharRotate.Angle = !petting && state == NekoState.Polling ? Math.Sin(t * 4) * 6 : 0;

        if (_celebrating)
        {
            double hopT = t - _celebrationStartT;
            double amplitude = 12 * Math.Max(0, 1 - hopT / DoneCelebrationDuration.TotalSeconds);
            CharTranslate.Y = -Math.Abs(Math.Sin(hopT * 6)) * amplitude;
        }
        else
        {
            CharTranslate.Y = 0;
        }

        // --- Бульбашка ---
        if (petting)
        {
            double petT = t - (_petUntilT!.Value - PetReactionDuration.TotalSeconds);
            BubbleTranslate.X = 0;
            BubbleTranslate.Y = -Math.Sin(petT * 8) * 1.5;
            double petPulse = 1 + 0.15 * Math.Abs(Math.Sin(petT * 7));
            BubbleScale.ScaleX = BubbleScale.ScaleY = petPulse;
            return;
        }

        switch (state)
        {
            case NekoState.Waiting:
                BubbleTranslate.X = 0;
                BubbleTranslate.Y = Math.Sin(t * 1.5) * 2;
                BubbleScale.ScaleX = BubbleScale.ScaleY = 1;
                break;

            case NekoState.Working:
                BubbleTranslate.X = 0;
                BubbleTranslate.Y = 0;
                BubbleScale.ScaleX = BubbleScale.ScaleY = 1;
                for (int i = 0; i < 3; i++)
                {
                    double phase = t * 5 - i * 0.6;
                    double bounce = Math.Max(0, Math.Sin(phase * Math.PI));
                    var tt = i == 0 ? _dot0T : i == 1 ? _dot1T : _dot2T;
                    tt.Y = -5 * bounce;
                }
                break;

            case NekoState.Polling:
                BubbleTranslate.X = Math.Sin(t * 18) * 1.5;
                BubbleTranslate.Y = Math.Sin(t * 3) * 1.5;
                BubbleScale.ScaleX = BubbleScale.ScaleY = 1;
                break;

            case NekoState.Done:
                BubbleTranslate.X = 0;
                BubbleTranslate.Y = 0;
                double pulse = 1 + 0.12 * Math.Abs(Math.Sin(t * 6));
                BubbleScale.ScaleX = BubbleScale.ScaleY = pulse;
                break;
        }
    }

    private void ApplyBubbleStyle(NekoState? state)
    {
        (Color bg, Color border, string text, bool showDots) = state switch
        {
            NekoState.Waiting => (Color.FromRgb(0x6C, 0x7A, 0x96), Color.FromRgb(0x4B, 0x55, 0x68), "Zzz", false),
            NekoState.Working => (Color.FromRgb(0xE8, 0xA3, 0x3D), Color.FromRgb(0xB9, 0x7B, 0x1F), "", true),
            NekoState.Polling => (Color.FromRgb(0x9B, 0x6B, 0xC9), Color.FromRgb(0x6E, 0x4A, 0x94), "?", false),
            NekoState.Done => (Color.FromRgb(0x4C, 0xAF, 0x7D), Color.FromRgb(0x35, 0x7A, 0x57), "✓", false),
            null => (Color.FromRgb(0xE0, 0x6B, 0x9A), Color.FromRgb(0xA8, 0x3E, 0x6B), "♥", false),
            _ => (Color.FromRgb(0x6C, 0x7A, 0x96), Color.FromRgb(0x4B, 0x55, 0x68), "Zzz", false),
        };

        Bubble.Background = new SolidColorBrush(bg);
        Bubble.BorderBrush = new SolidColorBrush(border);
        BubbleTail.Fill = new SolidColorBrush(bg);

        BubbleText.Text = text;
        BubbleText.Visibility = showDots ? Visibility.Collapsed : Visibility.Visible;
        BubbleDots.Visibility = showDots ? Visibility.Visible : Visibility.Collapsed;
    }

    // Перетягування зроблене вручну (без Window.DragMove()) — DragMove() емулює
    // HTCAPTION-перетягування заголовка, а Windows перехоплює швидкий другий клік у
    // цій зоні як системний жест і не пропускає його до WPF, через що ClickCount
    // ніколи не досягав 2. Ручний drag через CaptureMouse не має цієї проблеми.
    private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2)
        {
            TriggerPet();
            return;
        }

        _dragAnchorScreen = PointToScreen(e.GetPosition(this));
        _isDragging = true;
        CaptureMouse();
    }

    private void Window_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        var current = PointToScreen(e.GetPosition(this));
        Left += current.X - _dragAnchorScreen.X;
        Top += current.Y - _dragAnchorScreen.Y;
        _dragAnchorScreen = current;
    }

    private void Window_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        ReleaseMouseCapture();
    }

    private void TriggerPet()
    {
        _petUntilT = _clock.Elapsed.TotalSeconds + PetReactionDuration.TotalSeconds;
    }

    private void TriggerDoneCelebration()
    {
        _celebrating = true;
        _celebrationStartT = _clock.Elapsed.TotalSeconds;
        ApplyBubbleStyle(EffectiveState());
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _watcher.Dispose();
        Application.Current.Shutdown();
    }
}
