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
        private DalamudPluginInterface pluginInterface;
        public Config Configuration;

        public bool enabled = true;
        public bool enableEnc = true;
        public bool enableCDEnc = true;
        public bool config = false;
        public bool start = true;
        public float scale = 2f;
        public bool debug = false;
        public int pauseCheck = 0;
        public float lastTime = 0;
        public bool flipSwitch = true;

        static bool no_titlebar = true;
        static bool no_scrollbar = true;
        static bool no_move = false;
        static bool no_resize = false;
        static bool no_mouse = false;
        static bool no_box = false;


        //void FUN_140298840(longlong param_1)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr CountdownTimer(ulong param_1);
        private CountdownTimer countdownTimer;
        private Hook<CountdownTimer> countdownTimerHook;
        public IntPtr countdownPTR;

        public ulong countDown = 0;
        public float countUp = 0.00f;
        public DateTime CDEnd = new DateTime(2010);
        public DateTime EncStart = new DateTime(1991);
        public DateTime EncEnd = new DateTime(2020);

        public Num.Vector4 colour;
        public bool check = true;

        public IntPtr stateFlags;
        public bool[] stateFlagsArray;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            stateFlags = IntPtr.Zero;
            stateFlagsArray = new bool[100];

            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();

            this.pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            this.pluginInterface.CommandManager.AddHandler("/ctd", new CommandInfo(Command)
            {
                HelpMessage = "Accurate Countdown config."
            });

            enabled = Configuration.Enabled;
            enableEnc = Configuration.EnabledENC;
            scale = Configuration.Scale.Value;
            enableCDEnc = Configuration.EnabledCDENC;
            no_titlebar = Configuration.No_Titlebar;
            no_move = Configuration.No_Move;
            no_resize = Configuration.No_Resize;
            no_mouse = Configuration.No_Mouse;
            no_box = Configuration.No_Box;



            //countdownPTR = pluginInterface.TargetModuleScanner.ScanText("?? 89 ?? ?? ?? 57 48 83 ?? ?? 8B ?? ?? 48 8B ?? ?? 41 ?? 48 8B");
            countdownPTR = pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 40 8B 41");
            //funcPtr1 = pluginInterface.TargetModuleScanner.Module.BaseAddress + 0x298840;
            countdownTimer = new CountdownTimer(countdownTimerFunc);
            try
            {countdownTimerHook = new Hook<CountdownTimer>(countdownPTR, countdownTimer, this); countdownTimerHook.Enable();}
            catch(Exception e)
            {PluginLog.Log("BAD\n"+e.ToString());}


        }

        public IntPtr countdownTimerFunc(ulong param_1)
        {
            //PluginLog.Log(param_1.ToString());
            countDown = param_1;
            return countdownTimerHook.Original(param_1);
        }

        public void Command(string command, string arguments)
        {
            config = true;
        }

        public void Dispose()
        {
            this.pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
            this.pluginInterface.CommandManager.RemoveHandler("/ctd");
            countdownTimerHook.Disable();
        }

        private void ConfigWindow(object Sender, EventArgs args)
        {
            config = true;
        }

        private void DrawWindow()
        {
            ImGuiWindowFlags window_flags = 0;
            if (no_titlebar) window_flags |= ImGuiWindowFlags.NoTitleBar;
            if (no_scrollbar) window_flags |= ImGuiWindowFlags.NoScrollbar;
            if (no_move) window_flags |= ImGuiWindowFlags.NoMove;
            if (no_resize) window_flags |= ImGuiWindowFlags.NoResize;
            if (no_mouse) window_flags |= ImGuiWindowFlags.NoMouseInputs;
            if (no_box) window_flags |= ImGuiWindowFlags.NoBackground;

            if (config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(300, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Accurate Countdown Config", ref config);
                ImGui.Checkbox("Enable Countdown", ref enabled);
                ImGui.Checkbox("Enable EncounterTimer", ref enableEnc);
                ImGui.Checkbox(" - Only show EncounterTimer after a CountDown", ref enableCDEnc);
                ImGui.Checkbox("No TitleBar", ref no_titlebar);
                ImGui.Checkbox("No Background", ref no_box);
                ImGui.Checkbox("No Moving", ref no_move);
                ImGui.Checkbox("No Resizing", ref no_resize);
                ImGui.Checkbox("Clickthrough", ref no_mouse);
                ImGui.InputFloat("Font Scale", ref scale);
                ImGui.Separator();
                ImGui.Checkbox("Debug", ref debug);

                if (no_titlebar) window_flags |= ImGuiWindowFlags.NoTitleBar;
                window_flags |= ImGuiWindowFlags.NoScrollbar;
                if (no_move) window_flags |= ImGuiWindowFlags.NoMove;
                if (no_resize) window_flags |= ImGuiWindowFlags.NoResize;
                window_flags |= ImGuiWindowFlags.NoCollapse;
                if (no_mouse) window_flags |= ImGuiWindowFlags.NoMouseInputs;
                if (no_box) window_flags |= ImGuiWindowFlags.NoBackground;

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    config = false;
                }
                ImGui.End();
            }

            if (debug)
            {
                ImGui.Begin("Encounter", ref debug, window_flags);
                ImGui.SetWindowFontScale(scale);
                ImGui.Text("99:99");
                ImGui.SetWindowFontScale(1f);
                ImGui.End();

                ImGui.Begin("Countdown", ref debug, window_flags);
                ImGui.SetWindowFontScale(scale);
                ImGui.Text("99:9.99");
                ImGui.SetWindowFontScale(1f);
                ImGui.End();
            }

            if (enabled && countDown !=0)
            {
                if (Marshal.PtrToStructure<float>((IntPtr)countDown + 0x2c) == lastTime) { pauseCheck++; }
                else { pauseCheck = 0; flipSwitch = true; }

                if (pauseCheck > 50) { flipSwitch = false; }

                if (Marshal.PtrToStructure<float>((IntPtr)countDown + 0x2c) > 0 && flipSwitch)
                {
                    ImGui.Begin("Countdown", ref enabled, window_flags);
                    ImGui.SetWindowFontScale(scale);
                    ImGui.Text(String.Format("{00:0.00}", Marshal.PtrToStructure<float>((IntPtr)countDown + 0x2c)));
                    ImGui.SetWindowFontScale(1f);
                    ImGui.End();
                    CDEnd = DateTime.Now;
                }

                if(Marshal.PtrToStructure<float>((IntPtr)countDown + 0x2c) <= 0 && (DateTime.Now - CDEnd).TotalSeconds < 3 && flipSwitch)
                {
                    ImGui.Begin("Countdown", ref enabled, window_flags);
                    ImGui.SetWindowFontScale(scale);
                    ImGui.Text("FIGHT");
                    ImGui.SetWindowFontScale(1f);
                    ImGui.End();
                }
                lastTime = Marshal.PtrToStructure<float>((IntPtr)countDown + 0x2c);
            }

            if (enableEnc)
            {

                if (pluginInterface.ClientState.Condition[Dalamud.Game.ClientState.ConditionFlag.InCombat])
                {
                    if (start)
                    {
                        start = false;
                        EncStart = DateTime.Now;
                    }
                    EncEnd = DateTime.Now;
                }
                else
                {
                    start = true;
                }

                if (enableCDEnc && Math.Abs((DateTime.Now - CDEnd).TotalSeconds - (DateTime.Now - EncStart).TotalSeconds) >= 10)
                {
                    return;
                }

                if ((DateTime.Now - EncEnd).TotalSeconds <= 10)
                {
                    TimeSpan diff = EncEnd - EncStart;

                    ImGui.Begin("Encounter", ref enableEnc, window_flags);
                    ImGui.SetWindowFontScale(scale);
                    ImGui.Text(diff.ToString(@"mm\:ss"));
                    ImGui.SetWindowFontScale(1f);
                    ImGui.End();
                }
            }



        }

        public void SaveConfig()
        {
            Configuration.Enabled = enabled;
            Configuration.EnabledENC = enableEnc;
            Configuration.EnabledCDENC = enableCDEnc;
            Configuration.Scale = scale;
            Configuration.No_Titlebar = no_titlebar;
            Configuration.No_Move = no_move;
            Configuration.No_Resize = no_resize;
            Configuration.No_Mouse = no_mouse;
            Configuration.No_Box = no_box;
            this.pluginInterface.SavePluginConfig(Configuration);
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = true;
        public bool EnabledENC { get; set; } = false;
        public bool EnabledCDENC { get; set; } = false;
        public float? Scale { get; set; } = 1;
        public bool No_Titlebar { get; set; } = true;
        public bool No_Move { get; set; } = false;
        public bool No_Resize { get; set; } = false;
        public bool No_Mouse { get; set; } = false;
        public bool No_Box { get; set; } = false;
        public Num.Vector4 Colour { get; set; } = new Num.Vector4(1.0f, 1.0f, 1.0f, 1.0f);

    }
}
