namespace FantasyPlayer.Config
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Newtonsoft.Json;

    public class AutoPlaySettings : INotifyPropertyChanged
    {
        private bool playInDuty;
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

        public bool PlayInDuty
        {
            get => playInDuty;
            set => SetField(ref playInDuty, value);
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