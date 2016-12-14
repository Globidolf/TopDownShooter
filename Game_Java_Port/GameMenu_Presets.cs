﻿using Game_Java_Port.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static Game_Java_Port.AttributeBase;
using static Game_Java_Port.GameStatus;

namespace Game_Java_Port {
    partial class GameMenu {


        public static GameMenu GraphicsMenu {
            get {
                string name = "GraphicsMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;
                    Button toggleScreen = null;
                    toggleScreen = new Button(temp, Program.form.IsFullscreen ? "Go Windowed" : "Go Fullscreen", (args) =>
                    {
                        if(checkargs(args)) {
                            Program.form.IsFullscreen ^= true;
                            if(Program.form.IsFullscreen) {
                                temp._data["windowpos"] = Program.form.DesktopLocation;
                                temp._data["windowres"] = Program.form.Size;
                                Program.form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                                Program.form.DesktopLocation = System.Drawing.Point.Empty;
                                Program.form.Size = System.Windows.Forms.Screen.FromHandle(Program.form.Handle).Bounds.Size;

                            } else {
                                Program.form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
                                if(temp._data.ContainsKey("windowpos")) {
                                    Program.form.DesktopLocation = (System.Drawing.Point)temp._data["windowpos"];
                                    Program.form.Size = (System.Drawing.Size)temp._data["windowres"];
                                }
                            }

                            Program.formResized();
                            toggleScreen.Label = Program.form.IsFullscreen ? "Go Windowed" : "Go Fullscreen";
                        }
                    });



                    new Button(temp, "Back", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            SettingsMenu.open();
                        }
                    });

                    temp.resizeNumbers();
                    temp.resizeStrings();

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu SettingsMenu {
            get {
                string name = "SettingsMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;

                    new Button(temp, "Graphics", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            GraphicsMenu.open();
                        }
                    });

