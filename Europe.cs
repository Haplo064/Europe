using System;
using Dalamud.Plugin;
using ImGuiNET;
using Dalamud.Configuration;
using Num = System.Numerics;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.Hooking;

namespace AccurateCountDown
{
    public class Europe : IDalamudPlugin
    {
        public string Name => "Accurate Count Down";
        private DalamudPluginInterface _pluginInterface;
        private Config _configuration;
        private bool _enabled = true;
        private bool _enableEnc = true;
        private bool _enableCdEnc = true;
        private bool _config;
        private bool _start = true;
        private float _scale = 2f;
        private bool _debug;
        private int _pauseCheck;
        private float _lastTime;
        private bool _flipSwitch = true;
        private static bool _noTitlebar = true;
        private static bool _noMove;
        private static bool _noResize;
        private static bool _noMouse;
        private static bool _noBox;
        
        //void FUN_140298840(longlong param_1)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr CountdownTimer(ulong param1);
        private CountdownTimer _countdownTimer;
        private Hook<CountdownTimer> _countdownTimerHook;
        private IntPtr _countdownPtr;
        private ulong _countDown ;
        private DateTime _cdEnd = new DateTime(2010);
        private DateTime _encStart = new DateTime(1991);
        private DateTime _encEnd = new DateTime(2020);
        
        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
            _configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            _pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            _pluginInterface.CommandManager.AddHandler("/ctd", new CommandInfo(Command)
            {
                HelpMessage = "Accurate Countdown config."
            });

            _enabled = _configuration.Enabled;
            _enableEnc = _configuration.EnabledEnc;
            _scale = _configuration.Scale;
            _enableCdEnc = _configuration.EnabledCdenc;
            _noTitlebar = _configuration.NoTitlebar;
            _noMove = _configuration.NoMove;
            _noResize = _configuration.NoResize;
            _noMouse = _configuration.NoMouse;
            _noBox = _configuration.NoBox;
            _countdownPtr = pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41");
            _countdownTimer = CountdownTimerFunc;
            try
            {_countdownTimerHook = new Hook<CountdownTimer>(_countdownPtr, _countdownTimer, this); _countdownTimerHook.Enable();}
            catch(Exception e)
            {PluginLog.Log("BAD\n"+e);}
        }

        private IntPtr CountdownTimerFunc(ulong param1)
        {
            _countDown = param1;
            return _countdownTimerHook.Original(param1);
        }

        private void Command(string command, string arguments)
        {
            _config = true;
        }

        public void Dispose()
        {
            _pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            _pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            _pluginInterface.CommandManager.RemoveHandler("/ctd");
            _countdownTimerHook.Disable();
        }

        private void ConfigWindow(object sender, EventArgs args)
        {
            _config = true;
        }

