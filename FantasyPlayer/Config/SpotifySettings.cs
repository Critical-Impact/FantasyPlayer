using SpotifyAPI.Web;

namespace FantasyPlayer.Config
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class SpotifySettings : INotifyPropertyChanged
    {
        private PKCETokenResponse tokenResponse;
        private bool limitedAccess;

        private bool isDirty;
        public bool IsDirty
        {
            get => isDirty;
            set => SetField(ref isDirty, value, false);
        }

        public void MarkClean()
        {
            IsDirty = false;
        }

        public PKCETokenResponse TokenResponse
        {
            get => tokenResponse;
            set => SetField(ref tokenResponse, value);
        }

        public bool LimitedAccess
        {
            get => limitedAccess;
            set => SetField(ref limitedAccess, value);
        }

        public SpotifySettings()
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