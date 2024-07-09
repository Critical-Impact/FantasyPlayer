using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace FantasyPlayer.Config
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Newtonsoft.Json;

    public class Configuration : IPluginConfiguration, INotifyPropertyChanged
    {
        private bool isDirty;
        public bool IsDirty
        {
            get => isDirty || PlayerSettings.IsDirty || SpotifySettings.IsDirty || AutoPlaySettings.IsDirty;
            set => SetField(ref isDirty, value, false);
        }

        public void MarkClean()
        {
            IsDirty = false;
            PlayerSettings.MarkClean();
            SpotifySettings.MarkClean();
            AutoPlaySettings.MarkClean();
        }
        private bool displayChatMessages;
        private bool configShown;
        public int Version { get; set; } = 0;

        public PlayerSettings PlayerSettings { get; set; } = new PlayerSettings();
        public SpotifySettings SpotifySettings { get; set; } = new SpotifySettings();
        public AutoPlaySettings AutoPlaySettings { get; set; } = new AutoPlaySettings();

        public bool DisplayChatMessages
        {
            get => displayChatMessages;
            set => SetField(ref displayChatMessages, value);
        }

        [JsonIgnore]
        public bool ConfigShown
        {
            get => configShown;
            set => SetField(ref configShown, value);
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