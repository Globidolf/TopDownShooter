using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game_Java_Port {
    public enum FactionNames {
        Players,
        Lunatics,
        Civilians,
        Savages,
        Environment
    }
    public class Faction {
        public static implicit operator FactionNames(Faction faction) {
            return faction.getFactionName();
        }

        public static implicit operator Faction(FactionNames name) {
            return getFaction(name);
        }

        public string Name { get; }

        private static Dictionary<FactionNames, Faction> _factions { get; } = new Dictionary<FactionNames, Faction>();
        
        public enum Conduct {
            /// <summary>
            /// Attack source when triggered
            /// </summary>
            Attack,
            /// <summary>
            /// Run from source when triggered
            /// </summary>
            Run,
            /// <summary>
            /// Ignore source
            /// </summary>
            Ignore,
            /// <summary>
            /// Follow source when triggered
            /// </summary>
            Follow
        }

        public enum Disposition {
            /// <summary>
            /// Always try to kill
            /// </summary>
            Enemy,
            /// <summary>
            /// Can kill but won't unless provoked
            /// </summary>
            Neutral,
            /// <summary>
            /// Cannot kill
            /// </summary>
            Allied,
            /// <summary>
            /// won't try to provoke, instead flees
            /// </summary>
            Fear
        }

        public Color FactionColor { get; set; } = Color.White;

        private FactionNames getFactionName() {
            return (FactionNames)Enum.Parse(typeof(FactionNames), Name);
        }

        private static Faction getFaction(FactionNames name) {
            switch(name) {
                case FactionNames.Civilians: return Civilians;
                case FactionNames.Environment: return Environment;
                case FactionNames.Lunatics: return Lunatics;
                case FactionNames.Players: return Players;
                case FactionNames.Savages: return Savages;
                default: return null;
            }
        }

        public Dictionary<FactionNames, Disposition> Dispositions { get; } = new Dictionary<FactionNames, Disposition>();

        /// <summary>
        /// Disposition when unspecified
        /// </summary>
        public Disposition DefaultDisposition { get; set; } = Disposition.Neutral;

        /// <summary>
        /// Action when attacked
        /// </summary>
        public Conduct AgressionConduct { get; set; } = Conduct.Ignore;

        public Conduct InteractionConduct { get; set; } = Conduct.Ignore;

        /// <summary>
        /// Attack anything, even themselves
        /// </summary>
        public static Faction Lunatics { get {
                FactionNames name = FactionNames.Lunatics;
                if(!_factions.ContainsKey(name)) {
                    Faction temp = new Faction(name);
                    _factions.Add(name, temp);

                    temp.DefaultDisposition = Disposition.Enemy;

                    temp.AgressionConduct = Conduct.Attack;

                    temp.FactionColor = Color.Tomato;

                }
                return _factions[name];
            } }

        /// <summary>
        /// Liveless objects
        /// </summary>
        public static Faction Environment {
            get {
                FactionNames name = FactionNames.Environment;
                if(!_factions.ContainsKey(name)) {
                    Faction temp = new Faction(name);
                    _factions.Add(name, temp);

                    temp.DefaultDisposition = Disposition.Neutral;

                    temp.FactionColor = Color.DimGray;
                }
                return _factions[name];
            }
        }

        /// <summary>
        /// Run from Enemy Factions
        /// </summary>
        public static Faction Civilians { get {
                FactionNames name = FactionNames.Civilians;
                if(!_factions.ContainsKey(name)) {
                    Faction temp = new Faction(name);
                    _factions.Add(name, temp);

                    temp.DefaultDisposition = Disposition.Fear;

                    temp.Dispositions.Add(Players, Disposition.Neutral);
                    temp.Dispositions.Add(Civilians, Disposition.Allied);
                    temp.Dispositions.Add(Environment, Disposition.Neutral);

                    temp.AgressionConduct = Conduct.Run;

                    temp.InteractionConduct = Conduct.Follow;

                    temp.FactionColor = Color.LightCyan;
                }
                return _factions[name];
            } }

        /// <summary>
        /// Smarter Lunatics (Wont kill each other or destroy the environment unless by accident)
        /// </summary>
        public static Faction Savages { get {
                FactionNames name = FactionNames.Savages;
                if(!_factions.ContainsKey(name)) {
                    Faction temp = new Faction(name);
                    _factions.Add(name, temp);

                    temp.DefaultDisposition = Disposition.Enemy;
                    
                    temp.Dispositions.Add(Savages, Disposition.Neutral);
                    temp.Dispositions.Add(Environment, Disposition.Neutral);

                    temp.AgressionConduct = Conduct.Attack;

                    temp.FactionColor = Color.Red;
                }
                return _factions[name];
            } }


        /// <summary>
        /// Player Faction
        /// </summary>
        public static Faction Players { get {
                FactionNames name = FactionNames.Players;
                if(!_factions.ContainsKey(name)) {
                    Faction temp = new Faction(name);
                    _factions.Add(name, temp);

                    temp.DefaultDisposition = Disposition.Enemy;

                    temp.Dispositions.Add(Players, Disposition.Allied);
                    temp.Dispositions.Add(Civilians, Disposition.Neutral);

                    temp.FactionColor = Color.Blue;
                }
                return _factions[name];
            } }
        



        //nope
        private Faction(FactionNames name) {
            Name = name.ToString();
        }

        public override string ToString() {
            return Name;
        }
    }
}
