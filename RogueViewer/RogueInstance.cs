using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace RogueViewer
{
    public class RogueInstance : INotifyPropertyChanged
    {
        private string _mapText = "";
        public string MapText
        {
            get => _mapText;
            set
            {
                if (_mapText != value)
                {
                    _mapText = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Level { get; set; } = "Level 0";
        public TimeSpan Runtime { get; set; } = TimeSpan.Zero;
        public int ProcessId { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
