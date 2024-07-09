using System.Numerics;
using Dalamud.Game.Text;

namespace FantasyPlayer.Config
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class PlayerSettings : INotifyPropertyChanged
    {
        private bool isDirty;
        public bool IsDirty
        {
            get => isDirty;
            private set => SetField(ref isDirty, value, false);
        }

        public void MarkClean()
        {
            IsDirty = false;
        }

        private Vector4 accentColor = Interface.InterfaceUtils.FantasyPlayerColor;
        public Vector4 AccentColor
        {
            get => accentColor;
            set => SetField(ref accentColor, value);
        }

        private float transparency = 1f;
        public float Transparency
        {
            get => transparency;
            set => SetField(ref transparency, value);
        }

        private bool playerWindowShown = true;
        public bool PlayerWindowShown
        {
            get => playerWindowShown;
            set => SetField(ref playerWindowShown, value);
        }

        private string defaultProvider;
        public string DefaultProvider
        {
            get => defaultProvider;
            set => SetField(ref defaultProvider, value);
        }

        private bool compactPlayer;
        public bool CompactPlayer
        {
            get => compactPlayer;
            set => SetField(ref compactPlayer, value);
        }

        private bool noButtons;
        public bool NoButtons
        {
            get => noButtons;
            set => SetField(ref noButtons, value);
        }

        private bool firstRunNone = true;
        public bool FirstRunNone
        {
            get => firstRunNone;
            set => SetField(ref firstRunNone, value);
        }

        private bool firstRunCompactPlayer;
        public bool FirstRunCompactPlayer
        {
            get => firstRunCompactPlayer;
            set => SetField(ref firstRunCompactPlayer, value);
        }

        private bool firstRunSetNoButtons;
        public bool FirstRunSetNoButtons
        {
            get => firstRunSetNoButtons;
            set => SetField(ref firstRunSetNoButtons, value);
        }

        private bool disableInput;
        public bool DisableInput
        {
            get => disableInput;
            set => SetField(ref disableInput, value);
        }

        private bool playerLocked;
        public bool PlayerLocked
        {
            get => playerLocked;
            set => SetField(ref playerLocked, value);
        }

        private bool debugWindowOpen;
        public bool DebugWindowOpen
        {
            get => debugWindowOpen;
            set => SetField(ref debugWindowOpen, value);
        }

        private bool showTimeElapsed;
        public bool ShowTimeElapsed
        {
            get => showTimeElapsed;
            set => SetField(ref showTimeElapsed, value);
        }

        private bool onlyOpenWhenLoggedIn = true;
        public bool OnlyOpenWhenLoggedIn
        {
            get => onlyOpenWhenLoggedIn;
            set => SetField(ref onlyOpenWhenLoggedIn, value);
        }

        private XivChatType chatType = XivChatType.Echo;
        public XivChatType ChatType
        {
            get => chatType;
            set => SetField(ref chatType, value);
        }

        public PlayerSettings()
        {
            
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, bool markDirty = true, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            if (markDirty)
            {
                IsDirty = true;
            }

            return true;
        }
    }
}