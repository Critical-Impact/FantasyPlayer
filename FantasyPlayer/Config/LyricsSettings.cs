using Dalamud.Game.Text;

namespace FantasyPlayer.Config
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public enum LyricDisplayMode
    {
        Chat = 0,
        FlyText = 1,
    }

    public class LyricsSettings : INotifyPropertyChanged
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

        private bool enableLyrics;
        public bool EnableLyrics
        {
            get => enableLyrics;
            set => SetField(ref enableLyrics, value);
        }

        private LyricDisplayMode displayMode = LyricDisplayMode.Chat;
        public LyricDisplayMode DisplayMode
        {
            get => displayMode;
            set => SetField(ref displayMode, value);
        }

        private XivChatType chatType = XivChatType.Echo;
        public XivChatType ChatType
        {
            get => chatType;
            set => SetField(ref chatType, value);
        }

        private uint flyTextColor = 0xFF60D71E;
        public uint FlyTextColor
        {
            get => flyTextColor;
            set => SetField(ref flyTextColor, value);
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
