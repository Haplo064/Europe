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
        public bool config = false;

        //void FUN_140298840(longlong param_1)
        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr UnknownFunc1(ulong param_1);
        private UnknownFunc1 ukFunc1;
        private Hook<UnknownFunc1> ukFuncHook1;
        public IntPtr funcPtr1;

        public ulong countDown = 0;

        public Num.Vector4 colour;
        public bool check = true;


        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();


            this.pluginInterface.UiBuilder.OnBuildUi += DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += ConfigWindow;
            this.pluginInterface.CommandManager.AddHandler("/ctd", new CommandInfo(Command)
            {
                HelpMessage = ""
            });

            try
            { enabled = Configuration.Enabled; }
            catch (Exception)
            {
                PluginLog.LogError("Failed to set Enabled");
                enabled = true;
            }



            funcPtr1 = pluginInterface.TargetModuleScanner.Module.BaseAddress + 0x298840;
            ukFunc1 = new UnknownFunc1(ukFuncFunc1);
            try
            {ukFuncHook1 = new Hook<UnknownFunc1>(funcPtr1, ukFunc1, this); ukFuncHook1.Enable();}
            catch(Exception e)
            {PluginLog.Log("BAD\n"+e.ToString());}


        }

        public IntPtr ukFuncFunc1(ulong param_1)
        {

            countDown = param_1;
            return ukFuncHook1.Original(param_1);
        }

        public void Command(string command, string arguments)
        {
            config = true;
        }

        public void Dispose()
        {
            this.pluginInterface.UiBuilder.OnBuildUi -= DrawWindow;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= ConfigWindow;
        }

        private void ConfigWindow(object Sender, EventArgs args)
        {
            config = true;
        }

        private void DrawWindow()
        {
            if (config)
            {
                ImGui.SetNextWindowSize(new Num.Vector2(300, 500), ImGuiCond.FirstUseEver);
                ImGui.Begin("Countdown Config", ref config);
                ImGui.Checkbox("Enable", ref enabled);

                /*
                ImGui.Text(countDown.ToString("X"));
                if(countDown != 0)
                {
                    ImGui.Text(Marshal.PtrToStructure<float>((IntPtr)countDown + 0x20).ToString());
                    ImGui.Text(Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24).ToString());
                }
                */

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    config = false;
                }
                ImGui.End();
            }

            if (enabled)
            {
                if (countDown != 0 && Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24) > 0)
                {
                    ImGui.SetNextWindowSize(new Num.Vector2(50, 25));
                    ImGui.Begin("Countdown Display", ref config, ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoResize);
                    ImGui.Text(String.Format("{0:0.00}", Marshal.PtrToStructure<float>((IntPtr)countDown + 0x24)));
                    ImGui.End();
                }
                
            }

        }

        public void SaveConfig()
        {
            Configuration.Enabled = enabled;
            this.pluginInterface.SavePluginConfig(Configuration);
        }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public bool Enabled { get; set; } = false;
        public Num.Vector4 Colour { get; set; } = new Num.Vector4(1.0f, 1.0f, 1.0f, 1.0f);

    }


}
