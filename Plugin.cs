using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Reflection;
using Dalamud.Interface.Windowing;
using NKPlugin.Windows;
using Dalamud.Game.ClientState.Objects.SubKinds;
using System.Runtime.InteropServices;
using System;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState;
using Dalamud.Game;
using Dalamud.Data;
using Action = Lumina.Excel.GeneratedSheets.Action;
using FFXIVClientStructs.FFXIV.Client.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using System.Diagnostics;
using System.ComponentModel;
using Dalamud.Game.ClientState.Objects.Types;

namespace NKPlugin
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Ninja KT Plugin";
        private const string CommandName = "/nkp";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        [PluginService] public TargetManager TargetManaget { get; init; } = null!;
        public Configuration Configuration { get; init; }
        [PluginService] public ClientState clientState { get; init; } = null!;
        [PluginService] public static ChatGui chatGui { get; private set; } = null!;
        public WindowSystem WindowSystem = new("NKPlugin");
        [PluginService]
        internal ObjectTable objectTable { get; init; } = null!;
        [PluginService]
        internal Framework Framework { get; init; } = null!;
        [PluginService]
        internal static SigScanner Scanner{get; private set;}
        //internal PluginAddressResolver Address{get; set; } = null!;
        [PluginService]
        internal static DataManager DataManager { get; private set; } = null!;
        internal static ActionManager ActionManager { get; private set; }

        DateTime LastSelectTime;

        uint xdtz = (uint)29515; //星遁天诛
        //uint xdtz = (uint)29067; //圣盾阵
        Action _action => DataManager.GetExcelSheet<Action>().GetRow(xdtz);
        uint ID => _action.RowId;
        //uint ID = 11;

        #region 更新地址
        private CanAttackDelegate CanAttack;
        private delegate int CanAttackDelegate(int arg, IntPtr objectAddress);
        //private const int CanAttackOffset = 0x802840;//Struct121_IntPtr_17
        #endregion

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            chatGui.Print("NKP Init");
            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            // you might normally want to embed resources and load them from the manifest stream
            //var imagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");
            //var goatImage = this.PluginInterface.UiBuilder.LoadImage(imagePath);

            //WindowSystem.AddWindow(new ConfigWindow(this));

            this.CommandManager.AddHandler("/nkp", new CommandInfo(OnCommand)
            {
                HelpMessage = "_ ← pkn\\"
            }) ;

            //CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Process.GetCurrentProcess().MainModule.BaseAddress + CanAttackOffset);
            CanAttack = Marshal.GetDelegateForFunctionPointer<CanAttackDelegate>(Scanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B DA 8B F9 E8 ?? ?? ?? ?? 4C 8B C3"));

            this.Framework.Update += this.OnFrameworkUpdate;
            this.PluginInterface.UiBuilder.Draw += DrawUI;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler("/nkp");
            this.Framework.Update -= this.OnFrameworkUpdate;
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            //WindowSystem.GetWindow("MainWindow").IsOpen = true;
            //if (command == "/nkp")
            //{
            //WindowSystem.GetWindow("NKP设置").IsOpen = true;
            //}
            //chatGui.Print(command);
            //chatGui.Print(args);
            if (command == "/nkp" && args == "autoON")
            {
                chatGui.Print("———AUTO ON———");
                Configuration.AutoSelect = true;
            }
            if (command == "/nkp" && args == "autoOFF")
            {
                chatGui.Print("———AUTO OFF———");
                Configuration.AutoSelect = false;
            }

            //if (command == "/nkp" && args == "KON")
            //{
            //    chatGui.Print("———K ON———");
            //    Configuration.K = true;
            //}
            //if (command == "/nkp" && args == "KOFF")
            //{
            //    chatGui.Print("———K OFF———");
            //    Configuration.K = false;
            //}
            if (command == "/nkp" && args == "19")
            {
                chatGui.Print("——Distance: 19——");
                Configuration.SelectDistance = 20;
            }
            if (command == "/nkp" && args == "25")
            {
                chatGui.Print("——Distance: 25——");
                Configuration.SelectDistance = 25;
            }
        }
        private void OnFrameworkUpdate(Framework framework)
        {
            //GameObject target = clientState.LocalPlayer.TargetObject;
            //if (target != null) { 
            //    //var distance2D = Math.Sqrt(Math.Pow(target.YalmDistanceX, 2) + Math.Pow(target.YalmDistanceZ, 2)) - 6;
            //    var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - target.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - target.Position.Y, 2)) - 1;
            //    chatGui.Print(distance2D.ToString());
            //}

            if (Configuration.AutoSelect && clientState.IsPvP) {
                this.ReFreshEnermyActors_And_AutoSelect();
            }
        }

        private void SelectTarget(PlayerCharacter chara)
        {
            //if (chara == null)
            //{
            //    return;
            //}
            //if (Marshal.ReadInt32(this.TargetIdPtr) != chara.ObjectId)
            //{
                TargetManaget.SetTarget(chara);
            //Marshal.WriteInt64(TargetPtr, chara.Address.ToInt64());
            //}

        }

        private void ReFreshEnermyActors_And_AutoSelect()
        {
            #region 刷新敌对列表

            //if (Configuration.K)
            //{
            //    try
            //    {
            //        chatGui.Print("K():");
            //        K();
            //    }
            //    catch (Exception) { }
            //}

            if (clientState.LocalPlayer == null)
            {
                return;
            }
            Configuration.LocalPlayer = clientState.LocalPlayer;
            lock (Configuration.EnermyActors)
            {
                Configuration.EnermyActors.Clear();
                if (objectTable == null)
                {
                    return;
                }
                foreach (var obj in objectTable)
                {
                    try
                    {
                        //chatGui.Print(CanAttack(142, obj.Address).ToString());
                        //if (CanAttack(142, obj.Address) == 1) {
                        //    chatGui.Print(obj.Name);
                        //}
                        if ((obj.ObjectId != Configuration.LocalPlayer.ObjectId) & obj.Address.ToInt64() != 0 && CanAttack(142, obj.Address) == 1)
                        {
                            PlayerCharacter rcTemp = obj as PlayerCharacter;
                            //19 骑士   32 DK
                            if (rcTemp.ClassJob.Id != 19 && rcTemp.ClassJob.Id != 32)
                            {
                                //chatGui.Print(obj.Name);
                                Configuration.EnermyActors.Add(rcTemp);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                }
            }
            #endregion

            #region 自动选择
            if (Configuration.AutoSelect)
            {
                DateTime now = DateTime.Now;
                if (LastSelectTime == null || (now - LastSelectTime).TotalMilliseconds > Configuration.SelectInterval)
                {
                    SelectEnermyOnce();
                    LastSelectTime = now;
                }
            }

            #endregion
        }

        private void SelectEnermyOnce()
        {
            if (Configuration.LocalPlayer == null || Configuration.EnermyActors == null)
            {
                return;
            }
            PlayerCharacter selectActor = null;
            //double temp = 0;
            //double temp2 = 0;
            //double temp3 = 0;
            //double temp4 = 0;
            //double temp5 = 0;
            foreach (PlayerCharacter actor in Configuration.EnermyActors)
            {
                try
                {
                    //var distance2D = Math.Sqrt(Math.Pow(clientState.LocalPlayer.Position.X - actor.Position.X, 2) + Math.Pow(clientState.LocalPlayer.Position.Y - actor.Position.Y, 2)) - 1;
                    var distance2D = Math.Sqrt(Math.Pow(actor.YalmDistanceX, 2) + Math.Pow(actor.YalmDistanceZ, 2)) - 6;
                    //if (distance2D <= Configuration.SelectDistance && actor.CurrentHp != 0 && (selectActor == null || actor.CurrentHp < selectActor.CurrentHp))
                    if (distance2D <= Configuration.SelectDistance && actor.CurrentHp != 0 && actor.CurrentHp <= ((actor.MaxHp / 2)))
                    {
                        //temp = distance2D;
                        //temp2 = actor.YalmDistanceX;
                        //temp4 = actor.YalmDistanceZ;
                        //temp4 = actor.Position.Y;
                        //temp5 = actor.Position.Y;
                         selectActor = actor;
                    }
                }
                catch (Exception)
                {
                    continue;
                }
            }
            if (selectActor != null)
            {
                //chatGui.Print(temp.ToString());
                //chatGui.Print("selectActor:" + selectActor.ClassJob.Id);

                SelectTarget(selectActor);

                //chatGui.Print(temp.ToString());
                //chatGui.Print(temp2.ToString());
                //chatGui.Print(temp3.ToString());
                //chatGui.Print(temp4.ToString());
                //chatGui.Print(temp5.ToString());

                //bool KFlag = false;
                //if (selectActor.CurrentHp < ((selectActor.MaxHp / 2) - 2000))
                //{
                //    KFlag = true;
                //}
                //if (KFlag && Configuration.K)
                //{
                //    try
                //    {
                //        K();
                //    }
                //    catch (Exception) { }
                //}
            }
        }

        unsafe private void K()
        {
            ActionManager.Instance()->UseAction(ActionType.Spell, ID, clientState.LocalPlayer.TargetObjectId);
        }

            private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            //WindowSystem.GetWindow("NKP设置").IsOpen = true;
        }
    }
}
