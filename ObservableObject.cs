using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SortationDashboard
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // [CallerMemberName] automatically picks up the name of the property calling this method.
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // A helper method to update a backing field and fire the event in one line
        protected bool SetProperty<T>(ref T backingField, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(backingField, value)) return false;

            backingField = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}