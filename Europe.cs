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

        static bool no_titlebar = true;
        static bool no_scrollbar = true;
        static bool no_move = false;
        static bool no_resize = false;
        static bool no_mouse = false;


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

        //__int64 __fastcall sub_140B2EB40(__int64 a1, __int64 a2)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr UpdateStateFlags(Int64 arrayLocation, Int64 bitset);
        private UpdateStateFlags updateStateFlags;
        private Hook<UpdateStateFlags> updateStateFlagsHook;
        public IntPtr updateStateFlagsPtr;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            stateFlags = IntPtr.Zero;
            stateFlagsArray = new bool[100];

            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();

            updateStateFlagsPtr = pluginInterface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 6C 24 ?? 57 41 54 41 55 41 56 41 57 48 83 EC 20 4C 8B EA");
            updateStateFlags = new UpdateStateFlags(updateStateFlagsFunc);
            try
            { updateStateFlagsHook = new Hook<UpdateStateFlags>(updateStateFlagsPtr, updateStateFlags, this); updateStateFlagsHook.Enable(); }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e.ToString()); }

            this.pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            this.pluginInterface.CommandManager.AddHandler("/ctd", new CommandInfo(Command)
            {
                HelpMessage = ""
            });

            enabled = Configuration.Enabled;
            enableEnc = Configuration.EnabledENC;
            scale = Configuration.Scale.Value;
            enableCDEnc = Configuration.EnabledCDENC;
            no_titlebar = Configuration.No_Titlebar;
            no_move = Configuration.No_Move;
            no_resize = Configuration.No_Resize;
            no_mouse = Configuration.No_Mouse;



            countdownPTR = pluginInterface.TargetModuleScanner.ScanText("??  89 ??  ??  ?? 57 48 83 ??  ?? 8B ??  ?? 48 8B ??  ?? 41 ?? 48 8B");
            //funcPtr1 = pluginInterface.TargetModuleScanner.Module.BaseAddress + 0x298840;
            countdownTimer = new CountdownTimer(countdownTimerFunc);
            try
            {countdownTimerHook = new Hook<CountdownTimer>(countdownPTR, countdownTimer, this); countdownTimerHook.Enable();}
            catch(Exception e)
            {PluginLog.Log("BAD\n"+e.ToString());}


        }

        public IntPtr updateStateFlagsFunc(Int64 arrayLocation, Int64 bitset)
        {
            stateFlags = (System.IntPtr)arrayLocation;
            return updateStateFlagsHook.Original(arrayLocation, bitset);
        }

        public void UpdateStateFlagValues()
        {
            for (int i = 0; i < 100; i++)
            {
                if (stateFlags != IntPtr.Zero)
                {
                    if (Marshal.ReadByte(stateFlags + i) == 0)
                    {
                        stateFlagsArray[i] = false;
                    }
                    else
                    {
                        stateFlagsArray[i] = true;
                    }
                }
            }
        }

        public IntPtr countdownTimerFunc(ulong param_1)
        {

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
            countdownTimerHook.Disable();
            updateStateFlagsHook.Disable();
        }

        private void ConfigWindow(object Sender, EventArgs args)
        {
            config = true;
        }

        private void DrawWindow()
        {
            UpdateStateFlagValues();

            ImGuiWindowFlags window_flags = 0;
            if (no_titlebar) window_flags |= ImGuiWindowFlags.NoTitleBar;
            if (no_scrollbar) window_flags |= ImGuiWindowFlags.NoScrollbar;
            if (no_move) window_flags |= ImGuiWindowFlags.NoMove;
            if (no_resize) window_flags |= ImGuiWindowFlags.NoResize;
            if (no_mouse) window_flags |= ImGuiWindowFlags.NoMouseInputs;

            if (config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(300, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Accurate Countdown Config", ref config);
                ImGui.Checkbox("Enable Countdown", ref enabled);
                ImGui.Checkbox("Enable EncounterTimer", ref enableEnc);
                ImGui.Checkbox("Enable EncounterTimer for CD only", ref enableCDEnc);
                ImGui.Checkbox("No TitleBar", ref no_titlebar);
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

            if (enabled)
            {
                if (countDown != 0)
                {
                    if (Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24) > 0)
                    {
                        ImGui.Begin("Countdown", ref enabled, window_flags);
                        ImGui.SetWindowFontScale(scale);
                        ImGui.Text(String.Format("{00:0.00}", Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24)));
                        ImGui.SetWindowFontScale(1f);
                        ImGui.End();
                        CDEnd = DateTime.Now;
                    }
                    if(Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24) <= 0 && (DateTime.Now - CDEnd).TotalSeconds < 3)
                    {
                        ImGui.Begin("Countdown", ref enabled, window_flags);
                        ImGui.SetWindowFontScale(scale);
                        ImGui.Text("FIGHT");
                        ImGui.SetWindowFontScale(1f);
                        ImGui.End();
                    }
                }               
            }
            if (enableEnc)
            {

                if (stateFlagsArray[(int)StateFlag.combat])
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
        public Num.Vector4 Colour { get; set; } = new Num.Vector4(1.0f, 1.0f, 1.0f, 1.0f);

    }

    enum StateFlag
    {
        empty,

        normal,
        unconscious,
        emote,
        mounted,
        crafting,
        gathering,
        melding,
        sieging,
        carrying,
        mounted2,

        badPosition,
        racing,
        miniGame,
        verminion,
        customMatch,
        performing,
        unknown,
        unknown2,
        unknown3,
        unknown4,

        unknown5,
        unknown6,
        unknown7,
        unknown8,
        occupied,
        combat,
        casting,
        status,
        status2,
        occupied2,

        occupied3,
        occupied4,
        occupied5,
        duty,
        occupied6,
        dueling,
        trading,
        occupied7,
        occupied8,
        crafting2,

        craftprep,
        gathering2,
        fishing,
        unknown9,
        betweenAreas,
        stealthed,
        unknown10,
        jumping,
        autoRunning,
        occupied9,

        betweenAreas2,
        systemError,
        loggingOut,
        atLocation,
        waitingForDuty,
        boundByDuty,
        time,
        cutscene,
        waitingDutyFinder,
        creatingCharacter,

        jumping2,
        pvpDisplay,
        status3,
        mounting,
        carrying2,
        partyFinding,
        housing,
        transformed,
        freeTrial,
        beingMoved,

        mounting3,
        status4,
        status5,
        registeringMatch,
        waitingMatch,
        waitingTTMatch,
        flying,
        cutscene2,
        deepDungeon,
        swimming,

        diving,
        registeringTTMatch,
        waitingTTMatch2,
        cwp,
        unknown11,
        dutyRecord,
        casting2,
        state,
        state2,
        rolePlaying,

        boundByDuty2,
        readyingAnotherWorld,
        waitingAnotherWorld,
        parasol,
        boundbyDuty3,
        unknown12,
        unknown13,
        unknown14,
        unknown15

    }
}