        private void DrawWindow()
        {
            ImGuiWindowFlags windowFlags = 0;
            if (_noTitlebar) windowFlags |= ImGuiWindowFlags.NoTitleBar;
            windowFlags |= ImGuiWindowFlags.NoScrollbar;
            if (_noMove) windowFlags |= ImGuiWindowFlags.NoMove;
            if (_noResize) windowFlags |= ImGuiWindowFlags.NoResize;
            if (_noMouse) windowFlags |= ImGuiWindowFlags.NoMouseInputs;
            if (_noBox) windowFlags |= ImGuiWindowFlags.NoBackground;

            if (_config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(300, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Accurate Countdown Config", ref _config);
                ImGui.Checkbox("Enable Countdown", ref _enabled);
                ImGui.Checkbox("Enable EncounterTimer", ref _enableEnc);
                ImGui.Checkbox(" - Only show EncounterTimer after a CountDown", ref _enableCdEnc);
                ImGui.Checkbox("No TitleBar", ref _noTitlebar);
                ImGui.Checkbox("No Background", ref _noBox);
                ImGui.Checkbox("No Moving", ref _noMove);
                ImGui.Checkbox("No Resizing", ref _noResize);
                ImGui.Checkbox("Clickthrough", ref _noMouse);
                ImGui.InputFloat("Font Scale", ref _scale);
                ImGui.Separator();
                ImGui.Checkbox("Debug", ref _debug);

                if (_noTitlebar) windowFlags |= ImGuiWindowFlags.NoTitleBar;
                windowFlags |= ImGuiWindowFlags.NoScrollbar;
                if (_noMove) windowFlags |= ImGuiWindowFlags.NoMove;
                if (_noResize) windowFlags |= ImGuiWindowFlags.NoResize;
                windowFlags |= ImGuiWindowFlags.NoCollapse;
                if (_noMouse) windowFlags |= ImGuiWindowFlags.NoMouseInputs;
                if (_noBox) windowFlags |= ImGuiWindowFlags.NoBackground;

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    _config = false;
                }
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

                if (ImGui.Button("Buy Haplo a Hot Chocolate"))
                {
                    System.Diagnostics.Process.Start("https://ko-fi.com/haplo");
                }
                ImGui.PopStyleColor(3);
                ImGui.End();
            }

            if (_debug)
            {
                ImGui.Begin("Encounter", ref _debug, windowFlags);
                ImGui.SetWindowFontScale(_scale);
                ImGui.Text("99:99");
                ImGui.SetWindowFontScale(1f);
                ImGui.End();

                ImGui.Begin("Countdown", ref _debug, windowFlags);
                ImGui.SetWindowFontScale(_scale);
                ImGui.Text("99:9.99");
                ImGui.SetWindowFontScale(1f);
                ImGui.End();
            }

            if (_enabled && _countDown !=0)
            {
                if (Marshal.PtrToStructure<float>((IntPtr)_countDown + 0x2c) == _lastTime) { _pauseCheck++; }
                else { _pauseCheck = 0; _flipSwitch = true; }

                if (_pauseCheck > 50) { _flipSwitch = false; }

                if (Marshal.PtrToStructure<float>((IntPtr)_countDown + 0x2c) > 0 && _flipSwitch)
                {
                    ImGui.Begin("Countdown", ref _enabled, windowFlags);
                    ImGui.SetWindowFontScale(_scale);
                    ImGui.Text($"{Marshal.PtrToStructure<float>((IntPtr) _countDown + 0x2c):0.00}");
                    ImGui.SetWindowFontScale(1f);
                    ImGui.End();
                    _cdEnd = DateTime.Now;
                }

                if(Marshal.PtrToStructure<float>((IntPtr)_countDown + 0x2c) <= 0 && (DateTime.Now - _cdEnd).TotalSeconds < 3 && _flipSwitch)
                {
                    ImGui.Begin("Countdown", ref _enabled, windowFlags);
                    ImGui.SetWindowFontScale(_scale);
                    ImGui.Text("FIGHT");
                    ImGui.SetWindowFontScale(1f);
                    ImGui.End();
                }
                _lastTime = Marshal.PtrToStructure<float>((IntPtr)_countDown + 0x2c);
            }

            if (!_enableEnc) return;
            if (_pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat])
            {
                if (_start)
                {
                    _start = false;
                    _encStart = DateTime.Now;
                }
                _encEnd = DateTime.Now;
            }
            else
            {
                _start = true;
            }

            if (_enableCdEnc && Math.Abs((DateTime.Now - _cdEnd).TotalSeconds - (DateTime.Now - _encStart).TotalSeconds) >= 10)
            {
                return;
            }

            if (!((DateTime.Now - _encEnd).TotalSeconds <= 10)) return;
            var diff = _encEnd - _encStart;
            ImGui.Begin("Encounter", ref _enableEnc, windowFlags);
            ImGui.SetWindowFontScale(_scale);
            ImGui.Text(diff.ToString(@"mm\:ss"));
            ImGui.SetWindowFontScale(1f);
            ImGui.End();
        }

        private void SaveConfig()
        {
            _configuration.Enabled = _enabled;
            _configuration.EnabledEnc = _enableEnc;
            _configuration.EnabledCdenc = _enableCdEnc;
            _configuration.Scale = _scale;
            _configuration.NoTitlebar = _noTitlebar;
            _configuration.NoMove = _noMove;
            _configuration.NoResize = _noResize;
            _configuration.NoMouse = _noMouse;
            _configuration.NoBox = _noBox;
            _pluginInterface.SavePluginConfig(_configuration);
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool EnabledEnc { get; set; }
        public bool EnabledCdenc { get; set; }
        public float Scale { get; set; } = 1;
        public bool NoTitlebar { get; set; } = true;
        public bool NoMove { get; set; }
        public bool NoResize { get; set; }
        public bool NoMouse { get; set; }
        public bool NoBox { get; set; }
    }
}
