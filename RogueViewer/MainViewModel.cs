using RogueBot;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace RogueViewer
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<RogueInstance> Instances { get; } = new();
        public ICommand MoveCommand { get; }

        private RogueInstance? _selectedInstance;
        public RogueInstance? SelectedInstance
        {
            get => _selectedInstance;
            set
            {
                _selectedInstance = value;
                OnPropertyChanged();
            }
        }

        private readonly DispatcherTimer _timer;

        public MainViewModel()
        {
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(0.25)
            };

            _timer.Tick += (s, e) => RefreshInstances();
            _timer.Start();

            // TEMP test data
            Instances.Add(new RogueInstance
            {
                ProcessId = 5,
                Level = "Level 0",
                Runtime = TimeSpan.FromMinutes(0),
                MapText = string.Empty
            });

            SelectedInstance = Instances.First();
        }

        private void RefreshInstances()
        {
            var processes = GetRogueProcesses();

            var existing = Instances.ToDictionary(i => i.ProcessId);

            foreach (var process in processes)
            {
                if (existing.TryGetValue(process.ProcessId, out var instance))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // update existing
                        instance.Runtime = process.Runtime;
                        instance.MapText = process.MapText.Replace("\n\n", "\n");
                        instance.Level = process.Level;
                    });
                }
                else
                {
                    Instances.Add(process);
                }
            }

            // remove dead ones
            var toRemove = Instances.Where(i => !processes.Any(p => p.ProcessId == i.ProcessId)).ToList();
            foreach (var dead in toRemove)
                Instances.Remove(dead);
        }

        private IEnumerable<RogueInstance> GetRogueProcesses()
        {
            var processes = Process.GetProcessesByName("rogue54");

            Console.WriteLine($"PID | Level | Runtime");
            var result = new List<RogueInstance>();

            foreach (var process in processes)
            {
                try
                {
                    var console = new ConsoleController(process);
                    var map = new Map(console.ReadMap());
                    var player = new Player(map, console);
                    var id = process.Id;
                    var level = player == null ? 0 : player.Level;
                    var runTime = DateTime.Now - process.StartTime;
                    result.Add(
                        new RogueInstance
                        {
                            ProcessId = process.Id,
                            Runtime = runTime,
                            MapText = map.ToString(),
                            Level = "Level " + level
                        });
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            return result;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
