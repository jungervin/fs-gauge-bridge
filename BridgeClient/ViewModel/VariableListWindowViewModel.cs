using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;

namespace BridgeClient.ViewModel
{
    class VariableItem : BaseViewModel
    {
        public string Name { get => Get<string>(); set => Set(value); }
        public string Value { get => Get<string>(); set => Set(value); }
        public bool IsHighlight { get => Get<bool>(); set => Set(value); }

        private DispatcherTimer m_timer = new DispatcherTimer();

        public VariableItem()
        {
            m_timer.Interval = TimeSpan.FromSeconds(3);
            m_timer.Tick += (_, __) =>
            {
                IsHighlight = false;
                m_timer.Stop();
            };
        }

        protected override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);

            if (name == nameof(Value))
            {
                IsHighlight = true;
                m_timer.Stop();
                m_timer.Start();
            }
        }

    }

    class VariableListWindowViewModel : BaseViewModel
    {
        public string Input { get => Get<string>(); set => Set(value); }

        public ObservableCollection<VariableItem> Variables { get => Get<ObservableCollection<VariableItem>>(); set => Set(value); }

        private Dictionary<string, VariableItem> m_variables = new Dictionary<string, VariableItem>();

        public VariableListWindowViewModel(SimConnectViewModel simConnect)
        {
            Variables = new ObservableCollection<VariableItem>();

            var t = new DispatcherTimer();
            t.Interval = TimeSpan.FromSeconds(0.2);
            t.Tick += (_, __) =>
            {
                var all = simConnect.GetAllSafe().OrderBy(x => x.Key);
                foreach (var kv in all)
                {
                    if (!string.IsNullOrWhiteSpace(Input))
                    {
                        if (kv.Key.Trim().ToLower().IndexOf(Input.Trim().ToLower()) < 0)
                        {
                            continue;
                        }
                    }

                    if (m_variables.ContainsKey(kv.Key))
                    {
                        m_variables[kv.Key].Value = kv.Value.ToString();
                    }
                    else
                    {
                        m_variables[kv.Key] = new VariableItem { Name = kv.Key, Value = kv.Value.ToString() };
                        Variables.Add(m_variables[kv.Key]);
                    }
                }
            };
            t.Start();
        }

        protected override void OnPropertyChanged(string name)
        {
            base.OnPropertyChanged(name);

            if (name == nameof(Input))
            {
                Variables.Clear();
                m_variables.Clear();
            }
        }

    }
}

