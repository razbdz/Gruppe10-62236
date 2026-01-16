using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using PackBot.Data;

namespace PackBot;

public partial class MainWindow : Window
{
    private const string GroupId = "62236";

    // Robot + demo sensorer
    private Robot? _robot;
    private bool _di3;
    private bool _di7;

    // DB
    private int? _bagCountFromDb;
    private readonly OrderDbService _db = new();
    private readonly AuthService _auth = new();

    // Login state
    private bool _loggedIn;
    private bool _isAdmin;
    private string _currentUser = "";

    // UI timer
    private readonly DispatcherTimer _uiTimer;

    public MainWindow()
    {
        InitializeComponent();

        // UI timer (4x i sekundet)
        _uiTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
        _uiTimer.Tick += (_, _) => RefreshUi();
        _uiTimer.Start();

        Log("GUI started.");

        // Init DB
        _ = _db.InitAsync().ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (t.Exception != null)
                    Log("DB init ERROR: " + t.Exception.GetBaseException().Message);
                else
                    Log("DB ready (packbot.sqlite).");
            });
        });

        // Seed admin
        _ = _auth.EnsureAdminSeedAsync().ContinueWith(t =>
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (t.Exception != null)
                    Log("Auth seed ERROR: " + t.Exception.GetBaseException().Message);
                else
                    Log("Auth ready. Default admin: admin / admin123");
            });
        });

        UpdateAccessControl();
    }

    // ---------- UI ----------
    private void RefreshUi()
    {
        Di3Text.Text = _di3 ? "True" : "False";
        Di7Text.Text = _di7 ? "True" : "False";

        var sensorDecision =
            (_di3 && _di7) ? "STOR BOX" :
            (_di7) ? "LILLE BOX" :
            "VENTER";

        DecisionText.Text = sensorDecision;

        var connected = _robot?.Connected ?? false;
        RobotStateText.Text = connected
            ? $"State: Connected ({_robot!.RobotMode})"
            : "State: Disconnected";

        StepText.Text = sensorDecision switch
        {
            "STOR BOX"  => "Step: MoveJ(tag_stor_box) → ... → RG Grip(90)",
            "LILLE BOX" => "Step: MoveJ(tag_lille_box) → ... → RG Grip(90)",
            _           => "Step: Wait (sensor condition)"
        };

        UpdateBusinessUi(sensorDecision);
    }

    private void UpdateBusinessUi(string sensorDecision)
    {
        if (_bagCountFromDb is null)
        {
            BagCountText.Text = "—";
            DbDecisionText.Text = "—";
            VerificationText.Text = "—";
            return;
        }

        var count = _bagCountFromDb.Value;
        BagCountText.Text = count.ToString();

        var dbDecision = count > 5 ? "STOR BOX" : "LILLE BOX";
        DbDecisionText.Text = dbDecision;

        VerificationText.Text =
            sensorDecision == dbDecision
                ? "✅ Verified (match)"
                : "⚠️ Mismatch (check bags/sensors)";
    }

    private void UpdateAccessControl()
    {
        LoginStateText.Text = _loggedIn
            ? $"Logged in as {_currentUser} ({(_isAdmin ? "admin" : "user")})"
            : "Not logged in";

        UsersAdminPanel.IsEnabled = _isAdmin;
        DbAdminPanel.IsEnabled = _isAdmin;
    }

    // ---------- LOGIN ----------
    private async void LoginClick(object? sender, RoutedEventArgs e)
    {
        var u = LoginUsernameBox.Text?.Trim() ?? "";
        var p = LoginPasswordBox.Text ?? "";

        if (string.IsNullOrWhiteSpace(u) || string.IsNullOrWhiteSpace(p))
        {
            Log("Login failed: missing username/password.");
            return;
        }

        try
        {
            var (ok, isAdmin) = await _auth.LoginAsync(u, p);

            if (!ok)
            {
                Log("Login failed: wrong credentials.");
                return;
            }

            // UI updates på UI-tråden
            Dispatcher.UIThread.Post(() =>
            {
                _loggedIn = true;
                _isAdmin = isAdmin;
                _currentUser = u;

                LoginPasswordBox.Text = "";
                Log($"User logged in: {_currentUser} (admin={_isAdmin})");
                UpdateAccessControl();
            });
        }
        catch (Exception ex)
        {
            Log("ERROR (login): " + ex.Message);
        }
    }

    private void LogoutClick(object? sender, RoutedEventArgs e)
    {
        _loggedIn = false;
        _isAdmin = false;
        _currentUser = "";

        LoginPasswordBox.Text = "";
        Log("Logged out.");
        UpdateAccessControl();
    }

    // ---------- USERS (ADMIN) ----------
    private async void RegisterUserClick(object? sender, RoutedEventArgs e)
    {
        if (!_isAdmin)
        {
            Log("Access denied: admin only.");
            return;
        }

        try
        {
            var u = NewUsernameBox.Text?.Trim() ?? "";
            var p = NewPasswordBox.Text ?? "";
            var admin = NewIsAdminBox.IsChecked ?? false;

            await _auth.RegisterAsync(u, p, admin);
            Log($"User created: {u} (admin={admin})");

            NewUsernameBox.Text = "";
            NewPasswordBox.Text = "";
            NewIsAdminBox.IsChecked = false;
        }
        catch (Exception ex)
        {
            Log("ERROR (register): " + ex.Message);
        }
    }

    // ---------- ORDER ----------
    private async void FetchOrderClick(object? sender, RoutedEventArgs e)
    {
        var id = OrderIdBox.Text?.Trim() ?? "";

        if (id == "")
        {
            Log("Fetch failed: Order ID missing.");
            return;
        }

        try
        {
            _bagCountFromDb = await _db.GetBagCountAsync(id);

            if (_bagCountFromDb is null)
            {
                BagCountText.Text = "—";
                DbDecisionText.Text = "—";
                VerificationText.Text = "Order not found";
                Log($"Order {id} not found.");
                return;
            }

            Log($"Fetched order {id} from DB. BagCount={_bagCountFromDb}");
        }
        catch (Exception ex)
        {
            Log("ERROR (fetch order): " + ex.Message);
        }
    }

    // ---------- DB ADMIN ----------
    private async void DbUpsertClick(object? sender, RoutedEventArgs e)
    {
        if (!_isAdmin)
        {
            Log("Access denied: admin only.");
            return;
        }

        try
        {
            var id = DbOrderIdBox.Text?.Trim() ?? "";
            var count = int.Parse(DbBagCountBox.Text ?? "0");
            await _db.UpsertOrderAsync(id, count);
            Log($"DB Upsert OK: Order={id}, BagCount={count}");
        }
        catch (Exception ex)
        {
            Log("ERROR (db upsert): " + ex.Message);
        }
    }

    private async void DbSeedClick(object? sender, RoutedEventArgs e)
    {
        if (!_isAdmin)
        {
            Log("Access denied: admin only.");
            return;
        }

        try
        {
            await _db.SeedDemoAsync(false);
            Log("DB seed done (if empty).");
        }
        catch (Exception ex)
        {
            Log("ERROR (db seed): " + ex.Message);
        }
    }

    private async void DbResetClick(object? sender, RoutedEventArgs e)
    {
        if (!_isAdmin)
        {
            Log("Access denied: admin only.");
            return;
        }

        try
        {
            await _db.ResetAsync();
            _bagCountFromDb = null;
            Log("DB reset done.");
        }
        catch (Exception ex)
        {
            Log("ERROR (db reset): " + ex.Message);
        }
    }

    // ---------- ROBOT ----------
    private async void ConnectClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            var ip = IpBox.Text?.Trim() ?? "localhost";
            var port = int.Parse(PortBox.Text ?? "30002");

            _robot = new Robot(ipAddress: ip, dashboardPort: 29999, urscriptPort: port);
            _robot.Connect();
            await _robot.PowerOn();
            await _robot.BrakeRelease();

            Log($"Robot connected: {ip}:{port}");
        }
        catch (Exception ex)
        {
            Log("ERROR (connect): " + ex.Message);
        }
    }

    private void StartClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _robot?.SendUrscriptFile("Robot.script");
            Log("Start cycle pressed.");
        }
        catch (Exception ex)
        {
            Log("ERROR (start): " + ex.Message);
        }
    }

    private void StopClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            _robot?.StopProgram();
            Log("Stop pressed.");
        }
        catch (Exception ex)
        {
            Log("ERROR (stop): " + ex.Message);
        }
    }

    // ---------- DEMO ----------
    private void SimulateSensorsClick(object? sender, RoutedEventArgs e)
    {
        _di7 = !_di7;
        if (_di7) _di3 = !_di3;
        Log("Simulated DI3/DI7 toggle.");
    }

    // ---------- LOG ----------
    private void Log(string msg)
    {
        LogText.Text = $"[{DateTime.Now:HH:mm:ss}] {GroupId} {msg}\n" + LogText.Text;
    }
}
