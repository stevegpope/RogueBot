using RogueBot;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

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

        private string _level = "";
        public string Level
        {
            get => _level;
            set
            {
                if (_level != value)
                {
                    _level = value;
                    OnPropertyChanged();
                }
            }
        }

        private TimeSpan _runTime = TimeSpan.Zero;

        public TimeSpan Runtime
        {
            get => _runTime;
            set
            {
                if (_runTime != value)
                {
                    _runTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProcessId { get; set; } = 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        internal void SendKey(string key)
        {
            var process = Process.GetProcessById(ProcessId);
            if (process == null) return;

            var console = new ConsoleController(process);
            console.SendKey(key);
        }

    }
}