                    new Button(temp, "Back to Main Menu", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            MainMenu.open();
                        }
                    });

                    temp.resizeNumbers();
                    temp.resizeStrings();

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu HostMenu {
            get {
                string name = "HostMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;
                    new RegulatorButtons<int>(new Regulator<int>(temp, "Port", 65500, 65535, 65500), 1);
                    new Button(temp, "New Game", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            CharCreatorMenu.onContinue = async () =>
                            {
                                Game.instance.Host((int)temp.getRegulatorValue<int>("Port"));
                                NPC player = new NPC(CharCreatorMenu.getInputValue("Name"),
                                    (uint)CharCreatorMenu._data["Vitality"],
                                    (uint)CharCreatorMenu._data["Strength"],
                                    (uint)CharCreatorMenu._data["Dexterity"],
                                    (uint)CharCreatorMenu._data["Agility"],
                                    (uint)CharCreatorMenu._data["Intelligence"],
                                    (uint)CharCreatorMenu._data["Wisdom"],
                                    (uint)CharCreatorMenu._data["Luck"],
                                    false);
                                await Game.instance._client.awaitInit().ContinueWith((task) =>
                                {
                                    Game.instance._client.send(GameClient.CommandType.sendPlayer, player.serialize());
                                });

                            };
                            CharCreatorMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Load Game", (args) => {
                        if(checkargs(args)) {
                            temp.close();
#warning load and host game
                        }
                    }, _log);
                    new Button(temp, "Cancel", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            MainMenu.open();
                        }
                    }, _log);

                    temp.resizeStrings();
                    temp.resizeNumbers();

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }
        public static GameMenu JoinMenu {
            get {
                string name = "JoinMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;
                    temp.addInput("Adress", "localhost");
                    new RegulatorButtons<int>(new Regulator<int>(temp, "Port", 65500, 65535, 65500), 1);
                    new Button(temp, "Ok", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            CharCreatorMenu.onContinue = async () =>
                            {
                                Game.instance.addMessage("Attempting to connect on " + temp.getInputValue("Adress") + ":" + (int)temp.getRegulatorValue<int>("Port") + "...");
                                try {
                                    Game.instance.Connect(temp.getInputValue("Adress"), (int)temp.getRegulatorValue<int>("Port"));
                                    NPC player = new NPC(CharCreatorMenu.getInputValue("Name"),
                                        (uint)CharCreatorMenu._data["Vitality"],
                                        (uint)CharCreatorMenu._data["Strength"],
                                        (uint)CharCreatorMenu._data["Dexterity"],
                                        (uint)CharCreatorMenu._data["Agility"],
                                        (uint)CharCreatorMenu._data["Intelligence"],
                                        (uint)CharCreatorMenu._data["Wisdom"],
                                        (uint)CharCreatorMenu._data["Luck"],
                                        false);
                                    await Game.instance._client.awaitInit().ContinueWith((task) =>
                                    {
                                        player.ID = GetFirstFreeID;
                                        Game.instance._client.send(GameClient.CommandType.sendPlayer, player.serialize());
                                    });
                                } catch(Exception e) {
                                    Game.instance.addMessage("Connection failed: " + e.Message);
                                    MainMenu.open();
                                }
                            };
                            CharCreatorMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Load Game", (args) => {
                        if(checkargs(args)) {
                            temp.close();
#warning load and host game
                        }
                    }, _log);
                    new Button(temp, "Cancel", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            MainMenu.open();
                        }
                    }, _log);

                    temp.resizeStrings();
                    temp.resizeNumbers();

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu MainMenu {
            get {
                string name = "MainMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;
                    new Button(temp, "New Game", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            CharCreatorMenu.onContinue = () =>
                            {
                                // create and start when done
                                NPC player = new NPC(CharCreatorMenu.getInputValue("Name"),
                                    (uint)CharCreatorMenu._data["Vitality"],
                                    (uint)CharCreatorMenu._data["Strength"],
                                    (uint)CharCreatorMenu._data["Dexterity"],
                                    (uint)CharCreatorMenu._data["Agility"],
                                    (uint)CharCreatorMenu._data["Intelligence"],
                                    (uint)CharCreatorMenu._data["Wisdom"],
                                    (uint)CharCreatorMenu._data["Luck"]);
                                player.AI = AI_Library.RealPlayer;
                                Game.instance._player = player;
                            };
                            CharCreatorMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Load Game", (args) => {
                        if(checkargs(args)) {
                            temp.close();
#warning load and start game
                        }
                    }, _log);
                    new Button(temp, "Host Game", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            HostMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Join Game", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            JoinMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Settings", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            SettingsMenu.open();
                        }
                    }, _log);
                    new Button(temp, "Exit", (args) => {
                        if(checkargs(args)) {
                            exit();
                        }
                    }, _log);

                    temp.resizeStrings();

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu PauseMenu {
            get {
                string name = "PauseMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;

                    new Button(temp, "Continue", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                        }
                    });

                    new Button(temp, "Levelup", (args) =>
                    {
                        if(checkargs(args)) {
                            LevelUpMenu.open();
                            temp.close();
                        }
                    });

                    new Button(temp, "Exit to Menu", (args) => {
                        if(checkargs(args)) {
                            temp.close();
                            reset();
                        }
                    });
                    new Button(temp, "Exit to Windows", (args) => {
                        if(checkargs(args)) {
                            exit();
                        }
                    });
                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu LevelUpMenu {
            get {
                string name = "LevelUpMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;


                    List<Regulator<uint>> attrs = new List<Regulator<uint>>();

                    uint Vit;
                    uint Str;
                    uint Dex;
                    uint Agi;
                    uint Int;
                    uint Wis;
                    uint Luc;
                    uint points = 0;
                    uint sum = 0;
                    Text info = new Text(temp, "");
                    info.Lines = 7;

                    Action onchangedone = () =>
                    {
                        sum = points = (uint)temp._data["points"];
                        // subtract spent points
                        attrs.ForEach((reg) =>
                        {
                            sum -= reg.Value - reg.MinValue;
                            temp._data[reg.Label] = reg.Value;
                        });
                        //change maximum of each regulator based on spent points
                        attrs.ForEach((reg) =>
                        {
                            reg.MaxValue = reg.Value + sum;
                        });

                        Vit = (uint)temp._data["Vitality"];
                        Str = (uint)temp._data["Strength"];
                        Dex = (uint)temp._data["Dexterity"];
                        Agi = (uint)temp._data["Agility"];
                        Int = (uint)temp._data["Intelligence"];
                        Wis = (uint)temp._data["Wisdom"];
                        Luc = (uint)temp._data["Luck"];

                        info.Value =
                        "Precision: " + getPRCMult(Dex, Agi).ToString("0.#%") + "\n" +
                        "Melee Damage: " + getMDMGMult(Str, Dex).ToString("0.#%") + "\n" +
                        "Movement: " + getSPDMult(Str, Dex, Agi).ToString("0.##") + "\n" +
                        "Health: " + (BaseHealth * getHPMult(Vit, Str, Dex, Agi, Int, Wis, Luc)).ToString("0.##") + "\n" +
                        "Ranged Damage: " + getRDMGMult(Dex, Agi).ToString("0.#%") + "\n" +
                        "MeleeSpeed: " + getMSPDMult(Str, Dex, Agi).ToString("0.#%") + "\n" +
                        "Points: " + sum;

                        temp.resizeStrings();
                        temp.resizeNumbers();
                    };

                    temp.onOpen = () => {
                        if(Game.instance._player != null) {
                            sum = points = Game.instance._player.AttributePoints;
                            temp._data["points"] = sum;
                            temp._data["sum"] = sum;
                            attrs.ForEach((reg) =>
                            {
                                reg.MaxValue = Game.instance._player.Attributes[(Attribute)Enum.Parse(typeof(Attribute), reg.Label)];
                                reg.Value = reg.MaxValue;
                                reg.MinValue = reg.Value;
                            });

                            onchangedone?.Invoke();
                        }
                    };

                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Vitality",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Strength",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Dexterity",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Agility",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Intelligence",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Wisdom",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Luck",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);

                    lock(temp.Elements)
                        temp.Elements.ForEach((element) =>
                    {
                        if(element is Regulator<uint>) {
                            attrs.Add((Regulator<uint>)element);
                            temp._data[element.Label] = ((Regulator<uint>)element).Value;
                        }
                    });

                    Vit = (uint)temp._data["Vitality"];
                    Str = (uint)temp._data["Strength"];
                    Dex = (uint)temp._data["Dexterity"];
                    Agi = (uint)temp._data["Agility"];
                    Int = (uint)temp._data["Intelligence"];
                    Wis = (uint)temp._data["Wisdom"];
                    Luc = (uint)temp._data["Luck"];

                    new Button(temp, "Ok", (args) =>
                    {
                        if(checkargs(args)) {
                            attrs.ForEach((reg) =>
                            {
                                Game.instance._player.Attributes[(Attribute)Enum.Parse(typeof(Attribute), reg.Label)] = reg.Value;
                                Game.instance._player.AttributePoints = sum;
                            });
                            temp.close();
                        }
                    });
                    new Button(temp, "Cancel", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                        }
                    });

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }
        public static GameMenu CharCreatorMenu {
            get {
                string name = "CharCreatorMenu";

                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;


                    List<Regulator<uint>> attrs = new List<Regulator<uint>>();

                    uint Vit;
                    uint Str;
                    uint Dex;
                    uint Agi;
                    uint Int;
                    uint Wis;
                    uint Luc;
                    uint points = 0;
                    uint sum = 0;

                    temp.addInput("Name", NameGen.RandomName);


                    Text info = new Text(temp, "");

                    info.Lines = 7;



                    Action onchangedone = () =>
                    {
                        sum = points = (uint)temp._data["points"];
                        // subtract spent points
                        attrs.ForEach((reg) =>
                        {
                            sum -= reg.Value - reg.MinValue;
                            temp._data[reg.Label] = reg.Value;
                        });
                        //change maximum of each regulator based on spent points
                        attrs.ForEach((reg) =>
                        {
                            reg.MaxValue = reg.Value + sum;
                        });

                        Vit = (uint)temp._data["Vitality"];
                        Str = (uint)temp._data["Strength"];
                        Dex = (uint)temp._data["Dexterity"];
                        Agi = (uint)temp._data["Agility"];
                        Int = (uint)temp._data["Intelligence"];
                        Wis = (uint)temp._data["Wisdom"];
                        Luc = (uint)temp._data["Luck"];

                        info.Value =
                        "Precision: " + getPRCMult(Dex, Agi).ToString("0.#%") + "\n" +
                        "Melee Damage: " + getMDMGMult(Str, Dex).ToString("0.#%") + "\n" +
                        "Movement: " + getSPDMult(Str, Dex, Agi).ToString("0.##") + "\n" +
                        "Health: " + (BaseHealth * getHPMult(Vit, Str, Dex, Agi, Int, Wis, Luc)).ToString("0.##") + "\n" +
                        "Ranged Damage: " + getRDMGMult(Dex, Agi).ToString("0.#%") + "\n" +
                        "MeleeSpeed: " + getMSPDMult(Str, Dex, Agi).ToString("0.#%") + "\n" +
                        "Points: " + sum;
                        ;

                        temp.resizeStrings();
                        temp.resizeNumbers();
                    };

                    temp.onOpen = () =>
                    {
                        sum = points = BaseAttributePoints;
                        temp._data["points"] = sum;
                        temp._data["sum"] = sum;
                        attrs.ForEach((reg) =>
                        {
                            reg.Value = BaseAttributeValue;
                            reg.MinValue = reg.Value;
                        });

                        onchangedone?.Invoke();
                    };

                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Vitality",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Strength",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Dexterity",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Agility",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Intelligence",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Wisdom",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);
                    new RegulatorButtons<uint>(new Regulator<uint>(temp, "Luck",
                        1,
                        BaseAttributePoints,
                        BaseAttributeValue, onChange: onchangedone), 1);

                    lock(temp.Elements)
                        temp.Elements.ForEach((element) =>
                    {
                        if(element is Regulator<uint>) {
                            attrs.Add((Regulator<uint>)element);
                            temp._data[element.Label] = ((Regulator<uint>)element).Value;
                        }
                    });

                    Vit = (uint)temp._data["Vitality"];
                    Str = (uint)temp._data["Strength"];
                    Dex = (uint)temp._data["Dexterity"];
                    Agi = (uint)temp._data["Agility"];
                    Int = (uint)temp._data["Intelligence"];
                    Wis = (uint)temp._data["Wisdom"];
                    Luc = (uint)temp._data["Luck"];

                    new Button(temp, "Ok", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            temp.onContinue?.Invoke();
                            if(Game.instance._player != null)
                                Game.instance._player.AttributePoints = 0;
                        }
                    });
                    new Button(temp, "Back", (args) =>
                    {
                        if(checkargs(args)) {
                            temp.close();
                            MainMenu.open();
                        }
                    });

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }

        public static GameMenu InventoryMenu {
            get {
                string name = "InventoryMenu";
                if(!hasMenu(name)) {
                    GameMenu temp = new GameMenu();
                    temp.Name = name;

                    temp.onOpen += () =>
                    {
                        //clear all onclick events
                        lock(temp.Elements)
                            temp.Elements.ForEach((ele) =>
                        {
                            if (ele is Button) {
                                ((Button)ele).Dispose();
                            } else if (ele is MenuElementListBase) {
                                ((MenuElementListBase)ele).Children.ForEach((chl) =>
                                {
                                    if (chl is Button) {
                                        ((Button)chl).Dispose();
                                    }
                                });
                            }
                        });
                        //clear list
                        lock(temp.Elements)
                            temp.Elements.Clear();
                        new Button(temp, "Close Menu", (args) => {
                            if(checkargs(args)) {
                                temp.close();
                            }
                        });
                        if (Game.instance._player != null)
                            foreach (ItemBase item in Game.instance._player.Inventory) {
                                new ItemButton(temp, item);
                            }
                    };

                    

                    addMenu(temp);
                }
                return getMenu(name);
            }
        }
    }
}
